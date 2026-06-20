namespace LojaSistema.Api.Responses;

public sealed record OpcaoEntregaResponse(
    Guid Id,
    string Nome,
    string Tipo,
    string? Descricao,
    decimal Valor,
    decimal FreteGratisAcimaDe,
    int PrazoMinimoDias,
    int PrazoMaximoDias,
    string? CepInicial,
    string? CepFinal,
    IReadOnlyList<string> Cidades,
    IReadOnlyList<string> Bairros,
    IReadOnlyList<string> Estados,
    bool Ativo,
    bool DisponivelNaLoja,
    int Ordem,
    DateTime CriadoEm,
    DateTime AtualizadoEm);
