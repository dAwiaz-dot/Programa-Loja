namespace LojaSistema.Api.Models;

public sealed class Fornecedor
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public string? Documento { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
