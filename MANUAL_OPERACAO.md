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
6. Escolha a vendedora (o sistema lembra a última usada no aparelho).
7. No crédito, escolha em quantas vezes.
8. Finalize a venda.
9. O estoque baixa automaticamente.

## Caixa do dia

1. Abra `Caixa` e informe o troco que está na gaveta.
2. Durante o dia, registre retiradas (sangria) e entradas de troco (reforço).
3. No fim do dia, conte o dinheiro da gaveta e digite em `Fechar caixa`.
4. O sistema mostra se sobrou ou faltou e guarda o fechamento no histórico.

## Crediário (fiado)

1. No `PDV`, escolha `Fiado`, a quantidade de parcelas (1x a 12x) e a data da 1ª parcela.
2. As próximas parcelas vencem de 30 em 30 dias. A prévia aparece antes de finalizar.
3. A cliente precisa estar escolhida e ter WhatsApp cadastrado (dá pra cadastrar ali mesmo).
4. No comprovante, clique em `Enviar parcelas no WhatsApp` pra mandar o resumo pra cliente.
5. Quando tiver parcela vencendo hoje, atrasada ou nos próximos 3 dias, aparece um aviso no topo do painel.
6. Em `Fiado > Lembretes de vencimento`, use `Lembrar` ou `Cobrar` pra abrir o WhatsApp com a mensagem pronta.
7. Quando a cliente pagar, clique em `Receber`, informe o valor e a forma. Pode ser parcial.
8. Pagamento de fiado em dinheiro entra na conta do caixa.

## Vendas, cancelamento e devolução

- `Vendas` mostra o histórico com filtro por período, pagamento e busca.
- `Comprovante` reimprime ou copia.
- `Cancelar` é pra venda lançada errado: pede motivo, devolve as peças ao estoque e tira a venda dos relatórios. O caixa só cancela vendas do dia; o admin cancela qualquer uma.
- `Devolver` e `Trocar` são pra quando a cliente traz a peça de volta.
- Em `Vendedoras no período` aparece quanto cada uma vendeu e a comissão.

## Vendedoras

Em `Usuários > Vendedoras`, cadastre o nome e a % de comissão. Vendedora inativa some do PDV mas continua nos relatórios.

## Contagem de estoque

1. Em `Estoque`, clique em `Começar contagem`.
2. Bipe o código da etiqueta (cada leitura soma 1) ou digite a quantidade contada.
3. A contagem fica salva no aparelho, dá pra parar e continuar depois.
4. Marque `Só diferenças` pra revisar o que não bateu.
5. Clique em `Aplicar ajustes`. Só as linhas contadas mudam.

## Clientes

- Cadastre o aniversário da cliente pra ela aparecer em `Aniversariantes` (próximos 30 dias).
- `Sumidas` lista quem comprou antes e não volta há mais de 60 dias.
- Os botões de WhatsApp já abrem com a mensagem pronta.

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
