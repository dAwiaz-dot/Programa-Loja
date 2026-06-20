namespace LojaSistema.Api.Models;

public sealed class ItemPedidoOnline
{
    public Guid ProdutoId { get; init; }
    public required string ProdutoNome { get; init; }
    public string? Tamanho { get; init; }
    public string? Cor { get; init; }
    public string? Modelo { get; init; }
    public int Quantidade { get; init; }
    public decimal PrecoUnitario { get; init; }
    public decimal CustoUnitario { get; init; }
    public decimal Subtotal => Quantidade * PrecoUnitario;
    public decimal CustoTotal => Quantidade * CustoUnitario;
}
