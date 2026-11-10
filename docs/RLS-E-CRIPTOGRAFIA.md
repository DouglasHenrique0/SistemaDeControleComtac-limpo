# RLS e criptografia

## RLS ativo

O SQL Server aplica a policy `dbo.ComtacFinanceSecurityPolicy` às tabelas `Receitas`,
`Despesas` e `RelatoriosExcelGerados`. A função de filtro lê `comtac_is_admin` de
`SESSION_CONTEXT`; o interceptor da aplicação define esse valor com parâmetro sempre que
uma conexão é aberta. Isso evita depender apenas da interface para proteger o financeiro.

Validação operacional executada no banco local:

- contexto não administrador: zero linhas financeiras visíveis;
- contexto administrador: registros existentes visíveis;
- colaborador ainda consegue registrar a receita da saída, mas não consegue consultá-la;
- o teste de escrita foi executado dentro de uma transação e revertido.

Ao alterar a policy, teste obrigatoriamente conexões reutilizadas do pool para garantir que
o contexto anterior nunca vaze. Jobs internos sem request são tratados como contexto
administrativo para permitir migrations e rotinas de sistema.

## Proteções já ativas

- senhas usam o hash e o salt do ASP.NET Core Identity; não são criptografadas de forma
  reversível;
- cookies e tokens da aplicação usam ASP.NET Core Data Protection;
- o cookie de autenticação é `HttpOnly`, `Secure`, `SameSite=Lax` e usa prefixo `__Host-`;
- strings de conexão, DSNs e chaves ficam fora do Git;
- queries da aplicação usam EF Core/LINQ ou parâmetros explícitos;
- logs de banco contêm somente tempo, resultado, provedor e fingerprint, nunca valores.

## Criptografia de dados em repouso ainda exige decisão de produção

RLS não criptografa os arquivos do banco. Antes do go-live, escolha e registre uma destas
estratégias conforme a hospedagem:

1. TDE/criptografia gerenciada do volume e dos backups para proteção em repouso;
2. Always Encrypted para colunas que nem o operador do banco deve ler;
3. criptografia de campo com envelope encryption para uma necessidade específica.

A chave mestra deve ficar em Key Vault/KMS/HSM, com identidade gerenciada, rotação,
auditoria, backup e procedimento de recuperação. Não implemente criptografia de campo sem
definir pesquisa, ordenação, retenção e recuperação de chave: perder a chave perde os
dados. Portanto, a parte de criptografia em repouso permanece bloqueada até a escolha do
provedor de hospedagem e do serviço de chaves.

## Verificação da policy

Execute com uma conta de administração do SQL Server:

```sql
SELECT name, is_enabled
FROM sys.security_policies
WHERE name = N'ComtacFinanceSecurityPolicy';
```

O resultado esperado é `is_enabled = 1`. Nunca desative a policy como correção de um erro
de autorização; corrija o contexto, a regra ou a migration.
