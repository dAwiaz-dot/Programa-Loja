namespace LojaSistema.Api.Requests;

public sealed record CriarCategoriaRequest(string Nome, string? CategoriaPaiId = null);
