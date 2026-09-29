using System.Globalization;
using LojaSistema.Api.Models;
using LojaSistema.Api.Requests;
using LojaSistema.Api.Responses;
using Microsoft.Data.Sqlite;

namespace LojaSistema.Api.Services;

// Operação do dia a dia da loja física: caixa, fiado, vendedoras, cancelamento
// de venda e contagem de estoque. Fica separado do LojaService.cs só pra não
// inchar ainda mais o arquivo principal; é a mesma classe e usa o mesmo _sync.
public sealed partial class LojaService
{
    private const string TabelaMarcadoraAtualizacaoOperacao = "CaixaSessoes";
    private static readonly TimeZoneInfo FusoLoja = ObterFusoLoja();
    private static readonly FormaPagamento[] FormasRecebimentoFiado =
    [
        FormaPagamento.Dinheiro,
        FormaPagamento.Pix,
        FormaPagamento.CartaoDebito,
        FormaPagamento.CartaoCredito
    ];

    private readonly Dictionary<Guid, Vendedor> _vendedores = [];
    private readonly List<CaixaSessao> _caixas = [];
    private readonly List<RecebimentoFiado> _recebimentosFiado = [];

    // ---------- Banco ----------

    // Antes da primeira subida desta versão, guarda uma cópia íntegra do banco
    // (API de backup do SQLite, que respeita o WAL) em Data/backups.
    private void CriarBackupAntesDaAtualizacaoSeNecessario()
    {
        if (!File.Exists(_databasePath))
        {
            return;
        }

        using var origem = new SqliteConnection(_connectionString);
        origem.Open();
        using (var command = CreateCommand(origem, null, "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $Nome;"))
        {
            Add(command, "$Nome", TabelaMarcadoraAtualizacaoOperacao);
            if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
            {
                return;
            }
        }

        var backupDirectory = ObterDiretorioBackups();
        Directory.CreateDirectory(backupDirectory);
        var destinoPath = Path.Combine(backupDirectory, $"antes-atualizacao-caixa-fiado-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db");
        using var destino = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destinoPath, Pooling = false }.ToString());
        destino.Open();
        origem.BackupDatabase(destino);
    }

    private static void InicializarTabelasOperacao(SqliteConnection connection)
    {
        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS Vendedores (
                Id TEXT PRIMARY KEY,
                Nome TEXT NOT NULL,
                ComissaoPercentual TEXT NOT NULL,
                Ativo INTEGER NOT NULL,
                CriadoEm TEXT NOT NULL,
                AtualizadoEm TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS CaixaSessoes (
                Id TEXT PRIMARY KEY,
                AbertoEm TEXT NOT NULL,
                AbertoPor TEXT NOT NULL,
                ValorInicial TEXT NOT NULL,
                ObservacaoAbertura TEXT NULL,
                FechadoEm TEXT NULL,
                FechadoPor TEXT NULL,
                ValorContado TEXT NULL,
                ValorEsperado TEXT NULL,
                ObservacaoFechamento TEXT NULL,
                MovimentosJson TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS RecebimentosFiado (
                Id TEXT PRIMARY KEY,
                ClienteId TEXT NOT NULL,
                ClienteNome TEXT NOT NULL,
                Valor TEXT NOT NULL,
                FormaPagamento TEXT NOT NULL,
                Observacao TEXT NULL,
                Usuario TEXT NOT NULL,
                CriadoEm TEXT NOT NULL
            );
            """);
    }

    private void CarregarDadosOperacao(SqliteConnection connection)
    {
        using (var command = CreateCommand(connection, null, "SELECT * FROM Vendedores;"))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var vendedor = new Vendedor
                {
                    Id = ReadGuid(reader, "Id"),
                    Nome = ReadString(reader, "Nome"),
                    ComissaoPercentual = ReadDecimal(reader, "ComissaoPercentual"),
                    Ativo = ReadBool(reader, "Ativo"),
                    CriadoEm = ReadDateTime(reader, "CriadoEm"),
                    AtualizadoEm = ReadDateTime(reader, "AtualizadoEm")
                };
                _vendedores[vendedor.Id] = vendedor;
            }
        }

        using (var command = CreateCommand(connection, null, "SELECT * FROM CaixaSessoes;"))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                _caixas.Add(new CaixaSessao
                {
                    Id = ReadGuid(reader, "Id"),
                    AbertoEm = ReadDateTime(reader, "AbertoEm"),
                    AbertoPor = ReadString(reader, "AbertoPor"),
                    ValorInicial = ReadDecimal(reader, "ValorInicial"),
                    ObservacaoAbertura = ReadNullableString(reader, "ObservacaoAbertura"),
                    FechadoEm = ReadNullableDateTime(reader, "FechadoEm"),
                    FechadoPor = ReadNullableString(reader, "FechadoPor"),
                    ValorContado = ReadNullableDecimal(reader, "ValorContado"),
                    ValorEsperado = ReadNullableDecimal(reader, "ValorEsperado"),
                    ObservacaoFechamento = ReadNullableString(reader, "ObservacaoFechamento"),
                    Movimentos = DeserializeJson(ReadString(reader, "MovimentosJson"), new List<CaixaMovimento>())
                });
            }
        }

        using (var command = CreateCommand(connection, null, "SELECT * FROM RecebimentosFiado;"))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                _recebimentosFiado.Add(new RecebimentoFiado
                {
                    Id = ReadGuid(reader, "Id"),
                    ClienteId = ReadGuid(reader, "ClienteId"),
                    ClienteNome = ReadString(reader, "ClienteNome"),
                    Valor = ReadDecimal(reader, "Valor"),
                    FormaPagamento = ReadEnum(reader, "FormaPagamento", FormaPagamento.Dinheiro),
                    Observacao = ReadNullableString(reader, "Observacao"),
                    Usuario = ReadString(reader, "Usuario"),
                    CriadoEm = ReadDateTime(reader, "CriadoEm")
                });
            }
        }

        _caixas.Sort((a, b) => a.AbertoEm.CompareTo(b.AbertoEm));
        _recebimentosFiado.Sort((a, b) => a.CriadoEm.CompareTo(b.CriadoEm));
    }

    private void SalvarDadosOperacao(SqliteConnection connection, SqliteTransaction transaction)
    {
        ExecuteNonQuery(connection, transaction, "DELETE FROM Vendedores;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM CaixaSessoes;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM RecebimentosFiado;");

        foreach (var vendedor in _vendedores.Values)
        {
            using var command = CreateCommand(connection, transaction, """
                INSERT INTO Vendedores (Id, Nome, ComissaoPercentual, Ativo, CriadoEm, AtualizadoEm)
                VALUES ($Id, $Nome, $ComissaoPercentual, $Ativo, $CriadoEm, $AtualizadoEm);
                """);
            Add(command, "$Id", vendedor.Id);
            Add(command, "$Nome", vendedor.Nome);
            Add(command, "$ComissaoPercentual", vendedor.ComissaoPercentual);
            Add(command, "$Ativo", vendedor.Ativo);
            Add(command, "$CriadoEm", vendedor.CriadoEm);
            Add(command, "$AtualizadoEm", vendedor.AtualizadoEm);
            command.ExecuteNonQuery();
        }

        foreach (var caixa in _caixas)
        {
            using var command = CreateCommand(connection, transaction, """
                INSERT INTO CaixaSessoes (
                    Id, AbertoEm, AbertoPor, ValorInicial, ObservacaoAbertura, FechadoEm, FechadoPor,
                    ValorContado, ValorEsperado, ObservacaoFechamento, MovimentosJson)
                VALUES (
                    $Id, $AbertoEm, $AbertoPor, $ValorInicial, $ObservacaoAbertura, $FechadoEm, $FechadoPor,
                    $ValorContado, $ValorEsperado, $ObservacaoFechamento, $MovimentosJson);
                """);
            Add(command, "$Id", caixa.Id);
            Add(command, "$AbertoEm", caixa.AbertoEm);
            Add(command, "$AbertoPor", caixa.AbertoPor);
            Add(command, "$ValorInicial", caixa.ValorInicial);
            Add(command, "$ObservacaoAbertura", caixa.ObservacaoAbertura);
            Add(command, "$FechadoEm", caixa.FechadoEm);
            Add(command, "$FechadoPor", caixa.FechadoPor);
            Add(command, "$ValorContado", caixa.ValorContado);
            Add(command, "$ValorEsperado", caixa.ValorEsperado);
            Add(command, "$ObservacaoFechamento", caixa.ObservacaoFechamento);
            Add(command, "$MovimentosJson", SerializeJson(caixa.Movimentos));
            command.ExecuteNonQuery();
        }

        foreach (var recebimento in _recebimentosFiado)
        {
            using var command = CreateCommand(connection, transaction, """
                INSERT INTO RecebimentosFiado (Id, ClienteId, ClienteNome, Valor, FormaPagamento, Observacao, Usuario, CriadoEm)
                VALUES ($Id, $ClienteId, $ClienteNome, $Valor, $FormaPagamento, $Observacao, $Usuario, $CriadoEm);
                """);
            Add(command, "$Id", recebimento.Id);
            Add(command, "$ClienteId", recebimento.ClienteId);
            Add(command, "$ClienteNome", recebimento.ClienteNome);
            Add(command, "$Valor", recebimento.Valor);
            Add(command, "$FormaPagamento", recebimento.FormaPagamento);
            Add(command, "$Observacao", recebimento.Observacao);
            Add(command, "$Usuario", recebimento.Usuario);
            Add(command, "$CriadoEm", recebimento.CriadoEm);
            command.ExecuteNonQuery();
        }
    }

    // ---------- Datas ----------

    private static TimeZoneInfo ObterFusoLoja()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
        }
    }

    private static DateTime DataLocal(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.ToUniversalTime(), DateTimeKind.Utc), FusoLoja).Date;
    }

    private static DateTime HojeLocal()
    {
        return DataLocal(DateTime.UtcNow);
    }

    // Vencimento guardado ao meio-dia de Brasília (15h UTC) pra data não "pular"
    // um dia em nenhum fuso.
    private static DateTime? NormalizarDataVencimento(DateTime? data)
    {
        return data is null
            ? null
            : new DateTime(data.Value.Year, data.Value.Month, data.Value.Day, 15, 0, 0, DateTimeKind.Utc);
    }

    private static string? NormalizarDataNascimento(string? data)
    {
        if (string.IsNullOrWhiteSpace(data) ||
            !DateTime.TryParseExact(data.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var valor) ||
            valor.Year < 1900 ||
            valor > HojeLocal())
        {
            return null;
        }

        return valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    // ---------- Clientes ----------

    public Resultado<ClientePainelResponse> AtualizarClientePainel(Guid id, AtualizarClientePainelRequest request)
    {
        var nome = NormalizarTexto(request.Nome ?? "");
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Resultado<ClientePainelResponse>.Falha("Informe o nome do cliente.");
        }

        var emailInformado = NormalizarEmailOpcional(request.Email);
        if (!string.IsNullOrWhiteSpace(request.Email) && emailInformado is null)
        {
            return Resultado<ClientePainelResponse>.Falha("Informe um e-mail valido ou deixe em branco.");
        }

        var dataNascimento = NormalizarDataNascimento(request.DataNascimento);
        if (!string.IsNullOrWhiteSpace(request.DataNascimento) && dataNascimento is null)
        {
            return Resultado<ClientePainelResponse>.Falha("Data de nascimento invalida.");
        }

        lock (_sync)
        {
            if (!_clientes.TryGetValue(id, out var cliente))
            {
                return Resultado<ClientePainelResponse>.Falha("Cliente nao encontrado.");
            }

            if (emailInformado is not null &&
                _clientes.Values.Any(item => item.Id != id && string.Equals(item.Email, emailInformado, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<ClientePainelResponse>.Falha("Ja existe um cliente com esse e-mail.");
            }

            cliente.Nome = nome;
            cliente.Telefone = NormalizarTextoOpcional(request.Telefone);
            cliente.DataNascimento = dataNascimento;
            // E-mail em branco mantém o atual: pode ser o login da cliente na loja online.
            if (emailInformado is not null)
            {
                cliente.Email = emailInformado;
            }

            cliente.AtualizadoEm = DateTime.UtcNow;
            SalvarTudo();
            return Resultado<ClientePainelResponse>.Ok(ConstruirClientePainelResponse(cliente));
        }
    }

    // ---------- Vendedoras ----------

    public IReadOnlyList<Vendedor> ListarVendedores()
    {
        lock (_sync)
        {
            return _vendedores.Values
                .OrderByDescending(vendedor => vendedor.Ativo)
                .ThenBy(vendedor => vendedor.Nome)
                .ToList();
        }
    }

    public Resultado<Vendedor> SalvarVendedor(Guid? id, VendedorRequest request)
    {
        var nome = NormalizarTexto(request.Nome ?? "");
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Resultado<Vendedor>.Falha("Informe o nome da vendedora.");
        }

        if (request.ComissaoPercentual < 0 || request.ComissaoPercentual > 100)
        {
            return Resultado<Vendedor>.Falha("A comissao precisa ficar entre 0% e 100%.");
        }

        lock (_sync)
        {
            if (_vendedores.Values.Any(item => item.Id != id && string.Equals(item.Nome, nome, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<Vendedor>.Falha("Ja existe uma vendedora com esse nome.");
            }

            Vendedor vendedor;
            if (id is Guid vendedorId)
            {
                if (!_vendedores.TryGetValue(vendedorId, out vendedor!))
                {
                    return Resultado<Vendedor>.Falha("Vendedora nao encontrada.");
                }

                vendedor.Nome = nome;
                vendedor.ComissaoPercentual = request.ComissaoPercentual;
                vendedor.Ativo = request.Ativo;
                vendedor.AtualizadoEm = DateTime.UtcNow;
            }
            else
            {
                vendedor = new Vendedor
                {
                    Nome = nome,
                    ComissaoPercentual = request.ComissaoPercentual,
                    Ativo = request.Ativo
                };
                _vendedores[vendedor.Id] = vendedor;
            }

            SalvarTudo();
            return Resultado<Vendedor>.Ok(vendedor);
        }
    }

    // ---------- Cancelamento de venda ----------

    public Resultado<VendaLoja> CancelarVendaLoja(Guid id, string? motivo, string usuario, bool administrador)
    {
        var motivoNormalizado = NormalizarTextoOpcional(motivo);
        if (motivoNormalizado is null || motivoNormalizado.Length < 3)
        {
            return Resultado<VendaLoja>.Falha("Informe o motivo do cancelamento.");
        }

        lock (_sync)
        {
            var venda = _vendasLoja.FirstOrDefault(item => item.Id == id);
            if (venda is null)
            {
                return Resultado<VendaLoja>.Falha("Venda nao encontrada.");
            }

            if (venda.Cancelada)
            {
                return Resultado<VendaLoja>.Falha("Essa venda ja foi cancelada.");
            }

            if (venda.Devolvida || venda.Itens.Any(item => item.QuantidadeDevolvida > 0))
            {
                return Resultado<VendaLoja>.Falha("Essa venda ja teve devolucao ou troca. Use Devolver para o restante.");
            }

            if (!administrador && DataLocal(venda.CriadaEm) != HojeLocal())
            {
                return Resultado<VendaLoja>.Falha("Só o administrador pode cancelar vendas de dias anteriores.");
            }

            var resultado = AplicarDevolucaoVenda(venda, null, motivoNormalizado, "Cancelamento venda PDV");
            if (!resultado.Sucesso)
            {
                return resultado;
            }

            venda.Cancelada = true;
            venda.CanceladaEm = DateTime.UtcNow;
            venda.CanceladaPor = usuario;
            venda.MotivoCancelamento = motivoNormalizado;
            SalvarTudo();
            return Resultado<VendaLoja>.Ok(venda);
        }
    }

    // ---------- Fiado ----------

    private Dictionary<Guid, decimal> CalcularSaldosFiado()
    {
        var saldos = new Dictionary<Guid, decimal>();
        foreach (var venda in _vendasLoja.Where(venda => venda.FormaPagamento == FormaPagamento.Fiado && venda.ClienteId is not null))
        {
            saldos[venda.ClienteId!.Value] = saldos.GetValueOrDefault(venda.ClienteId!.Value) + venda.Total;
        }

        foreach (var recebimento in _recebimentosFiado)
        {
            saldos[recebimento.ClienteId] = saldos.GetValueOrDefault(recebimento.ClienteId) - recebimento.Valor;
        }

        return saldos;
    }

    private FiadoClienteResponse ConstruirResumoFiado(Guid clienteId)
    {
        _clientes.TryGetValue(clienteId, out var cliente);
        var compras = _vendasLoja
            .Where(venda => venda.FormaPagamento == FormaPagamento.Fiado && venda.ClienteId == clienteId)
            .OrderBy(venda => venda.CriadaEm)
            .ToList();
        var pagamentos = _recebimentosFiado.Where(item => item.ClienteId == clienteId).ToList();
        var totalComprado = compras.Sum(venda => venda.Total);
        var totalPago = pagamentos.Sum(item => item.Valor);

        // O vencimento que importa é o da parcela mais antiga ainda em aberto.
        var proximaParcela = CalcularParcelasFiado(clienteId).FirstOrDefault(parcela => parcela.Restante > 0);
        DateTime? vencimento = proximaParcela?.Vencimento;
        var diasAtraso = proximaParcela is null ? 0 : Math.Max(0, -proximaParcela.DiasParaVencer);
        return new FiadoClienteResponse(
            clienteId,
            cliente?.Nome ?? compras.LastOrDefault()?.ClienteNome ?? pagamentos.LastOrDefault()?.ClienteNome ?? "Cliente removido",
            cliente?.Telefone,
            totalComprado,
            totalPago,
            totalComprado - totalPago,
            compras.Count > 0 ? compras.Max(venda => venda.CriadaEm) : null,
            pagamentos.Count > 0 ? pagamentos.Max(item => item.CriadoEm) : null,
            vencimento,
            diasAtraso);
    }

    public IReadOnlyList<FiadoClienteResponse> ListarFiado()
    {
        lock (_sync)
        {
            return _vendasLoja
                .Where(venda => venda.FormaPagamento == FormaPagamento.Fiado && venda.ClienteId is not null)
                .Select(venda => venda.ClienteId!.Value)
                .Concat(_recebimentosFiado.Select(item => item.ClienteId))
                .Distinct()
                .Select(ConstruirResumoFiado)
                .OrderByDescending(item => item.DiasAtraso)
                .ThenByDescending(item => item.Saldo)
                .ThenBy(item => item.Nome)
                .ToList();
        }
    }

    public Resultado<FiadoExtratoResponse> ObterExtratoFiado(Guid clienteId)
    {
        lock (_sync)
        {
            if (!_clientes.ContainsKey(clienteId) &&
                !_vendasLoja.Any(venda => venda.ClienteId == clienteId) &&
                !_recebimentosFiado.Any(item => item.ClienteId == clienteId))
            {
                return Resultado<FiadoExtratoResponse>.Falha("Cliente nao encontrado.");
            }

            var lancamentos = _vendasLoja
                .Where(venda => venda.FormaPagamento == FormaPagamento.Fiado && venda.ClienteId == clienteId)
                .Select(venda => (
                    Data: venda.CriadaEm,
                    Id: venda.Id,
                    Tipo: "Compra",
                    Descricao: DescreverCompraFiado(venda),
                    Valor: venda.Total,
                    VendaId: (Guid?)venda.Id,
                    Vencimento: venda.VencimentoEm,
                    Forma: (string?)null,
                    Usuario: venda.RegistradaPor))
                .Concat(_recebimentosFiado
                    .Where(item => item.ClienteId == clienteId)
                    .Select(item => (
                        Data: item.CriadoEm,
                        Id: item.Id,
                        Tipo: "Pagamento",
                        Descricao: item.Observacao ?? "Pagamento recebido",
                        Valor: -item.Valor,
                        VendaId: (Guid?)null,
                        Vencimento: (DateTime?)null,
                        Forma: (string?)item.FormaPagamento.ToString(),
                        Usuario: (string?)item.Usuario)))
                .OrderBy(item => item.Data)
                .ToList();

            var saldo = 0m;
            var extrato = new List<FiadoLancamentoResponse>();
            foreach (var item in lancamentos)
            {
                saldo += item.Valor;
                extrato.Add(new FiadoLancamentoResponse(
                    item.Id,
                    item.Tipo,
                    item.Data,
                    item.Descricao,
                    item.Valor,
                    saldo,
                    item.VendaId,
                    item.Vencimento,
                    item.Forma,
                    item.Usuario));
            }

            extrato.Reverse();
            return Resultado<FiadoExtratoResponse>.Ok(new FiadoExtratoResponse(
                ConstruirResumoFiado(clienteId),
                extrato,
                CalcularParcelasFiado(clienteId)));
        }
    }

    // Cada compra no fiado vira N parcelas de 30 em 30 dias a partir do
    // primeiro vencimento. Os pagamentos da cliente quitam as parcelas na
    // ordem de vencimento (a mais antiga primeiro). Tudo é recalculado na hora,
    // então devolução/cancelamento ajustam as parcelas sozinhos.
    private List<ParcelaFiadoResponse> CalcularParcelasFiado(Guid clienteId)
    {
        var hoje = HojeLocal();
        var parcelas = new List<(VendaLoja Venda, int Numero, int Total, decimal Valor, DateTime Vencimento)>();
        foreach (var venda in _vendasLoja.Where(venda =>
            venda.FormaPagamento == FormaPagamento.Fiado &&
            venda.ClienteId == clienteId &&
            venda.Total > 0))
        {
            var quantidade = Math.Max(1, venda.Parcelas);
            var primeiroVencimento = venda.VencimentoEm ?? NormalizarDataVencimento(DataLocal(venda.CriadaEm).AddDays(30))!.Value;
            var valorBase = Math.Floor(venda.Total / quantidade * 100) / 100;
            for (var numero = 1; numero <= quantidade; numero++)
            {
                var valor = numero == quantidade ? venda.Total - valorBase * (quantidade - 1) : valorBase;
                parcelas.Add((venda, numero, quantidade, valor, primeiroVencimento.AddDays(30 * (numero - 1))));
            }
        }

        var credito = _recebimentosFiado.Where(item => item.ClienteId == clienteId).Sum(item => item.Valor);
        var resultado = new List<ParcelaFiadoResponse>();
        foreach (var parcela in parcelas
            .OrderBy(item => item.Vencimento)
            .ThenBy(item => item.Venda.CriadaEm)
            .ThenBy(item => item.Numero))
        {
            var pago = Math.Min(credito, parcela.Valor);
            credito -= pago;
            var restante = parcela.Valor - pago;
            var diasParaVencer = (parcela.Vencimento.Date - hoje).Days;
            var status = restante <= 0 ? "Paga" : diasParaVencer < 0 ? "Atrasada" : diasParaVencer == 0 ? "VenceHoje" : "AVencer";
            resultado.Add(new ParcelaFiadoResponse(
                parcela.Venda.Id,
                parcela.Venda.Id.ToString()[..8].ToUpperInvariant(),
                parcela.Venda.CriadaEm,
                parcela.Numero,
                parcela.Total,
                parcela.Valor,
                pago,
                restante,
                parcela.Vencimento,
                status,
                diasParaVencer));
        }

        return resultado;
    }

    public IReadOnlyList<LembreteFiadoResponse> ListarLembretesFiado(int diasAFrente)
    {
        var limite = Math.Clamp(diasAFrente, 0, 60);
        lock (_sync)
        {
            var clientes = _vendasLoja
                .Where(venda => venda.FormaPagamento == FormaPagamento.Fiado && venda.ClienteId is not null)
                .Select(venda => venda.ClienteId!.Value)
                .Distinct()
                .ToList();
            var saldos = CalcularSaldosFiado();

            return clientes
                .SelectMany(clienteId =>
                {
                    _clientes.TryGetValue(clienteId, out var cliente);
                    var nome = cliente?.Nome ?? _vendasLoja.Last(venda => venda.ClienteId == clienteId).ClienteNome ?? "Cliente";
                    return CalcularParcelasFiado(clienteId)
                        .Where(parcela => parcela.Restante > 0 && parcela.DiasParaVencer <= limite)
                        .Select(parcela => new LembreteFiadoResponse(
                            clienteId,
                            nome,
                            cliente?.Telefone,
                            saldos.GetValueOrDefault(clienteId),
                            parcela));
                })
                .OrderBy(item => item.Parcela.DiasParaVencer)
                .ThenBy(item => item.ClienteNome)
                .ToList();
        }
    }

    public Resultado<ClienteSimplesResponse> AtualizarTelefoneCliente(Guid clienteId, string? telefone)
    {
        var telefoneNormalizado = NormalizarTextoOpcional(telefone);
        if (telefoneNormalizado is null || telefoneNormalizado.Count(char.IsDigit) < 10)
        {
            return Resultado<ClienteSimplesResponse>.Falha("Informe o WhatsApp com DDD, ex: (35) 99999-9999.");
        }

        lock (_sync)
        {
            if (!_clientes.TryGetValue(clienteId, out var cliente))
            {
                return Resultado<ClienteSimplesResponse>.Falha("Cliente nao encontrado.");
            }

            cliente.Telefone = telefoneNormalizado;
            cliente.AtualizadoEm = DateTime.UtcNow;
            SalvarTudo();
        }

        var atualizado = ListarClientesSimples().First(item => item.Id == clienteId);
        return Resultado<ClienteSimplesResponse>.Ok(atualizado);
    }

    private static string DescreverCompraFiado(VendaLoja venda)
    {
        var codigo = venda.Id.ToString()[..8].ToUpperInvariant();
        var pecas = venda.Itens.Sum(item => item.Quantidade);
        var descricao = $"Compra #{codigo} · {pecas} peça{(pecas == 1 ? "" : "s")}{(venda.Parcelas > 1 ? $" · {venda.Parcelas}x" : "")}";
        if (venda.Cancelada)
        {
            return $"{descricao} (cancelada)";
        }

        if (venda.Devolvida)
        {
            return $"{descricao} (devolvida)";
        }

        return venda.DevolucaoParcial ? $"{descricao} (devolução parcial)" : descricao;
    }

    public Resultado<FiadoExtratoResponse> RegistrarRecebimentoFiado(Guid clienteId, RecebimentoFiadoRequest request, string usuario)
    {
        if (request.Valor <= 0)
        {
            return Resultado<FiadoExtratoResponse>.Falha("Informe um valor maior que zero.");
        }

        if (!FormasRecebimentoFiado.Contains(request.FormaPagamento))
        {
            return Resultado<FiadoExtratoResponse>.Falha("Escolha dinheiro, Pix ou cartão para receber o fiado.");
        }

        lock (_sync)
        {
            if (!_clientes.TryGetValue(clienteId, out var cliente))
            {
                return Resultado<FiadoExtratoResponse>.Falha("Cliente nao encontrado.");
            }

            var saldo = CalcularSaldosFiado().GetValueOrDefault(clienteId);
            if (saldo <= 0)
            {
                return Resultado<FiadoExtratoResponse>.Falha("Essa cliente nao tem saldo em aberto no fiado.");
            }

            var valor = Math.Round(request.Valor, 2, MidpointRounding.AwayFromZero);
            if (valor > saldo)
            {
                return Resultado<FiadoExtratoResponse>.Falha($"O valor passa do saldo em aberto ({saldo.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}).");
            }

            _recebimentosFiado.Add(new RecebimentoFiado
            {
                ClienteId = cliente.Id,
                ClienteNome = cliente.Nome,
                Valor = valor,
                FormaPagamento = request.FormaPagamento,
                Observacao = NormalizarTextoOpcional(request.Observacao),
                Usuario = usuario
            });
            SalvarTudo();
        }

        return ObterExtratoFiado(clienteId);
    }

    public Resultado<FiadoExtratoResponse> ExcluirRecebimentoFiado(Guid recebimentoId)
    {
        Guid clienteId;
        lock (_sync)
        {
            var recebimento = _recebimentosFiado.FirstOrDefault(item => item.Id == recebimentoId);
            if (recebimento is null)
            {
                return Resultado<FiadoExtratoResponse>.Falha("Pagamento nao encontrado.");
            }

            clienteId = recebimento.ClienteId;
            _recebimentosFiado.Remove(recebimento);
            SalvarTudo();
        }

        return ObterExtratoFiado(clienteId);
    }

    // ---------- Caixa ----------

    private CaixaResumoResponse ConstruirResumoCaixa(CaixaSessao caixa)
    {
        var fim = caixa.FechadoEm ?? DateTime.MaxValue;
        var vendas = _vendasLoja
            .Where(venda => venda.CriadaEm >= caixa.AbertoEm && venda.CriadaEm < fim)
            .ToList();
        var vendasValidas = vendas.Where(venda => !venda.Devolvida).ToList();
        var recebimentos = _recebimentosFiado
            .Where(item => item.CriadoEm >= caixa.AbertoEm && item.CriadoEm < fim)
            .ToList();

        var porPagamento = vendasValidas
            .GroupBy(venda => venda.FormaPagamento.ToString())
            .Select(grupo => new CaixaPagamentoResumoResponse(grupo.Key, grupo.Count(), grupo.Sum(venda => venda.Total)))
            .OrderByDescending(item => item.Total)
            .ToList();
        var vendasDinheiro = vendasValidas.Where(venda => venda.FormaPagamento == FormaPagamento.Dinheiro).Sum(venda => venda.Total);
        var vendidoFiado = vendasValidas.Where(venda => venda.FormaPagamento == FormaPagamento.Fiado).Sum(venda => venda.Total);
        var fiadoDinheiro = recebimentos.Where(item => item.FormaPagamento == FormaPagamento.Dinheiro).Sum(item => item.Valor);
        var fiadoOutros = recebimentos.Where(item => item.FormaPagamento != FormaPagamento.Dinheiro).Sum(item => item.Valor);
        var reforcos = caixa.Movimentos.Where(item => item.Tipo == "Reforco").Sum(item => item.Valor);
        var sangrias = caixa.Movimentos.Where(item => item.Tipo == "Sangria").Sum(item => item.Valor);
        var esperadoCalculado = caixa.ValorInicial + vendasDinheiro + fiadoDinheiro + reforcos - sangrias;
        var esperado = caixa.ValorEsperado ?? esperadoCalculado;

        return new CaixaResumoResponse(
            caixa,
            vendasValidas.Count,
            vendasValidas.Sum(venda => venda.Total),
            porPagamento,
            vendasDinheiro,
            vendidoFiado,
            fiadoDinheiro,
            fiadoOutros,
            reforcos,
            sangrias,
            esperado,
            caixa.ValorContado is decimal contado ? contado - esperado : null,
            vendas.Count(venda => venda.Cancelada));
    }

    public CaixaResumoResponse? ObterCaixaAtual()
    {
        lock (_sync)
        {
            var caixa = _caixas.LastOrDefault(item => item.Aberto);
            return caixa is null ? null : ConstruirResumoCaixa(caixa);
        }
    }

    public IReadOnlyList<CaixaResumoResponse> ListarCaixas()
    {
        lock (_sync)
        {
            return _caixas
                .Where(item => !item.Aberto)
                .OrderByDescending(item => item.AbertoEm)
                .Take(60)
                .Select(ConstruirResumoCaixa)
                .ToList();
        }
    }

    public Resultado<CaixaResumoResponse> AbrirCaixa(AbrirCaixaRequest request, string usuario)
    {
        if (request.ValorInicial < 0)
        {
            return Resultado<CaixaResumoResponse>.Falha("O troco inicial nao pode ser negativo.");
        }

        lock (_sync)
        {
            if (_caixas.Any(item => item.Aberto))
            {
                return Resultado<CaixaResumoResponse>.Falha("Ja existe um caixa aberto. Feche antes de abrir outro.");
            }

            var caixa = new CaixaSessao
            {
                AbertoPor = usuario,
                ValorInicial = request.ValorInicial,
                ObservacaoAbertura = NormalizarTextoOpcional(request.Observacao)
            };
            _caixas.Add(caixa);
            SalvarTudo();
            return Resultado<CaixaResumoResponse>.Ok(ConstruirResumoCaixa(caixa));
        }
    }

    public Resultado<CaixaResumoResponse> RegistrarMovimentoCaixa(MovimentoCaixaRequest request, string usuario)
    {
        var tipo = request.Tipo?.Trim() switch
        {
            "Sangria" or "sangria" => "Sangria",
            "Reforco" or "reforco" or "Reforço" or "reforço" => "Reforco",
            _ => null
        };
        if (tipo is null)
        {
            return Resultado<CaixaResumoResponse>.Falha("Escolha sangria (retirada) ou reforço (entrada de troco).");
        }

        if (request.Valor <= 0)
        {
            return Resultado<CaixaResumoResponse>.Falha("Informe um valor maior que zero.");
        }

        lock (_sync)
        {
            var caixa = _caixas.LastOrDefault(item => item.Aberto);
            if (caixa is null)
            {
                return Resultado<CaixaResumoResponse>.Falha("Abra o caixa primeiro.");
            }

            if (tipo == "Sangria" && request.Valor > ConstruirResumoCaixa(caixa).DinheiroEsperado)
            {
                return Resultado<CaixaResumoResponse>.Falha("A retirada passa do dinheiro que deveria estar na gaveta.");
            }

            caixa.Movimentos.Add(new CaixaMovimento
            {
                Tipo = tipo,
                Valor = request.Valor,
                Motivo = NormalizarTextoOpcional(request.Motivo),
                Usuario = usuario
            });
            SalvarTudo();
            return Resultado<CaixaResumoResponse>.Ok(ConstruirResumoCaixa(caixa));
        }
    }

    public Resultado<CaixaResumoResponse> FecharCaixa(FecharCaixaRequest request, string usuario)
    {
        if (request.ValorContado < 0)
        {
            return Resultado<CaixaResumoResponse>.Falha("O valor contado nao pode ser negativo.");
        }

        lock (_sync)
        {
            var caixa = _caixas.LastOrDefault(item => item.Aberto);
            if (caixa is null)
            {
                return Resultado<CaixaResumoResponse>.Falha("Nao ha caixa aberto.");
            }

            var esperado = ConstruirResumoCaixa(caixa).DinheiroEsperado;
            caixa.FechadoEm = DateTime.UtcNow;
            caixa.FechadoPor = usuario;
            caixa.ValorContado = request.ValorContado;
            caixa.ValorEsperado = esperado;
            caixa.ObservacaoFechamento = NormalizarTextoOpcional(request.Observacao);
            SalvarTudo();
            return Resultado<CaixaResumoResponse>.Ok(ConstruirResumoCaixa(caixa));
        }
    }

    // ---------- Contagem de estoque ----------

    public Resultado<ContagemEstoqueResponse> AplicarContagemEstoque(ContagemEstoqueRequest request)
    {
        if (request.Itens is null || request.Itens.Count == 0)
        {
            return Resultado<ContagemEstoqueResponse>.Falha("Nenhuma peça contada.");
        }

        if (request.Itens.Any(item => item.QuantidadeContada < 0))
        {
            return Resultado<ContagemEstoqueResponse>.Falha("Quantidade contada nao pode ser negativa.");
        }

        var observacao = NormalizarTextoOpcional(request.Observacao);
        var itens = request.Itens
            .Select(item => item with
            {
                Tamanho = NormalizarTextoOpcional(item.Tamanho),
                Cor = NormalizarTextoOpcional(item.Cor),
                Modelo = NormalizarTextoOpcional(item.Modelo)
            })
            .GroupBy(item => CriarChaveItemVenda(item.ProdutoId, item.Tamanho, item.Cor, item.Modelo))
            .Select(grupo => grupo.Last())
            .ToList();

        lock (_sync)
        {
            // Valida tudo antes de mexer em qualquer estoque.
            foreach (var item in itens)
            {
                if (!_produtos.TryGetValue(item.ProdutoId, out var produto))
                {
                    return Resultado<ContagemEstoqueResponse>.Falha("Um dos produtos contados nao existe mais. Atualize a tela.");
                }

                if (produto.VariacoesEstoque.Count > 0 && EncontrarVariacaoEstoque(produto, item.Tamanho, item.Cor, item.Modelo) is null)
                {
                    return Resultado<ContagemEstoqueResponse>.Falha($"Variação nao encontrada em {produto.Nome}. Atualize a tela.");
                }
            }

            var ajustados = 0;
            var sobrando = 0;
            var faltando = 0;
            foreach (var item in itens)
            {
                var produto = _produtos[item.ProdutoId];
                var variacao = produto.VariacoesEstoque.Count > 0
                    ? EncontrarVariacaoEstoque(produto, item.Tamanho, item.Cor, item.Modelo)
                    : null;
                var atual = variacao?.Quantidade ?? produto.QuantidadeEmEstoque;
                var diferenca = item.QuantidadeContada - atual;
                if (diferenca == 0)
                {
                    continue;
                }

                if (variacao is not null)
                {
                    variacao.Quantidade = item.QuantidadeContada;
                    produto.QuantidadeEmEstoque = Math.Max(0, produto.QuantidadeEmEstoque + diferenca);
                }
                else
                {
                    produto.QuantidadeEmEstoque = item.QuantidadeContada;
                }

                produto.AtualizadoEm = DateTime.UtcNow;
                var textoVariacao = variacao is null ? null : CriarTextoVariacao(variacao.Tamanho, variacao.Cor, variacao.Modelo);
                RegistrarMovimentacao(
                    produto,
                    diferenca > 0 ? TipoMovimentacaoEstoque.Entrada : TipoMovimentacaoEstoque.Ajuste,
                    Math.Abs(diferenca),
                    $"Contagem de estoque - {(diferenca > 0 ? "sobra" : "falta")}{(textoVariacao is null ? "" : $" ({textoVariacao})")}",
                    null,
                    documento: observacao);

                ajustados += 1;
                if (diferenca > 0)
                {
                    sobrando += diferenca;
                }
                else
                {
                    faltando += -diferenca;
                }
            }

            if (ajustados > 0)
            {
                SalvarTudo();
            }

            return Resultado<ContagemEstoqueResponse>.Ok(new ContagemEstoqueResponse(itens.Count, ajustados, sobrando, faltando));
        }
    }
}
