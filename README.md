# VSoft Tech Assessment

Monorepositório contendo o backend em .NET 8, o frontend em Vue 3 + Pinia e a infraestrutura Docker solicitada no desafio técnico da VSoft.

## Estrutura

```
.
├── docker-compose.yml           # Orquestração completa (API, Front, PostgreSQL, RabbitMQ)
├── README-DOCKER.md             # Guia detalhado do ambiente containerizado
├── vsoft-tech-assestment-back/  # API ASP.NET Core 8 (REST + SignalR)
└── vsoft-tech-assestment-front/ # SPA Vue 3 + Vite + shadcn-vue
```

## Pré-requisitos

| Contexto | Requisitos |
|----------|------------|
| Docker   | Docker Desktop ≥ 4 / Engine ≥ 20.10 e Docker Compose ≥ 2 |
| Local    | .NET SDK 8.0, Node.js 20.x (ou 22.x) + npm 10, PostgreSQL 15+, RabbitMQ 3.12+ |

> Todos os comandos abaixo assumem o diretório raiz `/Users/lucas/Gits/vsoft-tech-assestment`.

## Executando tudo com Docker (recomendado)

1. Copie `.env.example` para `.env` e ajuste secrets se necessário.
2. Suba os serviços:

```bash
docker compose up -d --build
```

3. Endpoints principais:
   - Frontend: http://localhost:3000
   - API / Swagger: http://localhost:8080
   - RabbitMQ UI: http://localhost:15672 (guest/guest)
   - Postgres: localhost:5432

Mais detalhes e comandos úteis estão em `README-DOCKER.md`.

## Execução local (des acoplado do Docker)

### Backend

```bash
cd vsoft-tech-assestment-back/VSoftTechAssestment.Api
dotnet restore
dotnet ef database update  # exige PostgreSQL configurado
dotnet run
```

Serviço sobe por padrão em `https://localhost:5001` (Kestrel). Veja `vsoft-tech-assestment-back/README.md` para cenários de debug (VS Code, Visual Studio e `dotnet watch`), coverage de testes e integração com RabbitMQ.

### Frontend

```bash
cd vsoft-tech-assestment-front
npm install
cp .env.example .env.local  # ajuste VITE_API_BASE_URL se necessário
npm run dev
```

A aplicação fica acessível em http://localhost:5173. O arquivo `vsoft-tech-assestment-front/README.md` descreve o fluxo de desenvolvimento (Pinia, geração de cliente OpenAPI e ferramentas de debug).

## Testes

| Camada    | Comando                                                       |
|-----------|----------------------------------------------------------------|
| Backend   | `dotnet test vsoft-tech-assestment-back/VSoftTechAssestment.Api.Tests` |
| Frontend  | `cd vsoft-tech-assestment-front && npm run test:unit`          |

Os testes de backend incluem cenários unitários e de integração (com banco em memória e RabbitMQ mockado). Os testes de frontend cobrem stores críticos (auth/tasks), Dashboard e integração do `App.vue`.

## Documentação complementar

- `README-DOCKER.md`: fluxo completo via Docker (build, logs, troubleshooting).
- `vsoft-tech-assestment-back/README.md`: arquitetura detalhada da API, migrations, debug e testes.
- `vsoft-tech-assestment-front/README.md`: guia de desenvolvimento do SPA, geração do SDK OpenAPI, lint/test/watch.
- `vsoft-tech-assestment-front/README-API.md`: instruções da geração automática do cliente TypeScript.

Em caso de dúvidas ou novos ambientes, priorize rodar `docker compose up -d` para validar rapidamente a entrega ponta a ponta.

