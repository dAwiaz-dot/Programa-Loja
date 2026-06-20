# Manual rápido - Nana Modas

## Links locais

- Painel, PDV e estoque: `http://localhost:5057/`
- Loja online: `http://localhost:5057/loja.html#inicio`

## Ordem recomendada de configuração

1. Entre no painel com o usuário admin.
2. Troque a senha padrão e crie usuários de caixa/estoque.
3. Cadastre categorias.
4. Cadastre produtos e estoque.
5. Na aba `Site da loja`, escolha quais produtos aparecem online.
6. Na aba `Imagens do site`, configure banners, campanha e vitrines.
7. Configure entrega, cupom, Pix, cartão e política de privacidade.
8. Faça um pedido online de teste.
9. Faça uma venda no PDV de teste.
10. Baixe um backup.

## Como publicar produto no site

1. Abra `Produtos` e cadastre o produto normalmente.
2. Abra `Site da loja`.
3. Selecione o produto.
4. Marque para publicar no site.
5. Ajuste nome, preço, descrição e fotos específicas da loja online.
6. Salve.

## Como configurar banners do site

1. Abra `Imagens do site`.
2. Suba uma imagem principal para o banner.
3. Configure imagem da campanha.
4. Configure até três vitrines.
5. Salve e abra a loja online para conferir.

## Como usar o PDV

1. Abra `PDV`.
2. Pesquise o produto.
3. Adicione ao carrinho.
4. Escolha forma de pagamento.
5. Aplique desconto, se necessário.
6. Finalize a venda.
7. O estoque baixa automaticamente.

## Como acompanhar pedidos online

1. Abra `Pedidos online`.
2. Veja dados do cliente, itens, entrega e pagamento.
3. Confirme pagamento manualmente se não estiver usando gateway.
4. Atualize status: recebido, pago, separando, enviado, entregue.
5. Use rastreio/observação quando necessário.

## Pagamento real com Asaas

Para usar Pix QR Code gerado na hora:

1. Crie/acesse a conta Asaas.
2. Gere o access token no ambiente de teste.
3. No painel da Nana Modas, em configurações do site, selecione `Asaas`.
4. Cole o access token.
5. Defina um segredo de webhook.
6. Cadastre no Asaas a URL do webhook do seu domínio.
7. Faça um pedido teste.
8. Depois de validar, repita no ambiente de produção.

Sem Asaas ativo, o sistema continua funcionando com Pix manual/link de cartão.

Referências oficiais:

- `https://docs.asaas.com/reference/obter-qr-code-para-pagamentos-via-pix`
- `https://docs.asaas.com/docs/webhooks`
