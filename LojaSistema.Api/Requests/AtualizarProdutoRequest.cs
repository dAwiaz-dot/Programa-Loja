namespace LojaSistema.Api.Requests;

public sealed record AtualizarProdutoRequest(
    string Nome,
    Guid CategoriaId,
    string? Sku,
    decimal Preco,
    decimal Custo,
    bool Ativo,
    string? Descricao,
    string? ImagemUrl,
    IReadOnlyList<string>? ImagensExtras,
    IReadOnlyList<string>? Tamanhos,
    IReadOnlyList<string>? Cores,
    IReadOnlyList<string>? Modelos,
    IReadOnlyList<ProdutoVariacaoEstoqueRequest>? VariacoesEstoque,
    string? GuiaMedidas);
