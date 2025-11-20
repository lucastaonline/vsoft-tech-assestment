#!/bin/bash
set -e

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
if dotnet ef database update --project /app/VSoftTechAssestment.Api.csproj --no-build 2>/dev/null; then
    echo "✓ Migrations aplicadas com sucesso!"
else
    echo "⚠ Tentando aplicar migrations via Database.Migrate()..."
    # Fallback: usar Database.Migrate() se dotnet ef não funcionar
    dotnet exec /app/VSoftTechAssestment.Api.dll --migrate 2>/dev/null || echo "⚠ Migrations serão aplicadas na primeira execução da aplicação"
fi

echo "Iniciando aplicação..."
exec "$@"
