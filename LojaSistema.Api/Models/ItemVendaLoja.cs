namespace LojaSistema.Api.Models;

public sealed class ItemVendaLoja
{
    public Guid ProdutoId { get; init; }
    public required string ProdutoNome { get; init; }
    public string? Tamanho { get; init; }
    public string? Cor { get; init; }
    public string? Modelo { get; init; }
    public int Quantidade { get; init; }
    public int QuantidadeDevolvida { get; set; }
    public decimal PrecoUnitario { get; init; }
    public decimal CustoUnitario { get; init; }
    public decimal Subtotal => Quantidade * PrecoUnitario;
    public decimal CustoTotal => Quantidade * CustoUnitario;
    public int QuantidadeLiquida => Math.Max(0, Quantidade - QuantidadeDevolvida);
    public decimal SubtotalDevolvido => QuantidadeDevolvida * PrecoUnitario;
    public decimal SubtotalLiquido => QuantidadeLiquida * PrecoUnitario;
    public decimal CustoLiquido => QuantidadeLiquida * CustoUnitario;
}
