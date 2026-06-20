namespace LojaSistema.Api.Models;

public sealed class VendaLoja
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required FormaPagamento FormaPagamento { get; init; }
    public required IReadOnlyList<ItemVendaLoja> Itens { get; init; }
    public decimal Desconto { get; init; }
    public decimal ValorRecebido { get; init; }
    public string? Observacao { get; init; }
    public bool Devolvida { get; set; }
    public DateTime? DevolvidaEm { get; set; }
    public string? MotivoDevolucao { get; set; }
    public decimal TotalBruto => Itens.Sum(item => item.Subtotal);
    public decimal TotalBrutoDevolvido => Itens.Sum(item => item.SubtotalDevolvido);
    public decimal TotalBrutoLiquido => Itens.Sum(item => item.SubtotalLiquido);
    public decimal DescontoDevolvido => CalcularProporcional(Desconto, TotalBrutoDevolvido, TotalBruto);
    public decimal DescontoLiquido => Math.Max(0, Desconto - DescontoDevolvido);
    public decimal ValorDevolvido => Math.Max(0, TotalBrutoDevolvido - DescontoDevolvido);
    public decimal TotalOriginal => Math.Max(0, TotalBruto - Desconto);
    public decimal Total => Math.Max(0, TotalBrutoLiquido - DescontoLiquido);
    public decimal Troco => Math.Max(0, ValorRecebido - TotalOriginal);
    public bool DevolucaoParcial => !Devolvida && Itens.Any(item => item.QuantidadeDevolvida > 0);
    public DateTime CriadaEm { get; init; } = DateTime.UtcNow;

    private static decimal CalcularProporcional(decimal valor, decimal parte, decimal total)
    {
        if (valor <= 0 || parte <= 0 || total <= 0)
        {
            return 0;
        }

        return Math.Min(valor, Math.Round(valor * (parte / total), 2, MidpointRounding.AwayFromZero));
    }
}
