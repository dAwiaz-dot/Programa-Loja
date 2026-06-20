namespace LojaSistema.Api.Models;

public sealed class EstoqueMovimentacao
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProdutoId { get; init; }
    public required string ProdutoNome { get; init; }
    public TipoMovimentacaoEstoque Tipo { get; init; }
    public int Quantidade { get; init; }
    public int EstoqueAposMovimento { get; init; }
    public required string Origem { get; init; }
    public Guid? VendaLojaId { get; init; }
    public Guid? PedidoOnlineId { get; init; }
    public Guid? FornecedorId { get; init; }
    public string? FornecedorNome { get; init; }
    public decimal? CustoUnitario { get; init; }
    public string? Documento { get; init; }
    public DateTime CriadaEm { get; init; } = DateTime.UtcNow;
}
