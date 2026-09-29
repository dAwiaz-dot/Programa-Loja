namespace LojaSistema.Api.Responses;

public sealed record ClienteSimplesResponse(
    Guid Id,
    string Nome,
    string? Telefone,
    string? DataNascimento = null,
    int ComprasLoja = 0,
    decimal TotalLoja = 0,
    DateTime? UltimaCompraEm = null,
    decimal SaldoFiado = 0);
