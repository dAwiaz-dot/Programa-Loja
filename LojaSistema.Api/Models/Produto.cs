namespace LojaSistema.Api.Models;

public sealed class Produto
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public Guid CategoriaId { get; set; }
    public string? Sku { get; set; }
    public string? Descricao { get; set; }
    public string? ImagemUrl { get; set; }
    public List<string> ImagensExtras { get; set; } = [];
    public List<string> Tamanhos { get; set; } = [];
    public List<string> Cores { get; set; } = [];
    public List<string> Modelos { get; set; } = [];
    public List<ProdutoVariacaoEstoque> VariacoesEstoque { get; set; } = [];
    public string? GuiaMedidas { get; set; }
    public decimal Custo { get; set; }
    public bool PublicadoNaLoja { get; set; }
    public bool DestaqueLoja { get; set; }
    public int OrdemLoja { get; set; }
    public string? NomeLoja { get; set; }
    public string? DescricaoLoja { get; set; }
    public decimal? PrecoLoja { get; set; }
    public string? ImagemLojaUrl { get; set; }
    public List<string> ImagensLojaExtras { get; set; } = [];
    public decimal Preco { get; set; }
    public int QuantidadeEmEstoque { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; init; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
