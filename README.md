# Nana Modas

Sistema de loja online, PDV e controle de estoque em ASP.NET Core.

## O que o sistema tem

- Painel administrativo protegido por login.
- Perfis de usuário: admin, caixa e estoque.
- Cadastro de produtos, categorias, fornecedores e variações.
- Controle de estoque com entrada, baixa e movimentações.
- PDV para venda presencial, desconto, troca, devolução e comprovante.
- Loja online pública com home, catálogo, produto individual, sacola e checkout.
- Login de cliente com histórico de pedidos.
- Configuração de banners, campanha e vitrines do site.
- Frete, cupons, política de privacidade e rodapé configuráveis.
- Pedido online com baixa automática de estoque.
- Pix manual, link de cartão e preparação para Pix automático via Asaas.
- Relatórios e backups.

## Como executar localmente

```bash
dotnet run --project LojaSistema.Api/LojaSistema.Api.csproj --urls http://localhost:5057
```

Depois abra:

- Painel: `http://localhost:5057/`
- Loja: `http://localhost:5057/loja.html#inicio`
- Saúde da API: `http://localhost:5057/health`

## Login inicial

O usuário padrão é `admin` (e também existem `caixa` e `estoque`). A senha inicial de
cada um está definida em `CriarUsuariosPainelPadrao` (`LojaSistema.Api/Services/LojaService.cs`)
e só é usada na primeira vez que o sistema roda com um banco vazio.

**Troque as três senhas pelo painel assim que fizer o primeiro login, em cada ambiente
(local, produção etc).** Como este é um repositório público, qualquer senha padrão
commitada aqui deve ser considerada pública — não é suficiente trocar só depois de
publicar, ela já nasce comprometida.

## Arquivos importantes

- Banco SQLite: `LojaSistema.Api/Data/nana-modas.db`
- Backups: `LojaSistema.Api/Data/backups/`
- Uploads: `LojaSistema.Api/wwwroot/uploads/`
- Checklist: `CHECKLIST_PRODUCAO.md`
- Manual rápido: `MANUAL_OPERACAO.md`
- Deploy: `DEPLOY.md`

## Publicação

O projeto pode rodar em VPS, Render, Railway, Fly.io ou outro servidor que suporte ASP.NET Core/.NET 10.

Para Docker:

```bash
docker build -t nana-modas .
docker run -p 8080:8080 -v nana-data:/app/Data -v nana-uploads:/app/wwwroot/uploads nana-modas
```

Em produção, use HTTPS, volume persistente para `Data` e `wwwroot/uploads`, backup externo e senha admin nova.
