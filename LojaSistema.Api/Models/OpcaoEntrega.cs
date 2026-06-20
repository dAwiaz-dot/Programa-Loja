namespace LojaSistema.Api.Models;

public sealed class OpcaoEntrega
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public required string Tipo { get; set; }
    public string? Descricao { get; set; }
    public decimal Valor { get; set; }
    public decimal FreteGratisAcimaDe { get; set; }
    public int PrazoMinimoDias { get; set; }
    public int PrazoMaximoDias { get; set; }
    public string? CepInicial { get; set; }
    public string? CepFinal { get; set; }
    public List<string> Cidades { get; set; } = [];
    public List<string> Bairros { get; set; } = [];
    public List<string> Estados { get; set; } = [];
    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; }
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
