namespace LojaSistema.Api.Models;

public sealed class Cliente
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public string? Telefone { get; set; }
    public required string SenhaHash { get; set; }
    public string? CodigoRecuperacaoHash { get; set; }
    public DateTime? CodigoRecuperacaoExpiraEm { get; set; }
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
