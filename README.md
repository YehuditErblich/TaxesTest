# TaxesTest

TaxesTest is a full-stack dynamic forms and approval workflow system.

## Project structure

- `server/`: ASP.NET Core Web API targeting .NET 8.
- `server/Domain/`: form, workflow, submission, upload, and status entities.
- `server/Application/`: typed DTOs and application services.
- `server/Data/`: EF Core `DbContext`, relational mappings, indexes, and status seed data.
- `client/`: Angular 20 standalone client with a responsive forms library and dynamic submission page.
- `infrastructure/`: AWS deployment guidance.

## Current status

The current development system supports:

- Creating and publishing form templates.
- Creating and publishing workflow templates.
- Database-driven form fields and field options.
- Form submission validation.
- Starting workflows for published forms.
- Assigned-user workflow actions and action history.
- Workflow status transitions.
- Angular loading, error, empty, validation, and success states.

The system is not production-complete yet. Authentication and authorization identity integration, role resolution, real file/object storage, relational migrations, and broader integration-test coverage still need to be added before production deployment.

The database currently uses EF Core InMemory. Data is reset whenever the API restarts. The model uses relational EF Core configuration so it can later move to SQL Server or PostgreSQL, but InMemory does not validate relational constraints, SQL translation, transactions, indexes, or provider-specific behavior.

## Prerequisites

- .NET 8 SDK
- Node.js 20 or newer
- npm

Check the installed versions:

```powershell
dotnet --version
node --version
npm.cmd --version
```

## Run locally

Install the Angular dependencies once:

```powershell
Set-Location client
npm.cmd install
Set-Location ..
```

Open two PowerShell terminals.

Terminal 1, start the API:

```powershell
dotnet run --project server --launch-profile http
```

Terminal 2, start Angular:

```powershell
Set-Location client
npm.cmd start
```

Open the client at `http://localhost:4200/forms`.

The API is available at:

- Swagger: `http://localhost:5080/swagger`
- Health: `http://localhost:5080/health`
- Form templates: `http://localhost:5080/api/form-templates`
- Workflow templates: `http://localhost:5080/api/workflow-templates`
- Statuses: `http://localhost:5080/api/statuses`

The Angular client currently uses `http://localhost:5080/api` as its API base URL. Update the client API service before using another environment.

## API documentation

See [server/API.md](server/API.md) for endpoint contracts and example route groups.

## Validate the project

Build the API:

```powershell
dotnet build server\server.csproj
```

Build the Angular client:

```powershell
Set-Location client
npm.cmd run build
```

Run Angular unit tests:

```powershell
npm.cmd test -- --watch=false --browsers=ChromeHeadless
```

## AWS direction

For production, host the Angular build in S3 behind CloudFront and host the API on a managed AWS service such as Elastic Beanstalk or App Runner. Replace InMemory with Amazon RDS-backed persistence and use AWS Secrets Manager or Systems Manager Parameter Store for secrets and environment-specific configuration. Add authentication, role resolution, object storage for uploads, migrations, observability, and relational integration tests before deployment.
