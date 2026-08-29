using LojaSistema.Api.Models;

namespace LojaSistema.Api.Requests;

public sealed record RegistrarVendaLojaRequest(
    FormaPagamento FormaPagamento,
    IReadOnlyList<ItemVendaLojaRequest> Itens,
    decimal Desconto,
    decimal ValorRecebido,
    string? Observacao,
    Guid? ClienteId);

public sealed record ItemVendaLojaRequest(
    Guid ProdutoId,
    int Quantidade,
    string? Tamanho,
    string? Cor,
    string? Modelo);

public sealed record DevolverVendaLojaRequest(
    string? Motivo,
    IReadOnlyList<ItemDevolucaoVendaLojaRequest>? Itens);

public sealed record ItemDevolucaoVendaLojaRequest(
    Guid ProdutoId,
    int Quantidade,
    string? Tamanho,
    string? Cor,
    string? Modelo);

public sealed record TrocarVendaLojaRequest(
    string? Motivo,
    IReadOnlyList<ItemDevolucaoVendaLojaRequest> ItensDevolvidos,
    IReadOnlyList<ItemVendaLojaRequest> ItensNovos,
    FormaPagamento FormaPagamento,
    decimal ValorRecebido,
    string? Observacao);
