# Administração do SQL Server no VS Code

## Conexão local

- Perfil no painel **SQL Server**: `Controle Comtac (SQL Express)`
- Servidor: `localhost\SQLEXPRESS`
- Banco: `SistemaDeControleComtac`
- Autenticação: Windows integrada, sem senha salva

Se uma pasta mostrar `No items`, use o botão de atualizar do painel. Para conferir a
conexão, abra **New Query** no perfil e execute:

```sql
SELECT DB_NAME() AS BancoAtual, @@SERVERNAME AS Servidor;
```

## Consultar os dados

Expanda **Tables**, clique com o botão direito em uma tabela e escolha a opção de selecionar
as primeiras linhas. Consultas de leitura seguras:

```sql
SELECT COUNT(*) AS TotalVeiculos FROM dbo.RegistrosVeiculos;

SELECT TOP (100) *
FROM dbo.RegistrosVeiculos
ORDER BY Id DESC;
```

## Alterar dados com segurança

Antes de `UPDATE` ou `DELETE`, faça um `SELECT` com o mesmo `WHERE`. Use uma transação e
confira o resultado antes de confirmar:

```sql
BEGIN TRANSACTION;

UPDATE dbo.RegistrosVeiculos
SET TecnicoResponsavel = N'Nome correto'
WHERE Id = 1;

SELECT * FROM dbo.RegistrosVeiculos WHERE Id = 1;

-- Use somente uma das duas linhas abaixo:
ROLLBACK TRANSACTION; -- desfaz para testar
-- COMMIT TRANSACTION; -- confirma depois de conferir
```

Nunca execute `UPDATE` ou `DELETE` sem `WHERE` no banco real.

## Criar e verificar um backup

O SQL Server Express não oferece compressão nativa. Esta consulta cria um backup com
checksum na pasta padrão protegida do SQL Server e o verifica em seguida:

```sql
DECLARE @Pasta nvarchar(4000) =
    CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'));
IF RIGHT(@Pasta, 1) NOT IN (N'\', N'/') SET @Pasta += N'\';

DECLARE @Arquivo nvarchar(4000) =
    @Pasta + N'SistemaDeControleComtac-manual.bak';

BACKUP DATABASE SistemaDeControleComtac
TO DISK = @Arquivo
WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10;

RESTORE VERIFYONLY FROM DISK = @Arquivo WITH CHECKSUM;
SELECT @Arquivo AS ArquivoVerificado;
```

Não restaure um backup sobre o banco ativo sem antes parar o sistema e confirmar o destino.

## Atualizações do projeto

A conexão local fica em User Secrets. Para conferir se está configurada:

```powershell
dotnet user-secrets list
```

Para aplicar uma migração SQL Server já revisada:

```powershell
dotnet ef database update
```

O serviço `SQL Server (SQLEXPRESS)` precisa estar em execução. SQLite permanece apenas nos
testes automatizados e no backup histórico feito antes da migração.
