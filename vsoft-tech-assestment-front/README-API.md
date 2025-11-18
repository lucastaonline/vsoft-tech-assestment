# Geração Automática do Cliente API

Este projeto utiliza **geração automática de cliente TypeScript** a partir da especificação OpenAPI do backend, seguindo as melhores práticas profissionais.

## Por que usar OpenAPI?

✅ **Type Safety**: Tipos TypeScript gerados automaticamente do spec  
✅ **Sincronização**: Cliente sempre sincronizado com o backend  
✅ **Menos Código**: Não precisa escrever manualmente tipos e funções  
✅ **Manutenibilidade**: Mudanças no backend refletem automaticamente no frontend  
✅ **Documentação**: O spec OpenAPI é a fonte única de verdade  

## Como funciona

1. O backend expõe o spec OpenAPI em `/swagger/v1/swagger.json`
2. Usamos `@hey-api/openapi-ts` para gerar o cliente TypeScript
3. O cliente gerado fica em `src/lib/api/`
4. Usamos o cliente gerado no código da aplicação

## Gerando o Cliente

### Pré-requisitos

Certifique-se de que o backend está rodando:

**Docker (recomendado):**
```bash
# O Docker expõe a API na porta 8080
docker-compose up
```

**Desenvolvimento local:**
```bash
cd ../vsoft-tech-assestment-back/VSoftTechAssestment.Api
dotnet run
# API estará em http://localhost:5001
```

### Gerar o Cliente

```bash
npm run generate:api
```

### Gerar com Watch Mode (regenera automaticamente)

```bash
npm run generate:api:watch
```

### Usar URL Customizada

**Docker (porta 8080):**
```bash
API_OPENAPI_URL=http://localhost:8080/swagger/v1/swagger.json npm run generate:api
```

**Desenvolvimento local (porta 5001):**
```bash
API_OPENAPI_URL=http://localhost:5001/swagger/v1/swagger.json npm run generate:api
```

## Estrutura Gerada

Após a geração, você terá:

```
src/lib/api/
├── index.ts          # Exportações principais
├── core/             # Core do cliente (configuração, tipos base)
├── services/         # Serviços por tag (AuthService, TasksService, etc)
└── types/            # Tipos TypeScript gerados
```

## Uso no Código

### Exemplo: Autenticação

```typescript
import { client } from '@/lib/api'
import { AuthService } from '@/lib/api/services/AuthService'

// Configurar token de autenticação
client.setConfig({
  headers: {
    Authorization: `Bearer ${token}`
  }
})

// Usar o serviço gerado
const response = await AuthService.login({
  userNameOrEmail: 'user@example.com',
  password: 'password123'
})

if (response.data) {
  // response.data está tipado automaticamente!
  console.log(response.data.token)
}
```

### Exemplo: Com tratamento de erros

```typescript
import { AuthService } from '@/lib/api/services/AuthService'

try {
  const response = await AuthService.login(credentials)
  
  if (response.data) {
    // Sucesso - dados tipados
    return response.data
  } else {
    // Erro da API
    throw new Error(response.error?.message || 'Erro ao fazer login')
  }
} catch (error) {
  // Erro de rede ou outro erro
  console.error('Erro:', error)
  throw error
}
```

## Migração do Código Manual

O código manual em `src/services/api.ts` será substituído pelo cliente gerado. 

**Vantagens da migração:**
- ✅ Tipos sempre sincronizados com o backend
- ✅ Menos código para manter
- ✅ Detecção de erros em tempo de compilação
- ✅ Autocomplete completo no IDE

## Configuração

A configuração está em `openapi.config.ts`:

```typescript
export default defineConfig({
  client: '@hey-api/client-fetch',  // Usa fetch nativo
  input: 'http://localhost:8080/swagger/v1/swagger.json', // Docker: 8080, Dev: 5001
  output: {
    path: './src/lib/api',
    format: 'prettier',
  },
})
```

## Integração com CI/CD

Adicione a geração do cliente no seu pipeline:

```yaml
# .github/workflows/ci.yml
- name: Generate API Client
  run: |
    # Iniciar backend temporariamente
    cd backend && dotnet run &
    sleep 10
    cd frontend && npm run generate:api
```

## Troubleshooting

### Erro: "Request failed"
- Verifique se o backend está rodando
- Verifique a URL do OpenAPI spec
- **Docker**: Teste `curl http://localhost:8080/swagger/v1/swagger.json`
- **Dev local**: Teste `curl http://localhost:5001/swagger/v1/swagger.json`

### Erro: "Cannot find module '@/lib/api'"
- Execute `npm run generate:api` primeiro
- Verifique se o backend está rodando

### Tipos desatualizados
- Execute `npm run generate:api` novamente
- Ou use `npm run generate:api:watch` para regenerar automaticamente

