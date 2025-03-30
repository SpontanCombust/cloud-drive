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
4. Enter `Update-Database` and execute. This will get your local DB up to date with the current schema.
5. Launch `CloudDrive.WebAPI` to start the REST server
6. Launch `CloudDrive.App` to start the client application


## Secrets

`ConnectionStrings:DefaultConnection` - database connection string
`Jwt:Secret` - JWT signing key, should be at least 32-characters long
`Jwt:ExpirationMinutes` - JWT token expiration timespan in minutes

All development secrets are currently stored directly in the repository due to time constraints.
Realistically you should use `dotnet user-secrets` or `Azure Key Vault` for storing sensitive configuration data.
