# Docker Setup - VSoft Tech Assessment

Este projeto está configurado para rodar completamente em Docker usando Docker Compose.

## Pré-requisitos

- Docker Desktop ou Docker Engine (versão 20.10+)
- Docker Compose (versão 2.0+)

## Configuração Inicial

1. Copie o arquivo `.env.example` para `.env` na raiz do projeto:

```bash
cp .env.example .env
```

2. (Opcional) Edite o arquivo `.env` para ajustar configurações como senhas e secrets.

## Como Executar

### Iniciar todos os serviços

```bash
docker-compose up -d
```

Este comando irá:
- Construir as imagens do frontend e backend
- Iniciar PostgreSQL na porta 5432
- Iniciar RabbitMQ na porta 5672 (management UI na porta 15672)
- Iniciar a API .NET na porta 8080
- Iniciar o frontend Vue.js na porta 3000

### Ver logs

```bash
# Todos os serviços
docker-compose logs -f

# Serviço específico
docker-compose logs -f api
docker-compose logs -f frontend
```

### Parar os serviços

```bash
docker-compose down
```

### Parar e remover volumes (apaga dados do banco)

```bash
docker-compose down -v
```

## Serviços Disponíveis

| Serviço | URL | Descrição |
|---------|-----|-----------|
| Frontend | http://localhost:3000 | Interface Vue.js |
| Backend API | http://localhost:8080 | API .NET REST |
| API Docs (Scalar) | http://localhost:8080 | Documentação interativa da API |
| PostgreSQL | localhost:5432 | Banco de dados |
| RabbitMQ Management | http://localhost:15672 | Interface de gerenciamento (guest/guest) |

## Estrutura Docker

### Frontend (`vsoft-tech-assestment-front/Dockerfile`)
- Multi-stage build com Node.js para build e Nginx para servir
- Build otimizado do projeto Vue.js com Vite
- Configuração Nginx para SPA routing

### Backend (`vsoft-tech-assestment-back/VSoftTechAssestment.Api/Dockerfile`)
- Multi-stage build com .NET SDK 8.0 para build e .NET Runtime para execução
- Health check endpoint em `/health`
- Porta 8080 exposta

### Docker Compose (`docker-compose.yml`)
- Orquestra todos os serviços
- Configura rede interna para comunicação entre containers
- Define volumes persistentes para PostgreSQL e RabbitMQ
- Configura healthchecks para dependências

## Variáveis de Ambiente

As variáveis de ambiente são configuradas no arquivo `.env`. Veja `.env.example` para referência.

### Principais variáveis:

- `DATABASE_CONNECTION_STRING`: Connection string do PostgreSQL
- `JWT_SECRET`: Chave secreta para assinatura de tokens JWT
- `RABBITMQ_HOST`, `RABBITMQ_PORT`: Configurações do RabbitMQ
- `FRONTEND_URL`: URL do frontend para configuração de CORS

## Migrations do Banco de Dados

### Executar Migrations

Migrations devem ser executadas manualmente antes de iniciar a aplicação. Isso segue as melhores práticas:

- Evita falhas de startup da aplicação devido a problemas de migration
- Previne condições de corrida em deploys com múltiplas instâncias
- Dá controle total sobre quando mudanças de schema ocorrem
- Permite revisão e teste de migrations antes da aplicação iniciar

### Como Executar

Execute as migrations localmente apontando para o banco do Docker:

```bash
# Instalar a ferramenta dotnet-ef (se necessário)
dotnet tool install --global dotnet-ef

# Executar migrations
cd vsoft-tech-assestment-back/VSoftTechAssestment.Api
dotnet ef database update --connection "Host=localhost;Port=5432;Database=vsoft_tech_assessment;Username=postgres;Password=postgres"
```

**Nota:** O `dotnet ef` requer acesso ao código-fonte completo do projeto (incluindo arquivos `.csproj` e pastas `Migrations/`), que não estão incluídos no build de Release publicado. Por isso, migrations devem ser executadas localmente ou em um ambiente que tenha acesso ao código-fonte completo.

### Executar Migrations em Produção

Para ambientes de produção, siga estas práticas recomendadas:

#### 1. Via CI/CD Pipeline (Recomendado)

Execute migrations como etapa separada antes do deploy da aplicação:

```yaml
# Exemplo GitHub Actions
- name: Run database migrations
  run: |
    cd vsoft-tech-assestment-back/VSoftTechAssestment.Api
    dotnet ef database update --connection "${{ secrets.DATABASE_CONNECTION_STRING }}"
  
- name: Deploy application
  run: |
    # Deploy após migrations bem-sucedidas
```

**Vantagens:**
- Execução controlada e auditável
- Fail-fast: falha antes do deploy se migrations falharem
- Permite validação pré-deploy

#### 2. Init Container (Kubernetes)

Use um init container para executar migrations antes do container principal iniciar:

```yaml
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      initContainers:
      - name: migrations
        image: mcr.microsoft.com/dotnet/sdk:8.0
        command: ['sh', '-c']
        args:
          - |
            dotnet tool install --global dotnet-ef
            cd /src/VSoftTechAssestment.Api
            dotnet ef database update
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
      containers:
      - name: api
        # ... container principal
```

**Vantagens:**
- Migrations executadas automaticamente antes do app iniciar
- Bloqueia o start do container principal até migrations completarem
- Ideal para zero-downtime deployments

#### 3. Script de Deploy Separado

Crie um script de deploy que execute migrations antes de iniciar a aplicação:

```bash
#!/bin/bash
# deploy.sh

set -e

echo "Running database migrations..."
cd vsoft-tech-assestment-back/VSoftTechAssestment.Api
dotnet ef database update --connection "$DATABASE_CONNECTION_STRING"

echo "Migrations completed successfully"
echo "Starting application..."

# Iniciar aplicação
```

#### 4. Job/Task Dedicado (Cloud Providers)

Em plataformas como AWS ECS, Azure Container Instances ou Google Cloud Run, crie jobs separados:

- **AWS ECS:** Use ECS Tasks para executar migrations antes do serviço
- **Azure:** Use Container Instances ou Azure Container Apps Jobs
- **GCP:** Use Cloud Run Jobs para executar migrations

#### Boas Práticas Gerais

✅ **Sempre:**
- Execute migrations em ambiente de staging antes de produção
- Tenha backups do banco antes de executar migrations destrutivas
- Documente migrations complexas e seus efeitos
- Use transações para migrations que podem ser revertidas
- Monitore o tempo de execução das migrations

✅ **Validação Pré-Deploy:**
- Teste migrations em banco de dados de staging idêntico à produção
- Valide queries de migration em volumes similares aos de produção
- Execute `dotnet ef migrations script` para revisar SQL gerado

✅ **Rollback Strategy:**
- Mantenha histórico de migrations para rollback se necessário
- Para migrations destrutivas, considere criar migrations de reversão antecipadamente
- Documente procedimento de rollback para cada release

✅ **Zero-Downtime Deployments:**
- Migrations devem ser backward-compatible quando possível
- Para mudanças breaking, use estratégia de blue-green deployment
- Execute migrations que adicionam colunas/índices antes do deploy do código novo

✅ **Segurança:**
- Use variáveis de ambiente ou secret managers para connection strings
- Limite permissões do usuário de migration ao mínimo necessário
- Audite e logue todas as execuções de migration

## Desenvolvimento

Para desenvolvimento local sem Docker, use os scripts padrão:

### Backend
```bash
cd vsoft-tech-assestment-back/VSoftTechAssestment.Api
dotnet run
```

### Frontend
```bash
cd vsoft-tech-assestment-front
npm install
npm run dev
```

## Troubleshooting

### Portas já em uso

Se alguma porta estiver em uso, altere no `docker-compose.yml` ou pare o serviço que está usando a porta.

### Limpar e reconstruir

```bash
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

### Verificar health dos serviços

```bash
docker-compose ps
```

### Acessar logs de erro específicos

```bash
docker-compose logs api | grep -i error
docker-compose logs frontend | grep -i error
```

## Comandos Úteis

```bash
# Reconstruir apenas um serviço
docker-compose build api
docker-compose up -d api

# Executar comandos dentro do container
docker-compose exec api bash
docker-compose exec postgres psql -U postgres -d vsoft_tech_assessment

# Ver uso de recursos
docker stats
```

