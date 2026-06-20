namespace LojaSistema.Api.Responses;

public sealed record UsuarioPainelResponse(
    Guid Id,
    string Usuario,
    string NomeExibicao,
    string Perfil,
    bool Ativo,
    DateTime CriadoEm,
    DateTime AtualizadoEm);
