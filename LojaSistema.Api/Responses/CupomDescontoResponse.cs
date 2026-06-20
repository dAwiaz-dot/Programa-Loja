namespace LojaSistema.Api.Responses;

public sealed record CupomDescontoResponse(
    Guid Id,
    string Codigo,
    string? Descricao,
    decimal PercentualDesconto,
    decimal ValorMinimoPedido,
    bool Ativo,
    bool DisponivelNaLoja,
    DateTime? ValidoAte,
    DateTime CriadoEm,
    DateTime AtualizadoEm);

public sealed record CupomLojaResponse(
    string Codigo,
    string? Descricao,
    decimal PercentualDesconto,
    decimal ValorMinimoPedido,
    DateTime? ValidoAte);
