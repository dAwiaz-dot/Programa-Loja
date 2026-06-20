namespace LojaSistema.Api.Requests;

public sealed record AtualizarVitrineProdutoRequest(
    bool PublicadoNaLoja,
    bool DestaqueLoja,
    int OrdemLoja,
    string? NomeLoja,
    string? DescricaoLoja,
    decimal? PrecoLoja,
    string? ImagemLojaUrl,
    IReadOnlyList<string>? ImagensLojaExtras);
