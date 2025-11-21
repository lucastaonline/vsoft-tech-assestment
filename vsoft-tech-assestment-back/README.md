# Backend (.NET 8) – VSoft Tech Assessment

API ASP.NET Core 8 que expõe os endpoints REST/SSE usados pelo frontend. O projeto segue princípios de Clean Code, integra RabbitMQ para notificações e utiliza SignalR para entrega em tempo real.

## Tecnologias principais

- ASP.NET Core 8 Web API + Minimal Hosting
- Entity Framework Core + PostgreSQL + Identity
- JWT + cookies HttpOnly para autenticação segura
- RabbitMQ + `BackgroundService` (consumer) + SignalR Hub
- Swagger/Scalar com suporte a Bearer token
- Testes xUnit (unitários e integração) com FluentAssertions, Moq e `WebApplicationFactory`

## Estrutura

```
vsoft-tech-assestment-back/
├── VSoftTechAssestment.Api/
│   ├── Controllers/                 # Auth, Tasks, Users, Notifications
│   ├── Data/ApplicationDbContext.cs # Contexto EF + Identity
│   ├── Models/DTOs e Entities       # Contratos e domínios
│   ├── Services/                    # Auth, Tasks, RabbitMQ, Notifications
│   ├── Hubs/NotificationHub.cs      # SignalR
│   ├── Program.cs / appsettings.*   # Bootstrap e DI
│   └── Dockerfile / entrypoint.sh
└── VSoftTechAssestment.Api.Tests/   # Suite de testes
```

## Pré-requisitos

- .NET SDK 8.0 (`dotnet --version`)
- PostgreSQL 15+ e RabbitMQ 3.12+ (ou utilize os containers do `docker compose`)
- Ferramenta EF CLI: `dotnet tool install --global dotnet-ef`

## Configuração

1. Ajuste `appsettings.Development.json` ou use `dotnet user-secrets`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=vsoft_tech_assessment;Username=postgres;Password=postgres"
   },
   "Jwt": {
     "Secret": "chave-super-secreta-com-32-caracteres",
     "Issuer": "VSoftTechAssestment",
     "Audience": "VSoftTechAssestment",
     "ExpirationMinutes": 60
   },
   "RabbitMQ": {
     "HostName": "localhost",
     "UserName": "guest",
     "Password": "guest",
     "QueueName": "task_notifications"
   }
   ```
2. Crie o banco/migrations:
   ```bash
   cd vsoft-tech-assestment-back/VSoftTechAssestment.Api
   dotnet ef database update
   ```
3. (Opcional) Use `dotnet user-secrets set "Jwt:Secret" "<sua-chave>"` para esconder secrets durante o desenvolvimento.

## Execução e debug

### CLI

```bash
cd vsoft-tech-assestment-back/VSoftTechAssestment.Api
dotnet restore
dotnet watch run   # recompila ao salvar
```

API disponível em `https://localhost:5001` (Kestrel) e `http://localhost:5000`. O Swagger/Scalar fica em `/swagger` e já suporta o fluxo de autenticação.

### Visual Studio

1. Abra `VSoftTechAssestment.sln`.
2. Selecione o projeto `VSoftTechAssestment.Api` como Startup.
3. Use o perfil `https` ou `http` de `Properties/launchSettings.json`.
4. Pressione `F5` e debugue normalmente (breakpoints em controllers/serviços).

### VS Code

1. Abra a pasta `vsoft-tech-assestment-back`.
2. Execute **.NET: Generate Assets for Build and Debug** (C# Dev Kit/OmniSharp) para criar `.vscode/launch.json`.
3. Use o profile “.NET Launch Kestrel” para depurar.
4. `dotnet watch run` é a opção mais rápida para hot reload. Breakpoints funcionam via `Run and Debug` > `.NET Launch`.

### Dependências externas

- **RabbitMQ**: Se não estiver usando docker, suba manualmente (`brew services start rabbitmq` ou `docker compose up rabbitmq`).  
- **SignalR**: o hub está em `/notificationsHub`. Utilize o frontend (hook `useSignalR`) ou um client como `wscat` para validar notificações.
- **UsersController**: expõe `POST /api/users/createRandom` para gerar usuários fake e testar fan-out de notificações via RabbitMQ.

## Testes

```bash
dotnet test vsoft-tech-assestment-back/VSoftTechAssestment.Api.Tests/VSoftTechAssestment.Api.Tests.csproj
```

- **Controllers**: Auth e Tasks com cenários de sucesso/erro (JWT, validação, ownership).
- **Integração**: `TasksIntegrationTests` usa `WebApplicationFactory`, EF InMemory e RabbitMQ mockado (o hosted service real é removido durante os testes).
- Para executar algo específico: `dotnet test --filter "FullyQualifiedName~TasksControllerTests.UpdateTask_WithValidData"`.

## Dicas de desenvolvimento

- Regere o cliente OpenAPI após alterar DTOs (`npm run generate:api` no frontend).
- Para acompanhar mensagens na fila, abra http://localhost:15672 (guest/guest) quando estiver usando Docker.
- `dotnet ef migrations add <NomeDaMigration>` sempre dentro de `VSoftTechAssestment.Api`.
- `docker compose up -d` (na raiz) sobe um ambiente completo (Postgres, RabbitMQ, API e Front). Consulte `README-DOCKER.md` para mais detalhes.

## Troubleshooting rápido

| Sintoma | Correção |
|--------|----------|
| `RabbitMQ.Client.Exceptions.BrokerUnreachableException` | Serviço RabbitMQ indisponível. Suba o container (`docker compose up rabbitmq`) ou revise as credenciais em `appsettings`. |
| `Npgsql.NpgsqlException (0x80004005): 3D000` | Banco inexistente. Execute `dotnet ef database update` ou crie manualmente `CREATE DATABASE vsoft_tech_assessment`. |
| `System.InvalidOperationException: Unable to resolve service` | Rode `dotnet restore` e confirme que todos os projetos estão presentes na solução. |

## Próximos passos

- Garanta cobertura mínima de testes para novos endpoints críticos.
- Documente migrations relevantes no PR e valide em staging antes do deploy.
- Mantenha a execução via `docker compose up -d` intacta sempre que alterar configurações do backend.

