namespace LojaSistema.Api.Requests;

public sealed record AtualizarPagamentoPedidoOnlineRequest(
    string? ReferenciaPagamento,
    string? ObservacaoPagamento,
    bool ConfirmarPagamento);
