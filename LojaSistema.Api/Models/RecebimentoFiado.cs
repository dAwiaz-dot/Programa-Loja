namespace LojaSistema.Api.Models;

public sealed class RecebimentoFiado
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ClienteId { get; init; }
    public required string ClienteNome { get; init; }
    public decimal Valor { get; init; }
    public FormaPagamento FormaPagamento { get; init; }
    public string? Observacao { get; init; }
    public required string Usuario { get; init; }
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
}
