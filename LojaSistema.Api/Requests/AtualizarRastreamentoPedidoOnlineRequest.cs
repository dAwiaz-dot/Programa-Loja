namespace LojaSistema.Api.Requests;

public sealed record AtualizarRastreamentoPedidoOnlineRequest(
    string? CodigoRastreio,
    string? ObservacaoEntrega);
