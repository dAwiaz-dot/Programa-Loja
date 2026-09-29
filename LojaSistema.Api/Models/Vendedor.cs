namespace LojaSistema.Api.Models;

public sealed class Vendedor
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public decimal ComissaoPercentual { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
