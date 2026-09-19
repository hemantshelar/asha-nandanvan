IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'app-ashanandanvan-dev')
BEGIN
    CREATE USER [app-ashanandanvan-dev] FROM EXTERNAL PROVIDER;
END

IF IS_ROLEMEMBER(N'db_owner', N'app-ashanandanvan-dev') = 0
BEGIN
    ALTER ROLE db_owner ADD MEMBER [app-ashanandanvan-dev];
END
