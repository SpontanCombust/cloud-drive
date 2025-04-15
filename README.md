# Cloud Drive

Implementation of a remote storage system with synchronization and simple file versioning.
`ASP.NET` + `PostgreSQL` + `WPF`

> [!CAUTION]
This is a university project.


## How-to

### Database

1. Bring up development database using docker-compose: `docker compose --profile dev up`.
2. Open the project in Visual Studio.
3. Open the `Package Manager Console` view.
4. Choose `CloudDrive.Infrastructure` as the default project.
5. Enter `Update-Database` and execute. This will get your local DB up to date with the current schema.


## Secrets

### CloudDrive.WebAPI
`ConnectionStrings:DefaultConnection` - database connection string
`Jwt:Secret` - JWT signing key, should be at least 32-characters long
`Jwt:ExpirationMinutes` - JWT token expiration timespan in minutes
`FsRoot` - root file system directory in which files will be stored

All development secrets are currently stored directly in the repository due to time constraints.
Realistically you should use `dotnet user-secrets` or `Azure Key Vault` for storing sensitive configuration data.
