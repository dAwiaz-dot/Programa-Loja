using LojaSistema.Api.Models;

namespace LojaSistema.Api.Responses;

public sealed record ClientePainelResponse(
    Guid Id,
    string Nome,
    string? Email,
    string? Telefone,
    int PedidosOnline,
    int PedidosOnlineValidos,
    decimal TotalOnline,
    int ComprasLoja,
    decimal TotalLoja,
    decimal TotalGasto,
    DateTime? UltimaCompraEm,
    string? OrigemUltimaCompra,
    StatusPedidoOnline? UltimoStatusOnline,
    DateTime CriadoEm,
    DateTime AtualizadoEm);
