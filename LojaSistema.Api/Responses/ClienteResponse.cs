namespace LojaSistema.Api.Responses;

public sealed record ClienteResponse(
    Guid Id,
    string Nome,
    string Email,
    string? Telefone,
    DateTime CriadoEm,
    DateTime AtualizadoEm);

public sealed record ClienteAcessoResponse(
    bool Criado,
    ClienteResponse Cliente);
