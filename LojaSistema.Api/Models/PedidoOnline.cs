namespace LojaSistema.Api.Models;

public sealed class PedidoOnline
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string NomeCliente { get; init; }
    public required string EmailCliente { get; init; }
    public string? TelefoneCliente { get; init; }
    public string? DocumentoCliente { get; init; }
    public Guid? ClienteId { get; init; }
    public required string EnderecoEntrega { get; init; }
    public string? CepEntrega { get; init; }
    public string? RuaEntrega { get; init; }
    public string? NumeroEntrega { get; init; }
    public string? ComplementoEntrega { get; init; }
    public string? BairroEntrega { get; init; }
    public string? CidadeEntrega { get; init; }
    public string? EstadoEntrega { get; init; }
    public string? Observacao { get; init; }
    public string? CupomCodigo { get; init; }
    public decimal Desconto { get; init; }
    public Guid? OpcaoEntregaId { get; init; }
    public string? EntregaNome { get; init; }
    public decimal EntregaValor { get; init; }
    public int? EntregaPrazoMinimoDias { get; init; }
    public int? EntregaPrazoMaximoDias { get; init; }
    public string? CodigoRastreio { get; set; }
    public string? ObservacaoEntrega { get; set; }
    public DateTime? RastreamentoAtualizadoEm { get; set; }
    public string? ReferenciaPagamento { get; set; }
    public string? ObservacaoPagamento { get; set; }
    public string? GatewayPagamentoProvedor { get; set; }
    public string? GatewayPagamentoId { get; set; }
    public string? GatewayPagamentoStatus { get; set; }
    public string? PixCopiaECola { get; set; }
    public string? PixQrCodeBase64 { get; set; }
    public DateTime? PixExpiraEm { get; set; }
    public string? UrlPagamento { get; set; }
    public DateTime? PagamentoAtualizadoEm { get; set; }
    public DateTime? PagamentoConfirmadoEm { get; set; }
    public required FormaPagamento FormaPagamento { get; init; }
    public StatusPedidoOnline Status { get; set; } = StatusPedidoOnline.Recebido;
    public required IReadOnlyList<ItemPedidoOnline> Itens { get; init; }
    public decimal TotalBruto => Itens.Sum(item => item.Subtotal);
    public decimal TotalProdutos => Math.Max(0, TotalBruto - Desconto);
    public decimal Total => TotalProdutos + EntregaValor;
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
}
