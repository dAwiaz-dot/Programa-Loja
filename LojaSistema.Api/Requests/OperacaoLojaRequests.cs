using LojaSistema.Api.Models;

namespace LojaSistema.Api.Requests;

public sealed record AbrirCaixaRequest(decimal ValorInicial, string? Observacao);

public sealed record MovimentoCaixaRequest(string Tipo, decimal Valor, string? Motivo);

public sealed record FecharCaixaRequest(decimal ValorContado, string? Observacao);

public sealed record RecebimentoFiadoRequest(decimal Valor, FormaPagamento FormaPagamento, string? Observacao);

public sealed record AtualizarTelefoneClienteRequest(string? Telefone);

public sealed record VendedorRequest(string Nome, decimal ComissaoPercentual, bool Ativo);

public sealed record ContagemEstoqueRequest(string? Observacao, IReadOnlyList<ItemContagemEstoqueRequest> Itens);

public sealed record ItemContagemEstoqueRequest(
    Guid ProdutoId,
    string? Tamanho,
    string? Cor,
    string? Modelo,
    int QuantidadeContada);
