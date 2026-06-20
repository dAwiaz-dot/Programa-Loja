namespace LojaSistema.Api.Requests;

public sealed record AcessarClienteRequest(
    string Nome,
    string Email,
    string? Telefone,
    string Senha);
