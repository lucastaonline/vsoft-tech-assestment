# Frontend (Vue 3 + Vite) – VSoft Tech Assessment

SPA construída com Vue 3, Pinia, shadcn-vue e Vite. Consome o backend .NET via cliente TypeScript gerado automaticamente a partir do OpenAPI spec.

## Tecnologias

- Vue 3 + `<script setup>`
- Vite 7 + Vitest + Testing Library (Vue Test Utils)
- Pinia para gerenciamento global
- shadcn-vue + Tailwind para UI
- @hey-api/openapi-ts para geração do SDK (`src/lib/api`)
- SignalR + `@microsoft/signalr` para notificações em tempo real

## Pré-requisitos

- Node.js 20.x (ou 22.x) + npm 10
- Backend rodando em `http://localhost:8080` (via Docker ou `dotnet run`)

## Setup

```bash
cd vsoft-tech-assestment-front
npm install
cp .env.example .env.local   # opcional — define VITE_API_BASE_URL
npm run dev
```

Por padrão o app roda em `http://localhost:5173`.

### Variáveis de ambiente

| Variável | Descrição | Default |
|----------|-----------|---------|
| `VITE_API_BASE_URL` | URL da API .NET para REST/SignalR | `http://localhost:8080` |

## Scripts úteis

| Comando | Descrição |
|---------|-----------|
| `npm run dev` | Ambiente de desenvolvimento com HMR |
| `npm run build` | Build de produção (checa tipos + Vite build) |
| `npm run preview` | Serve o build gerado |
| `npm run lint` | ESLint com autofix e cache |
| `npm run test:unit` | Vitest (stores, views, componentes) |
| `npm run generate:api` | Regenera `src/lib/api` usando o arquivo versionado `openapi/swagger.v1.json` |

> Para apontar para outra URL durante a geração, use `API_OPENAPI_URL=http://... npm run generate:api` (o arquivo local será sobrescrito).

## Fluxo de desenvolvimento

1. **Inicie o backend** (`dotnet watch run` ou `docker compose up api rabbitmq postgres`).  
2. **Atualize o spec local** (quando necessário) exportando `/swagger/v1/swagger.json` para `openapi/swagger.v1.json` e rode `npm run generate:api`.  
3. **Rode `npm run dev`** e acesse `http://localhost:5173`.  
4. Use o `Pinia Devtools` + `Vue Devtools` para inspecionar stores `auth` e `tasks`.  
5. Para testar SignalR, faça login, crie/mova cards e observe notificações em tempo real.

### Debugando

- **VS Code**: abra a pasta `vsoft-tech-assestment-front`, instale “Vue Language Features” e use o debug “Vite” (launch via extensão `Debugger for Chrome`).  
- **Breakpoints**: configure `launch.json` com:
  ```json
  {
    "type": "pwa-chrome",
    "request": "launch",
    "name": "Vite Chrome",
    "url": "http://localhost:5173",
    "webRoot": "${workspaceFolder}"
  }
  ```
- **Vitest UI**: rode `npx vitest --ui` para depurar testes e visualizar coverage.  
- **geração OpenAPI**: mantenha o backend rodando e execute `npm run generate:api:watch` para regenerar sempre que o Swagger mudar.

## Testes

```bash
npm run test:unit
```

Cobertura atual:

- `useAuthStore` – login/registro/errors.
- `useTasksStore` – criar, atualizar e mover cards (Kanban).
- `DashboardView` – métricas e agregações.
- `App.vue` – layout base + CookieConsent + router.

Ao criar novos componentes, priorize testes de comportamento (rendição condicional, interações com Pinia) usando `@vue/test-utils`.

## Integração com o backend

- O Axios não é usado diretamente; toda chamada via `@hey-api/client-fetch` (fetch nativo).  
- Os interceptors (`src/config/interceptors.ts`) adicionam cookies HttpOnly no fluxo e gerenciam erros globais.  
- Em modo Docker, o frontend é servido por Nginx (`Dockerfile` + `nginx.conf`), respeitando rewrites para SPA.

## Troubleshooting

| Sintoma | Correção |
|---------|----------|
| `Failed to resolve component: router-view` em testes | garanta que os componentes sejam stubados (ver `src/__tests__/App.spec.ts`). |
| API retorna 401 mesmo logado | confirme que os cookies HttpOnly estão habilitados (`withCredentials` via `client.gen`) e que o domínio do backend bate com `VITE_API_BASE_URL`. |
| SignalR desconecta imediatamente | cheque se `VITE_API_BASE_URL` usa `http`/`https` compatível com o backend e se o hub `/notificationsHub` está acessível. |

## Referências

- `README-API.md`: detalhes do processo de geração do cliente OpenAPI.
- `README-DOCKER.md`: como rodar o frontend pelo container Nginx.
- `src/stores/*.ts`: contratos principais testados pelo Vitest.

