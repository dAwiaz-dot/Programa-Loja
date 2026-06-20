namespace LojaSistema.Api.Requests;

public sealed record CriarUsuarioPainelRequest(
    string? Usuario,
    string? NomeExibicao,
    string? Perfil,
    string? Senha,
    bool Ativo);

public sealed record AtualizarUsuarioPainelRequest(
    string? Usuario,
    string? NomeExibicao,
    string? Perfil,
    string? Senha,
    bool Ativo);
