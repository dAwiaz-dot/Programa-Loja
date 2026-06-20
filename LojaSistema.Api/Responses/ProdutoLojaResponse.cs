namespace LojaSistema.Api.Responses;

public sealed record ProdutoLojaResponse(
    Guid Id,
    string Nome,
    Guid CategoriaId,
    string Categoria,
    string? Sku,
    decimal Preco,
    int QuantidadeEmEstoque,
    bool Ativo,
    string? Descricao,
    string? ImagemUrl,
    IReadOnlyList<string> ImagensExtras,
    IReadOnlyList<string> Tamanhos,
    IReadOnlyList<string> Cores,
    IReadOnlyList<string> Modelos,
    IReadOnlyList<ProdutoVariacaoEstoqueResponse> VariacoesEstoque,
    string? GuiaMedidas,
    bool DestaqueLoja,
    int OrdemLoja);
