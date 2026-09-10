namespace LojaSistema.Api.Responses;

public sealed record NotaFiscalItemPreview(
    string Codigo,
    string Descricao,
    string? Ncm,
    string Unidade,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal ValorTotal,
    Guid? ProdutoSugeridoId,
    string? ProdutoSugeridoNome);

public sealed record NotaFiscalPreviewResponse(
    string? FornecedorNome,
    string? FornecedorCnpj,
    string? NumeroNota,
    string? Serie,
    IReadOnlyList<NotaFiscalItemPreview> Itens);
