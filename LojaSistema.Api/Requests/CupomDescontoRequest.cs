namespace LojaSistema.Api.Requests;

public sealed record CupomDescontoRequest(
    string Codigo,
    string? Descricao,
    decimal PercentualDesconto,
    decimal ValorMinimoPedido,
    bool Ativo,
    DateTime? ValidoAte);
