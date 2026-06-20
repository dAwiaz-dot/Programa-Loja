namespace LojaSistema.Api.Services;

public sealed record GatewayPagamentoConfiguracao(
    string Provedor,
    bool Ativo,
    bool Producao,
    string AccessToken,
    string WebhookSecret);

public sealed record PagamentoPixGateway(
    string Provedor,
    string PagamentoId,
    string Status,
    string PixCopiaECola,
    string? PixQrCodeBase64,
    DateTime? PixExpiraEm,
    string? UrlPagamento);
