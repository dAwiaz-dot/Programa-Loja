namespace LojaSistema.Api.Responses;

public sealed record RelatorioResumoResponse(
    int ProdutosCadastrados,
    int ProdutosAtivos,
    int ProdutosComEstoqueBaixo,
    int VendasLoja,
    decimal FaturamentoLoja,
    int PedidosOnline,
    decimal FaturamentoOnline,
    decimal FaturamentoTotal,
    decimal DescontosLoja,
    decimal DescontosOnline,
    decimal DescontosTotal,
    decimal CustoEstoque,
    decimal CustoVendidoTotal,
    decimal LucroEstimado,
    decimal MargemLucroPercentual,
    decimal TicketMedio,
    string? ProdutoMaisVendido,
    int QuantidadeProdutoMaisVendido,
    IReadOnlyList<ResumoFormaPagamentoResponse> VendasPorPagamento,
    IReadOnlyList<ResumoProdutoVendidoResponse> ProdutosMaisVendidos,
    IReadOnlyList<ResumoVendasDiaResponse> VendasPorDia,
    IReadOnlyList<ResumoEntradaFornecedorResponse> EntradasPorFornecedor,
    IReadOnlyList<ResumoEstoqueBaixoResponse> ProdutosEstoqueBaixo);

public sealed record ResumoFormaPagamentoResponse(
    string FormaPagamento,
    int Quantidade,
    decimal Total);

public sealed record ResumoProdutoVendidoResponse(
    Guid ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal Total);

public sealed record ResumoVendasDiaResponse(
    string Data,
    int VendasPdv,
    int PedidosOnline,
    int Transacoes,
    decimal Faturamento,
    decimal Custo,
    decimal Lucro);

public sealed record ResumoEntradaFornecedorResponse(
    string FornecedorNome,
    int Entradas,
    int Unidades,
    decimal CustoTotal,
    string? UltimaEntrada);

public sealed record ResumoEstoqueBaixoResponse(
    Guid ProdutoId,
    string ProdutoNome,
    string Categoria,
    int QuantidadeEmEstoque);
