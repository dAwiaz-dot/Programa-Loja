namespace LojaSistema.Api.Requests;

public sealed record CriarClientePainelRequest(
    string Nome,
    string? Telefone,
    string? Email);
