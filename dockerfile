FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY SistemaDeControleComtac.csproj ./
COPY Directory.Build.props ./

RUN dotnet restore SistemaDeControleComtac.csproj

COPY . .

RUN dotnet publish SistemaDeControleComtac.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# ============================
# ETAPA 2 - RUNTIME
# ============================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Ambiente de produção
ENV ASPNETCORE_ENVIRONMENT=Production

# Aplicação escutará internamente nesta porta
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# Copia somente a aplicação publicada
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SistemaDeControleComtac.dll"]