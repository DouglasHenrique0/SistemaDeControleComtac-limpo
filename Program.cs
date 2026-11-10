using System.Diagnostics;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SistemaDeControleComtac.Data;
using SistemaDeControleComtac.Health;
using SistemaDeControleComtac.Infrastructure;
using SistemaDeControleComtac.Middleware;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Observability;
using SistemaDeControleComtac.Services;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

// O E2E executa a aplicação a partir do projeto, fora de Development. Nesse cenário,
// o SDK não habilita o manifesto de assets automaticamente como ocorre no publish.
if (builder.Environment.IsEnvironment("E2E"))
{
    builder.WebHost.UseStaticWebAssets();
}

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
    options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
});

var sentryDsn = builder.Configuration["Sentry:Dsn"]?.Trim();
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.AttachStacktrace = true;
        options.SendDefaultPii = false;
        options.TracesSampleRate = builder.Configuration.GetValue<double?>("Sentry:TracesSampleRate") ?? 0.1;
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(ApplicationRoles.Admin));
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Financeiro", "AdminOnly");
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 256;
    options.KeyLengthLimit = 256;
    options.ValueLengthLimit = 16 * 1024;
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

var loginPermitLimit = Math.Max(
    1,
    builder.Configuration.GetValue<int?>("RateLimiting:Login:PermitLimit") ?? 10);
var loginWindowSeconds = Math.Max(
    1,
    builder.Configuration.GetValue<int?>("RateLimiting:Login:WindowSeconds") ?? 300);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromSeconds(loginWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        Telemetry.LoginAttempts.Add(1, new KeyValuePair<string, object?>("outcome", "rate_limited"));

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Security.LoginRateLimit");
        logger.LogWarning(
            "Login request rejected by rate limit. {RequestId} {Path}",
            context.HttpContext.TraceIdentifier,
            context.HttpContext.Request.Path.Value);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                error = "Muitas tentativas. Aguarde antes de tentar novamente.",
                requestId = context.HttpContext.TraceIdentifier
            },
            cancellationToken);
    };
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A conexão 'DefaultConnection' não foi configurada.");
var databaseProvider = builder.Configuration["DatabaseProvider"]?.Trim() ?? "SqlServer";

builder.Services.AddSingleton<DatabaseTelemetryInterceptor>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SqlServerSessionContextInterceptor>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    options.AddInterceptors(
        services.GetRequiredService<DatabaseTelemetryInterceptor>(),
        services.GetRequiredService<SqlServerSessionContextInterceptor>());

    if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
        return;
    }

    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString, sqlServerOptions =>
            sqlServerOptions.EnableRetryOnFailure());
        return;
    }

    throw new InvalidOperationException(
        $"O provedor de banco '{databaseProvider}' não é suportado. Use 'SqlServer' ou 'Sqlite'.");
});

builder.Services.AddDataProtection()
    .SetApplicationName("SistemaDeControleComtac");

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IApplicationCache, InstrumentedMemoryCache>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<RelatorioFinanceiroService>();
builder.Services.AddSingleton<RelatorioExcelService>();
builder.Services.AddSingleton<RelatorioArquivoStorageService>();
builder.Services.AddSingleton<ReportStorageHealthCheck>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready"])
    .AddCheck<ReportStorageHealthCheck>(
        name: "report-storage",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["ready"]);

var otlpEnabled = builder.Configuration.GetValue<bool>("OpenTelemetry:Otlp:Enabled");
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: "SistemaDeControleComtac",
        serviceVersion: ApplicationInfo.Version))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options => options.RecordException = true)
            .AddHttpClientInstrumentation()
            .AddSource(Telemetry.ActivitySourceName);

        if (otlpEnabled)
        {
            tracing.AddOtlpExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(Telemetry.MeterName);

        if (otlpEnabled)
        {
            metrics.AddOtlpExporter();
        }
    });

builder.Services.AddIdentity<UsuarioIdentity, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Login";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "__Host-Comtac.Auth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        await dbContext.Database.MigrateAsync();
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var roleName in new[] { ApplicationRoles.Admin, ApplicationRoles.Colaborador })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var bootstrapEmail = builder.Configuration["BootstrapAdmin:Email"]?.Trim();
    var bootstrapPassword = builder.Configuration["BootstrapAdmin:Password"];

    if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        var adminUser = await userManager.FindByEmailAsync(bootstrapEmail);

        if (adminUser is null)
        {
            adminUser = new UsuarioIdentity
            {
                UserName = bootstrapEmail,
                Email = bootstrapEmail,
                Nome = "Administrador",
                Ativo = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, bootstrapPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Não foi possível criar o administrador inicial com a configuração fornecida.");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Não foi possível atribuir a função Admin ao administrador inicial.");
            }
        }

        adminUser.LockoutEnabled = true;
        adminUser.Ativo = true;
        await userManager.UpdateAsync(adminUser);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "same-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers.ContentSecurityPolicy =
            "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; " +
            "object-src 'none'; img-src 'self' data:; script-src 'self'; style-src 'self' 'unsafe-inline'";
        return Task.CompletedTask;
    });

    await next();
});

app.UseRouting();
app.UseWhen(
    context => HttpMethods.IsPost(context.Request.Method) &&
        context.Request.Path.Equals("/Conta/Login", StringComparison.OrdinalIgnoreCase),
    branch => branch.UseRateLimiter());
app.UseAuthentication();
app.UseAuthorization();

var staticAssets = app.MapStaticAssets();
staticAssets.Add(endpointBuilder => endpointBuilder.Metadata.Add(new AllowAnonymousAttribute()));
staticAssets.ShortCircuit();
app.MapRazorPages()
    .WithStaticAssets();

app.MapGet("/api/status", () => Results.Ok(new
{
    status = "ok",
    version = ApplicationInfo.Version
}))
    .RequireAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync
})
    .AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync
})
    .AllowAnonymous();

app.MapFallbackToPage("/NaoEncontrado");

app.Run();

public partial class Program;
