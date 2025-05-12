# Cloud Drive

Implementation of a remote storage system with synchronization and simple file versioning.
`ASP.NET` + `PostgreSQL` + `WPF`

> [!CAUTION]
This is a university project.


## How-to

### Database setup

You have two options here - either have PostgreSQL database installed straight onto your system or use Docker.

#### Manual initialization

Open either pgAdmin or a console through which you will interact with the database service.  
Use the `db-init/db-init.sh` script to run SQL statements that will setup appropriate databases and users used by the application.  
When using a console you can use the script directly. Just make sure to change user, database and password to match a super user in your database.  
When using pgAdmin copy code between `<<EOSQL` and `EOSQL` at the end into the query tool and run all statements.

#### Docker

To bring up the database with Docker in project root do: `docker compose --profile dev up`.
Similarly to stop the environment without completely removing it do: `docker compose --profile dev stop`.


### Database update

1. Bring up development database using docker-compose: `docker compose --profile dev up`.
2. Open the project in Visual Studio.
3. Choose `CloudDrive.WebAPI` as the Startup Project.
4. Open the `Package Manager Console` view.
5. In it choose `CloudDrive.Infrastructure` as the Default project.
6. Enter `Update-Database` and execute. This will get your local DB up to date with the current schema.


## Secrets

### CloudDrive.WebAPI
`ConnectionStrings:DefaultConnection` - database connection string
`Jwt:Secret` - JWT signing key, should be at least 32-characters long
`Jwt:ExpirationMinutes` - JWT token expiration timespan in minutes
`FsRoot` - root file system directory in which files will be stored

All development secrets are currently stored directly in the repository due to time constraints.
Realistically you should use `dotnet user-secrets` or `Azure Key Vault` for storing sensitive configuration data.
