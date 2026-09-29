namespace LojaSistema.Api.Models;

public sealed class CaixaSessao
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime AbertoEm { get; init; } = DateTime.UtcNow;
    public required string AbertoPor { get; init; }
    public decimal ValorInicial { get; init; }
    public string? ObservacaoAbertura { get; init; }
    public DateTime? FechadoEm { get; set; }
    public string? FechadoPor { get; set; }
    public decimal? ValorContado { get; set; }
    public decimal? ValorEsperado { get; set; }
    public string? ObservacaoFechamento { get; set; }
    public List<CaixaMovimento> Movimentos { get; init; } = [];
    public bool Aberto => FechadoEm is null;
}

public sealed class CaixaMovimento
{
    public Guid Id { get; init; } = Guid.NewGuid();
    // "Sangria" (retirada) ou "Reforco" (entrada de troco)
    public required string Tipo { get; init; }
    public decimal Valor { get; init; }
    public string? Motivo { get; init; }
    public required string Usuario { get; init; }
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
}
