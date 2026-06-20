# Checklist de produção - Nana Modas

Use este checklist antes de colocar a loja para vender de verdade.

## 1. Segurança e acessos

- [ ] Trocar a senha padrão do usuário `admin`.
- [ ] Criar usuários separados para `Caixa` e `Estoque`.
- [ ] Usar domínio com HTTPS ativo.
- [ ] Conferir se o painel admin não fica aberto em computador público.
- [ ] Baixar um backup manual antes de qualquer publicação.

## 2. Loja online

- [ ] Cadastrar produtos reais com preço, descrição, fotos e estoque.
- [ ] Publicar no site apenas os produtos que devem aparecer na loja virtual.
- [ ] Configurar banners, campanha e vitrines em `Imagens do site`.
- [ ] Conferir a loja no celular e no computador.
- [ ] Revisar rodapé, nome de quem criou o site e política de privacidade.

## 3. Frete e checkout

- [ ] Criar ao menos uma opção de entrega ativa.
- [ ] Testar cálculo de frete por CEP.
- [ ] Fazer um pedido teste até a tela de confirmação.
- [ ] Conferir se o estoque baixa após pedido online.
- [ ] Conferir se a sacola, cupom e observação do pedido funcionam.

## 4. Pagamento

- [ ] Configurar Pix manual com chave, nome e cidade do recebedor.
- [ ] Para Pix automático, configurar Asaas com access token e webhook secret.
- [ ] Cadastrar no Asaas a URL `https://SEU-DOMINIO/pagamentos/asaas/webhook`.
- [ ] Testar em sandbox antes de marcar modo produção.
- [ ] Fazer uma compra pequena real antes de divulgar a loja.

## 5. E-mail e atendimento

- [ ] Configurar SMTP real para aviso de pedidos.
- [ ] Enviar e-mail de teste pelo painel.
- [ ] Conferir se o cliente recebe/tem acesso ao histórico de pedidos.
- [ ] Definir canal de atendimento para dúvidas de pagamento e entrega.

## 6. Backup e servidor

- [ ] Garantir backup do diretório `LojaSistema.Api/Data`.
- [ ] Garantir backup do diretório `LojaSistema.Api/wwwroot/uploads`.
- [ ] Confirmar que o servidor reinicia automaticamente se cair.
- [ ] Confirmar que o banco SQLite está em volume persistente.
- [ ] Guardar uma cópia local do banco antes de atualizações.

## 7. Testes finais

- [ ] Login admin.
- [ ] Cadastro/edição de produto.
- [ ] Entrada de estoque.
- [ ] Venda no PDV.
- [ ] Devolução/troca no PDV.
- [ ] Pedido online.
- [ ] Confirmação de pagamento.
- [ ] Relatórios.
- [ ] Backup.
