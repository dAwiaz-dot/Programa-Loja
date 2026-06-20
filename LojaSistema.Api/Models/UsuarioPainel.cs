namespace LojaSistema.Api.Models;

public sealed class UsuarioPainel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Usuario { get; set; } = string.Empty;
    public string NomeExibicao { get; set; } = string.Empty;
    public string Perfil { get; set; } = "Caixa";
    public string SenhaHash { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
