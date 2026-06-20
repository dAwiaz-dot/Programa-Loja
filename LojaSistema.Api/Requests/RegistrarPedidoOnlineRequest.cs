using LojaSistema.Api.Models;

namespace LojaSistema.Api.Requests;

public sealed record RegistrarPedidoOnlineRequest(
    string NomeCliente,
    string EmailCliente,
    string? TelefoneCliente,
    string? DocumentoCliente,
    string? EnderecoEntrega,
    string? CepEntrega,
    string? RuaEntrega,
    string? NumeroEntrega,
    string? ComplementoEntrega,
    string? BairroEntrega,
    string? CidadeEntrega,
    string? EstadoEntrega,
    string? Observacao,
    string? CupomCodigo,
    Guid? OpcaoEntregaId,
    FormaPagamento FormaPagamento,
    IReadOnlyList<ItemPedidoOnlineRequest> Itens);

public sealed record ItemPedidoOnlineRequest(
    Guid ProdutoId,
    int Quantidade,
    string? Tamanho,
    string? Cor,
    string? Modelo);
