namespace LojaSistema.Api.Models;

public sealed class Categoria
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public Guid? CategoriaPaiId { get; set; }
    public bool Ativa { get; set; } = true;
    public DateTime CriadaEm { get; init; } = DateTime.UtcNow;
}
