namespace LojaSistema.Api.Responses;

public sealed record ProdutoVariacaoEstoqueResponse(
    string? Tamanho,
    string? Cor,
    string? Modelo,
    int Quantidade,
    string? Sku);
