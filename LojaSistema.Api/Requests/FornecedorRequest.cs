namespace LojaSistema.Api.Requests;

public sealed record CriarFornecedorRequest(
    string Nome,
    string? Documento,
    string? Telefone,
    string? Email);

public sealed record AtualizarFornecedorRequest(
    string Nome,
    string? Documento,
    string? Telefone,
    string? Email,
    bool Ativo);
