using LojaSistema.Api.Models;

namespace LojaSistema.Api.Responses;

public sealed record ClientePainelResponse(
    Guid Id,
    string Nome,
    string Email,
    string? Telefone,
    int Pedidos,
    int PedidosValidos,
    decimal TotalCompras,
    DateTime? UltimoPedidoEm,
    StatusPedidoOnline? UltimoStatus,
    DateTime CriadoEm,
    DateTime AtualizadoEm);
