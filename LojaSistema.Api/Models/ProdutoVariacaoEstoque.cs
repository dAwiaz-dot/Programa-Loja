namespace LojaSistema.Api.Models;

public sealed class ProdutoVariacaoEstoque
{
    public string? Tamanho { get; set; }
    public string? Cor { get; set; }
    public string? Modelo { get; set; }
    public int Quantidade { get; set; }
    public string? Sku { get; set; }
}
