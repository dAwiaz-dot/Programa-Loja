namespace LojaSistema.Api.Requests;

public sealed record OpcaoEntregaRequest(
    string Nome,
    string Tipo,
    string? Descricao,
    decimal Valor,
    decimal FreteGratisAcimaDe,
    int PrazoMinimoDias,
    int PrazoMaximoDias,
    string? CepInicial,
    string? CepFinal,
    IReadOnlyList<string>? Cidades,
    IReadOnlyList<string>? Bairros,
    IReadOnlyList<string>? Estados,
    bool Ativo,
    int Ordem);
