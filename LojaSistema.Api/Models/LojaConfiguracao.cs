namespace LojaSistema.Api.Models;

public sealed class LojaConfiguracao
{
    public string NomeCriadorSite { get; set; } = "Davi Silva Dias";
    public string RazaoSocial { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string SiteUrlCanonica { get; set; } = "";
    public string PoliticaPrivacidade { get; set; } = "A Nana Modas coleta, no checkout e na criação de conta, os dados necessários para identificar o cliente, confirmar o pedido, calcular frete, combinar a entrega e prestar atendimento (nome, e-mail, telefone, CPF/CNPJ e endereço). Esses dados não são vendidos nem compartilhados com terceiros para fins comerciais. São usados apenas pela Nana Modas e pelos serviços que processam o pedido (pagamento e entrega). Você pode solicitar a qualquer momento a confirmação, correção ou exclusão dos seus dados pelos canais de atendimento da loja, conforme a Lei Geral de Proteção de Dados (LGPD - Lei 13.709/2018).";
    public string PoliticaTrocaDevolucao { get; set; } = "Você pode desistir da compra em até 7 dias corridos após o recebimento, sem precisar justificar o motivo, conforme o art. 49 do Código de Defesa do Consumidor. Nesse caso, o valor pago é reembolsado integralmente, incluindo o frete. Para trocas por tamanho/cor ou devolução por defeito, entre em contato pelos canais de atendimento da loja informando o número do pedido; o produto deve estar sem uso, com etiqueta e embalagem originais.";
    public string GoogleAnalyticsId { get; set; } = "";
    public string MetaPixelId { get; set; } = "";
    public bool BackupEmailAtivo { get; set; }
    public string BackupEmailDestino { get; set; } = "";
    public decimal FreteValorPadrao { get; set; } = 19.90m;
    public decimal FreteGratisAcimaDe { get; set; } = 399m;
    public int PrazoMinimoDias { get; set; } = 3;
    public int PrazoMaximoDias { get; set; } = 7;
    public string MensagemFrete { get; set; } = "O frete é calculado como estimativa. A loja confirma o valor final e o prazo pelo atendimento após o pedido.";
    public string MensagemLoginCliente { get; set; } = "Entre para salvar seus dados neste dispositivo e deixar o checkout mais rápido.";
    public string BannerEyebrow { get; set; } = "Coleção pronta entrega";
    public string BannerTitulo { get; set; } = "Nana Modas";
    public string BannerDescricao { get; set; } = "Peças selecionadas, estética premium e compra online integrada ao estoque da loja física.";
    public string BannerBotaoPrimario { get; set; } = "Ver coleção";
    public string BannerBotaoSecundario { get; set; } = "Minha sacola";
    public string BannerImagemUrl { get; set; } = "";
    public string PromocaoTopoTexto { get; set; } = "Compra segura Nana Modas: estoque real, atendimento direto e pagamento por Pix ou cartão.";
    public string CampanhaTitulo { get; set; } = "Coleção premium pronta entrega";
    public string CampanhaDescricao { get; set; } = "Banners, promoções e vitrines podem ser trocados no painel sem mexer nas fotos dos produtos.";
    public string CampanhaBotaoTexto { get; set; } = "Ver novidades";
    public string CampanhaImagemUrl { get; set; } = "";
    public string VitrineImagem1Url { get; set; } = "";
    public string VitrineImagem1Titulo { get; set; } = "Novidades";
    public string VitrineImagem2Url { get; set; } = "";
    public string VitrineImagem2Titulo { get; set; } = "Promoções";
    public string VitrineImagem3Url { get; set; } = "";
    public string VitrineImagem3Titulo { get; set; } = "Mais desejados";
    public string WhatsappLoja { get; set; } = "";
    public string InstagramLoja { get; set; } = "";
    public string EnderecoLoja { get; set; } = "";
    public string PixChave { get; set; } = "Configure a chave Pix no painel";
    public string PixNomeRecebedor { get; set; } = "NANA MODAS";
    public string PixCidade { get; set; } = "SAO PAULO";
    public bool PixOnlineAtivo { get; set; } = true;
    public bool CartaoOnlineAtivo { get; set; } = true;
    public string CheckoutCartaoNome { get; set; } = "Link de pagamento";
    public string CheckoutCartaoUrl { get; set; } = "";
    public string MensagemPagamento { get; set; } = "Após finalizar o pedido, envie o comprovante pelo WhatsApp da loja para confirmação.";
    public string MensagemPagamentoCartao { get; set; } = "Finalize o pedido e use o link de pagamento, ou aguarde a loja enviar a cobrança.";
    public bool EmailNotificacoesAtivo { get; set; }
    public string EmailProvedor { get; set; } = "Brevo";
    public string EmailRemetente { get; set; } = "";
    public string EmailPedidosDestino { get; set; } = "";
    public string BrevoApiKey { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPorta { get; set; } = 587;
    public string SmtpUsuario { get; set; } = "";
    public string SmtpSenha { get; set; } = "";
    public bool SmtpSsl { get; set; } = true;
    public bool BackupAutomaticoAtivo { get; set; } = true;
    public int BackupIntervaloHoras { get; set; } = 24;
    public DateTime? BackupUltimoEm { get; set; }
    public string GatewayPagamentoProvedor { get; set; } = "";
    public bool GatewayPagamentoAtivo { get; set; }
    public bool GatewayPagamentoProducao { get; set; }
    public string GatewayPagamentoPublicKey { get; set; } = "";
    public string GatewayPagamentoAccessToken { get; set; } = "";
    public string GatewayPagamentoWebhookSecret { get; set; } = "";
    public string GatewayPagamentoWebhookUrl { get; set; } = "";
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
