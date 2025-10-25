# CRM Work Item API (.NET 8)

This project exposes a CRM work item management API implemented with ASP.NET Core 8 and Entity Framework Core backed by PostgreSQL. It satisfies the following endpoints:

- `POST /api/crm/work-items` &mdash; create a work item.
- `GET /api/crm/work-items` &mdash; list, filter, and paginate work items.
- `GET /api/crm/work-items/{id}` &mdash; retrieve a work item with comments and attachments.
- `PUT /api/crm/work-items/{id}` &mdash; update work items using `If-Match` optimistic concurrency via `state_version`.
- `POST /api/crm/work-items/{id}/comments` &mdash; add a comment to a work item.
- `POST /api/crm/work-items/{id}/attachments` &mdash; register attachment metadata post pre-signing.
- `POST /api/crm/work-items/{id}/assign` &mdash; update the assignee with row locking (`FOR UPDATE`).
- `POST /api/crm/work-items/{id}/status` &mdash; drive the status state machine transitions.

## Getting started

### Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- PostgreSQL 14+

### Database

1. Create a PostgreSQL database and user. Update the connection string located at `src/CrmApi/appsettings.json` or export `CRM_DATABASE_CONNECTION`.
2. Apply migrations manually (the solution ships with EF Core models; generate migrations with `dotnet ef migrations add InitialCreate` and apply using `dotnet ef database update`).

### Run the API

```bash
dotnet restore src/CrmApi/CrmApi.csproj
dotnet run --project src/CrmApi/CrmApi.csproj
```

Swagger UI is available at `https://localhost:5001/swagger` by default.

### Testing the endpoints

Use any HTTP client (curl, Postman, etc.) to interact with the endpoints. Remember to set the `If-Match` header with the latest `state_version` value when issuing `PUT` updates.

## Project structure

```
src/
  CrmApi/
    Controllers/        -- ASP.NET Core controllers
    Data/               -- Entity Framework Core DbContext
    Dtos/               -- Request/response contracts
    Models/             -- EF Core entity models
    Services/           -- Domain and persistence logic
```

## License

MIT
