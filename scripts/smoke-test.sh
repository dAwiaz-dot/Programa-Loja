#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5057}"

echo "== Build backend =="
dotnet build LojaSistema.Api/LojaSistema.Api.csproj

echo "== Check JavaScript =="
node --check LojaSistema.Api/wwwroot/app.js
node --check LojaSistema.Api/wwwroot/loja.js
node --check LojaSistema.Api/wwwroot/login.js

echo "== Check public endpoints =="
curl -fsS "$BASE_URL/loja-api/produtos" >/dev/null
curl -fsS "$BASE_URL/loja-api/categorias" >/dev/null
curl -fsS "$BASE_URL/loja-api/configuracao" >/dev/null
curl -fsS "$BASE_URL/loja-api/entregas" >/dev/null

echo "Smoke test OK em $BASE_URL"
