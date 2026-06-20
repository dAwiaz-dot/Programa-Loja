namespace LojaSistema.Api.Responses;

public sealed record BackupArquivoResponse(
    string NomeArquivo,
    long TamanhoBytes,
    DateTime CriadoEm,
    bool Automatico);
