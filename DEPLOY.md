# Deploy Nana Modas

Este guia prepara a publicação do sistema em um servidor real.

## 1. Antes de publicar

1. Rode localmente e faça um pedido online de teste.
2. Faça uma venda no PDV de teste.
3. Baixe um backup manual.
4. Troque a senha padrão do admin.
5. Configure produtos, imagens, frete, política de privacidade e rodapé.

## 2. Hospedagem recomendada

Para começar simples:

- VPS com Docker e Nginx.
- Render/Railway/Fly.io com volume persistente.
- Servidor Windows/Linux com runtime .NET 10 instalado.

Evite hospedagem apenas estática, porque o sistema precisa de backend, banco SQLite, uploads, login e PDV.

## 3. Domínio

Você pode separar domínio/subdomínio:

- Loja: `https://nanamodas.com.br`
- Painel: `https://admin.nanamodas.com.br`

No MVP atual, loja e painel rodam no mesmo aplicativo. A separação por subdomínio pode ser feita no proxy/servidor apontando para o mesmo app, mas o ideal futuro é criar regra para painel responder apenas no subdomínio admin.

## 4. Dados persistentes

Preserve sempre:

- `LojaSistema.Api/Data`
- `LojaSistema.Api/wwwroot/uploads`

Se usar Docker, monte volumes:

```bash
docker run \
  -p 8080:8080 \
  -v nana-data:/app/Data \
  -v nana-uploads:/app/wwwroot/uploads \
  nana-modas
```

## 5. Docker

Build:

```bash
docker build -t nana-modas .
```

Run:

```bash
docker run -p 8080:8080 nana-modas
```

Depois coloque Nginx/Caddy/Cloudflare na frente para HTTPS e domínio.

## 6. Pagamento Asaas

No painel:

1. Abra `Site da loja`.
2. Selecione `Asaas` em provedor de pagamento.
3. Cole o access token.
4. Defina um segredo de webhook.
5. Informe a URL pública: `https://SEU-DOMINIO/pagamentos/asaas/webhook`.
6. Primeiro teste sem marcar `Modo produção`.
7. Depois de validar, marque produção e use token de produção.

O webhook agora exige segredo configurado para aceitar confirmação automática.

Referências oficiais:

- QR Code Pix Asaas: `https://docs.asaas.com/reference/obter-qr-code-para-pagamentos-via-pix`
- Webhooks Asaas: `https://docs.asaas.com/docs/webhooks`

## 7. E-mail

Configure SMTP real para:

- avisos de pedido;
- recuperação de senha de cliente;
- acompanhamento operacional.

Depois use o botão `Testar e-mail` no painel.

## 8. Checklist final

Use `CHECKLIST_PRODUCAO.md` antes de divulgar a loja.
