namespace LojaSistema.Api.Requests;

public sealed record CriarClientePainelRequest(
    string Nome,
    string? Telefone,
    string? Email,
    string? DataNascimento = null);

public sealed record AtualizarClientePainelRequest(
    string Nome,
    string? Telefone,
    string? Email,
    string? DataNascimento);
