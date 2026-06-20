using LojaSistema.Api.Models;

namespace LojaSistema.Api.Responses;

public sealed record TrocaVendaLojaResponse(
    VendaLoja VendaOriginal,
    VendaLoja VendaTroca);
