namespace LojaSistema.Api.Services;

public sealed class BackupAutomaticoService(LojaService loja, ILogger<BackupAutomaticoService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var arquivo = loja.CriarBackupAutomaticoSeNecessario();
                if (!string.IsNullOrWhiteSpace(arquivo))
                {
                    logger.LogInformation("Backup automático criado em {Arquivo}", arquivo);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Não foi possível criar backup automático.");
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
