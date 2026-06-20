namespace LojaSistema.Api.Services;

public sealed record Resultado<T>(bool Sucesso, T? Valor, string? Erro)
{
    public static Resultado<T> Ok(T valor) => new(true, valor, null);
    public static Resultado<T> Falha(string erro) => new(false, default, erro);
}
