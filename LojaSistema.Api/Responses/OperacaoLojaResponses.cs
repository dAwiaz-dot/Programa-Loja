using LojaSistema.Api.Models;

namespace LojaSistema.Api.Responses;

public sealed record CaixaPagamentoResumoResponse(string FormaPagamento, int Quantidade, decimal Total);

public sealed record CaixaResumoResponse(
    CaixaSessao Sessao,
    int QuantidadeVendas,
    decimal TotalVendas,
    IReadOnlyList<CaixaPagamentoResumoResponse> PorPagamento,
    decimal VendasDinheiro,
    decimal VendidoFiado,
    decimal RecebimentosFiadoDinheiro,
    decimal RecebimentosFiadoOutros,
    decimal Reforcos,
    decimal Sangrias,
    decimal DinheiroEsperado,
    decimal? Diferenca,
    int Cancelamentos);

public sealed record FiadoClienteResponse(
    Guid ClienteId,
    string Nome,
    string? Telefone,
    decimal TotalComprado,
    decimal TotalPago,
    decimal Saldo,
    DateTime? UltimaCompraEm,
    DateTime? UltimoPagamentoEm,
    DateTime? VencimentoEm,
    int DiasAtraso);

public sealed record FiadoLancamentoResponse(
    Guid Id,
    string Tipo,
    DateTime Data,
    string Descricao,
    decimal Valor,
    decimal SaldoApos,
    Guid? VendaId,
    DateTime? VencimentoEm,
    string? FormaPagamento,
    string? Usuario);

public sealed record FiadoExtratoResponse(
    FiadoClienteResponse Resumo,
    IReadOnlyList<FiadoLancamentoResponse> Lancamentos,
    IReadOnlyList<ParcelaFiadoResponse> Parcelas);

// Status: "Paga", "Atrasada", "VenceHoje" ou "AVencer".
public sealed record ParcelaFiadoResponse(
    Guid VendaId,
    string VendaCodigo,
    DateTime CompraEm,
    int Numero,
    int TotalParcelas,
    decimal Valor,
    decimal Pago,
    decimal Restante,
    DateTime Vencimento,
    string Status,
    int DiasParaVencer);

public sealed record LembreteFiadoResponse(
    Guid ClienteId,
    string ClienteNome,
    string? Telefone,
    decimal SaldoCliente,
    ParcelaFiadoResponse Parcela);

public sealed record ContagemEstoqueResponse(
    int ItensConferidos,
    int ItensAjustados,
    int PecasSobrando,
    int PecasFaltando);
