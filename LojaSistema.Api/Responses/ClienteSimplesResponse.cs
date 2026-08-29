namespace LojaSistema.Api.Responses;

public sealed record ClienteSimplesResponse(
    Guid Id,
    string Nome,
    string? Telefone);
