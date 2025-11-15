# VSoft Tech Assessment - Backend

API Backend desenvolvida em .NET 8 para o desafio técnico VSoft.

## Requisitos Técnicos

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core com PostgreSQL
- ASP.NET Core Identity
- JWT Bearer Token Authentication
- Swagger/OpenAPI para documentação
- CORS configurado para comunicação com o frontend

## Estrutura do Projeto

```
vsoft-tech-assestment-back/
├── VSoftTechAssestment.Api/      # Projeto principal da API
│   ├── Controllers/              # Controladores da API
│   │   └── AuthController.cs    # Endpoints de autenticação
│   ├── Data/                     # Contexto do banco de dados
│   │   └── ApplicationDbContext.cs
│   ├── Models/                   # DTOs e modelos
│   │   └── DTOs/
│   │       └── Auth/            # DTOs de autenticação
│   ├── Properties/               # Configurações de lançamento
│   ├── Program.cs               # Configuração principal da aplicação
│   └── appsettings.json         # Configurações da aplicação
└── VSoftTechAssestment.sln      # Arquivo de solução
```

## Pré-requisitos

- .NET 8 SDK instalado
- PostgreSQL instalado e rodando
- Ferramenta Entity Framework Core CLI instalada (para migrations)

### Instalando Entity Framework Core CLI

```bash
dotnet tool install --global dotnet-ef
```

## Configuração do Banco de Dados

1. Certifique-se de que o PostgreSQL está rodando
2. Configure a connection string no `appsettings.json` e `appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=vsoft_tech_assessment;Username=postgres;Password=postgres"
   }
   ```

3. Crie a database (se ainda não existir):
   ```sql
   CREATE DATABASE vsoft_tech_assessment;
   ```

4. Crie e aplique as migrations:
   ```bash
   cd VSoftTechAssestment.Api
   dotnet ef migrations add InitialIdentityMigration
   dotnet ef database update
   ```

## Executando o Projeto

```bash
# Restaurar dependências
dotnet restore

# Compilar o projeto
dotnet build

# Executar o projeto
cd VSoftTechAssestment.Api
dotnet run
```

A API estará disponível em:
- HTTP: `http://localhost:5001`
- Swagger UI: `http://localhost:5001/swagger`

## Autenticação

A API utiliza JWT Bearer Token para autenticação.

### Endpoints de Autenticação

- **POST /api/auth/register** - Registrar novo usuário
  ```json
  {
    "email": "user@example.com",
    "userName": "username",
    "password": "Password123"
  }
  ```

- **POST /api/auth/login** - Realizar login
  ```json
  {
    "userNameOrEmail": "username",
    "password": "Password123"
  }
  ```
  
  Retorna um token JWT que deve ser usado no header `Authorization: Bearer {token}` em requisições subsequentes.

### Configuração JWT

As configurações JWT estão em `appsettings.json`:
- `Jwt:Secret` - Chave secreta para assinar tokens (mínimo 32 caracteres)
- `Jwt:Issuer` - Emissor do token
- `Jwt:Audience` - Público do token
- `Jwt:ExpirationMinutes` - Tempo de expiração do token (padrão: 60 minutos)

### Swagger com Autenticação

No Swagger UI, clique no botão **"Authorize"** no topo da página e insira o token no formato:
```
Bearer {seu_token_jwt}
```

## Configuração

O projeto está configurado com:
- CORS habilitado para permitir comunicação com o frontend (localhost:5173 e localhost:3000)
- Swagger habilitado em ambiente de desenvolvimento com suporte a JWT
- Logging configurado via appsettings.json
- Identity configurado com regras de senha:
  - Mínimo 6 caracteres
  - Requer dígito, letra minúscula e maiúscula
  - Não requer caracteres especiais

