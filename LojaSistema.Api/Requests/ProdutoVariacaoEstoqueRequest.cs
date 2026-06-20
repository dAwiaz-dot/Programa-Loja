namespace LojaSistema.Api.Requests;

public sealed record ProdutoVariacaoEstoqueRequest(
    string? Tamanho,
    string? Cor,
    string? Modelo,
    int Quantidade);
