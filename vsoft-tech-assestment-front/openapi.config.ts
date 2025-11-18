import { defineConfig } from '@hey-api/openapi-ts'

/**
 * Configuração para geração do cliente TypeScript a partir do OpenAPI spec
 * 
 * Para gerar o cliente:
 * 1. Certifique-se de que o backend está rodando (Docker: localhost:8080)
 * 2. Execute: npm run generate:api
 * 
 * URLs suportadas:
 * - Docker: http://localhost:8080/swagger/v1/swagger.json
 * - Desenvolvimento local: http://localhost:5001/swagger/v1/swagger.json
 * 
 * Ou use uma URL diferente:
 * API_OPENAPI_URL=http://seu-backend/swagger/v1/swagger.json npm run generate:api
 */
export default defineConfig({
    client: '@hey-api/client-fetch',
    input: process.env.API_OPENAPI_URL || 'http://localhost:8080/swagger/v1/swagger.json',
    output: {
        path: './src/lib/api',
        format: 'prettier',
    },
    types: {
        enums: 'typescript',
    },
})
