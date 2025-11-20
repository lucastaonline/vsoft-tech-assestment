#!/bin/bash
set -e

# Se migrations falharem, não continuar (fail fast)
set -o errexit

echo "=========================================="
echo "Aplicando migrations do banco de dados..."
echo "NOTA: Isso é apenas para facilitar a avaliação do teste."
echo "Em produção, migrations devem ser executadas manualmente."
echo "=========================================="

# Aguardar banco de dados estar pronto
echo "Aguardando banco de dados estar pronto..."
until PGPASSWORD=${POSTGRES_PASSWORD:-postgres} psql -h postgres -U ${POSTGRES_USER:-postgres} -d ${POSTGRES_DB:-vsoft_tech_assessment} -c '\q' 2>/dev/null; do
  echo "Banco de dados não está pronto ainda. Aguardando..."
  sleep 2
done

echo "Banco de dados está pronto!"

# Aplicar migrations usando dotnet ef database update
# O SDK está disponível no container para facilitar a avaliação
echo "Aplicando migrations..."

# Garantir que o PATH inclui as ferramentas dotnet
export PATH="$PATH:/root/.dotnet/tools"

# Verificar se o arquivo .csproj existe
if [ ! -f "/app/VSoftTechAssestment.Api.csproj" ]; then
    echo "❌ Erro: Arquivo .csproj não encontrado em /app/"
    echo "❌ Não é possível aplicar migrations sem o arquivo do projeto."
    exit 1
fi

# Aplicar migrations com dotnet ef
# Primeiro restaurar dependências (necessário para dotnet ef)
echo "Restaurando dependências do projeto..."
cd /app

# Configurar connection string para o dotnet ef
export ConnectionStrings__DefaultConnection="${DATABASE_CONNECTION_STRING:-Host=postgres;Port=5432;Database=vsoft_tech_assessment;Username=postgres;Password=postgres}"

if ! dotnet restore VSoftTechAssestment.Api.csproj; then
    echo "⚠ Aviso: Não foi possível restaurar dependências. Tentando continuar mesmo assim..."
fi

echo "Executando: dotnet ef database update --project /app/VSoftTechAssestment.Api.csproj"
if dotnet ef database update --project /app/VSoftTechAssestment.Api.csproj; then
    echo "✓ Migrations aplicadas com sucesso!"
else
    echo "❌ Erro ao aplicar migrations com dotnet ef."
    echo "❌ Verifique os logs acima para mais detalhes."
    exit 1
fi

echo "Iniciando aplicação..."
exec "$@"
