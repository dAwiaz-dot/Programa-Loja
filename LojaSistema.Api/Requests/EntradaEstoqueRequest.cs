namespace LojaSistema.Api.Requests;

public sealed record EntradaEstoqueRequest(
    int Quantidade,
    string? Observacao,
    Guid? FornecedorId,
    decimal? CustoUnitario,
    string? Documento,
    string? Tamanho,
    string? Cor,
    string? Modelo);
