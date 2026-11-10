# Configuração segura

O sistema não contém mais senhas ou contas de teste no código-fonte.

## Histórico higienizado

Em 27 de agosto de 2026, os três branches remotos tiveram o histórico reescrito para
remover o banco SQLite versionado e os diretórios gerados `bin`/`obj`. Um scan completo
com Gitleaks 8.24.3, validado pelo checksum oficial, examinou os 34 commits reescritos e
não encontrou segredos textuais. Clones criados antes dessa limpeza devem ser descartados
e clonados novamente; não faça merge de branches baseados nos SHAs antigos.

O bundle de recuperação anterior à limpeza deve permanecer somente em armazenamento
local protegido, pois contém exatamente os artefatos removidos, e deve ser eliminado
quando a janela de recuperação terminar.

## Administrador inicial

O banco existente mantém seus usuários atuais. Em um banco novo, configure o primeiro administrador por variáveis de ambiente ou User Secrets antes da primeira execução:

```powershell
dotnet user-secrets set "BootstrapAdmin:Email" "administrador@empresa.com"
dotnet user-secrets set "BootstrapAdmin:Password" "use-uma-senha-forte"
```

Depois que o administrador for criado, remova `BootstrapAdmin:Password` dos segredos. O sistema nunca redefine automaticamente a senha de uma conta existente.

## Antes de publicar

- Troque qualquer senha que já tenha aparecido em commits antigos.
- Não envie arquivos `.db`, `.db-wal`, `.db-shm` ou `.bak` ao Git.
- Reescreva o histórico se um segredo ou banco já foi versionado e revogue antes todas as
  credenciais potencialmente expostas. Quem tiver clones antigos precisará clonar novamente.
- Use HTTPS e mantenha segredos fora de `appsettings.json`; use cofre com rotação em produção.
- Em produção, forneça `ConnectionStrings__DefaultConnection` por variável de ambiente ou cofre de segredos.
- Faça e verifique um backup SQL Server antes de executar migrations em produção.
- Escolha TDE/Always Encrypted ou criptografia de campo conforme
  `docs/RLS-E-CRIPTOGRAFIA.md`; RLS não substitui criptografia em repouso.
- Conecte um backend OTLP/Sentry e configure alertas antes de aceitar tráfego real.
- Ative deploy por ambiente protegido e teste o rollback automático conforme
  `docs/DEPLOY-E-ROLLBACK.md`.

## Comportamento de segurança atual

- todas as rotas exigem autenticação por padrão; exceções usam `[AllowAnonymous]`;
- APIs retornam 401/403 sem redirecionamento HTML;
- login tem rate limit por IP e Identity bloqueia a conta após cinco falhas;
- cookies são `HttpOnly`, `Secure`, `SameSite=Lax` e protegidos por Data Protection;
- formulários mantêm antiforgery e inputs possuem limites server-side;
- acesso ao banco usa EF Core/queries parametrizadas;
- o RLS financeiro está ativo no SQL Server e depende do papel Admin da sessão autenticada;
- stack traces ficam somente no backend, correlacionados pelo Request ID.
