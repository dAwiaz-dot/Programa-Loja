namespace LojaSistema.Api.Models;

public sealed class CupomDesconto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Codigo { get; set; }
    public string? Descricao { get; set; }
    public decimal PercentualDesconto { get; set; }
    public decimal ValorMinimoPedido { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime? ValidoAte { get; set; }
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
