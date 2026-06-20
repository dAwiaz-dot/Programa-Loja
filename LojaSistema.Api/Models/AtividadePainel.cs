namespace LojaSistema.Api.Models;

public sealed class AtividadePainel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Usuario { get; init; }
    public required string Acao { get; init; }
    public string? Detalhe { get; init; }
    public DateTime CriadaEm { get; init; } = DateTime.UtcNow;
}
