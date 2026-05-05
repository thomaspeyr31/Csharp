# TaskBoard

Application de gestion de tâches type Kanban en ASP.NET Core.

## Prérequis
- .NET 10
- SQLite

## Installation
```bash
dotnet restore
dotnet user-secrets set "Jwt:Key" "une-cle-secrete-de-32-caracteres-min"
dotnet ef database update
dotnet run
```

## Tests
```bash
dotnet test ./TaskBoard.slnx
```
