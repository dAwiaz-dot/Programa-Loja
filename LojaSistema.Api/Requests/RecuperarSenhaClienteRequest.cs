namespace LojaSistema.Api.Requests;

public sealed record RecuperarSenhaClienteRequest(string Email);

public sealed record RedefinirSenhaClienteRequest(
    string Email,
    string Codigo,
    string NovaSenha);
