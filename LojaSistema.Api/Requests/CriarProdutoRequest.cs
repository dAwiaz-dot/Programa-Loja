namespace LojaSistema.Api.Requests;

public sealed record CriarProdutoRequest(
    string Nome,
    Guid CategoriaId,
    string? Sku,
    decimal Preco,
    decimal Custo,
    int QuantidadeInicial,
    string? Descricao,
    string? ImagemUrl,
    IReadOnlyList<string>? ImagensExtras,
    IReadOnlyList<string>? Tamanhos,
    IReadOnlyList<string>? Cores,
    IReadOnlyList<string>? Modelos,
    IReadOnlyList<ProdutoVariacaoEstoqueRequest>? VariacoesEstoque,
    string? GuiaMedidas);
