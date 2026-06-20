using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LojaSistema.Api.Models;

namespace LojaSistema.Api.Services;

public sealed class AsaasPagamentoService(HttpClient httpClient)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Resultado<PagamentoPixGateway>> CriarPixAsync(
        PedidoOnline pedido,
        GatewayPagamentoConfiguracao configuracao,
        CancellationToken cancellationToken)
    {
        if (!configuracao.Ativo || !ProvedorAsaas(configuracao.Provedor))
        {
            return Resultado<PagamentoPixGateway>.Falha("Gateway Asaas nao esta ativo.");
        }

        if (string.IsNullOrWhiteSpace(configuracao.AccessToken))
        {
            return Resultado<PagamentoPixGateway>.Falha("Token do Asaas nao configurado.");
        }

        var documento = SomenteDigitos(pedido.DocumentoCliente);
        if (documento.Length is not 11 and not 14)
        {
            return Resultado<PagamentoPixGateway>.Falha("Informe CPF ou CNPJ valido para gerar o Pix automatico.");
        }

        try
        {
            var customerId = await CriarClienteAsync(pedido, documento, configuracao, cancellationToken);
            if (!customerId.Sucesso)
            {
                return Resultado<PagamentoPixGateway>.Falha(customerId.Erro ?? "Nao foi possivel criar o cliente no Asaas.");
            }

            var pagamento = await CriarCobrancaPixAsync(pedido, customerId.Valor!, configuracao, cancellationToken);
            if (!pagamento.Sucesso)
            {
                return Resultado<PagamentoPixGateway>.Falha(pagamento.Erro ?? "Nao foi possivel criar a cobranca Pix no Asaas.");
            }

            var qrCode = await ObterQrCodePixAsync(pagamento.Valor!.PagamentoId, configuracao, cancellationToken);
            if (!qrCode.Sucesso)
            {
                return Resultado<PagamentoPixGateway>.Falha(qrCode.Erro ?? "Nao foi possivel obter o QR Code Pix no Asaas.");
            }

            var dadosQr = qrCode.Valor!;
            return Resultado<PagamentoPixGateway>.Ok(pagamento.Valor with
            {
                PixCopiaECola = dadosQr.PixCopiaECola,
                PixQrCodeBase64 = dadosQr.PixQrCodeBase64,
                PixExpiraEm = dadosQr.PixExpiraEm
            });
        }
        catch (HttpRequestException)
        {
            return Resultado<PagamentoPixGateway>.Falha("Nao foi possivel conectar ao Asaas agora.");
        }
        catch (TaskCanceledException)
        {
            return Resultado<PagamentoPixGateway>.Falha("Tempo esgotado ao conectar no Asaas.");
        }
        catch (JsonException)
        {
            return Resultado<PagamentoPixGateway>.Falha("O Asaas retornou uma resposta inesperada.");
        }
    }

    private async Task<Resultado<string>> CriarClienteAsync(
        PedidoOnline pedido,
        string documento,
        GatewayPagamentoConfiguracao configuracao,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            name = pedido.NomeCliente,
            cpfCnpj = documento,
            email = pedido.EmailCliente,
            mobilePhone = SomenteDigitosOuNull(pedido.TelefoneCliente),
            postalCode = SomenteDigitosOuNull(pedido.CepEntrega),
            address = pedido.RuaEntrega,
            addressNumber = pedido.NumeroEntrega,
            complement = pedido.ComplementoEntrega,
            province = pedido.BairroEntrega,
            externalReference = pedido.ClienteId?.ToString() ?? pedido.EmailCliente,
            notificationDisabled = true
        };

        using var response = await EnviarAsync(HttpMethod.Post, "/customers", configuracao, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Resultado<string>.Falha(await LerErroAsaasAsync(response, cancellationToken));
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var id = ObterString(document.RootElement, "id");
        return string.IsNullOrWhiteSpace(id)
            ? Resultado<string>.Falha("O Asaas nao retornou o ID do cliente.")
            : Resultado<string>.Ok(id);
    }

    private async Task<Resultado<PagamentoPixGateway>> CriarCobrancaPixAsync(
        PedidoOnline pedido,
        string customerId,
        GatewayPagamentoConfiguracao configuracao,
        CancellationToken cancellationToken)
    {
        var shortId = pedido.Id.ToString()[..8].ToUpperInvariant();
        var body = new
        {
            customer = customerId,
            billingType = "PIX",
            value = decimal.Round(pedido.Total, 2),
            dueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            description = $"Pedido Nana Modas {shortId}",
            externalReference = pedido.Id.ToString()
        };

        using var response = await EnviarAsync(HttpMethod.Post, "/payments", configuracao, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Resultado<PagamentoPixGateway>.Falha(await LerErroAsaasAsync(response, cancellationToken));
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        var paymentId = ObterString(root, "id");
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return Resultado<PagamentoPixGateway>.Falha("O Asaas nao retornou o ID da cobranca.");
        }

        return Resultado<PagamentoPixGateway>.Ok(new PagamentoPixGateway(
            "Asaas",
            paymentId,
            ObterString(root, "status") ?? "PENDING",
            "",
            null,
            null,
            ObterString(root, "invoiceUrl")));
    }

    private async Task<Resultado<PagamentoPixGateway>> ObterQrCodePixAsync(
        string paymentId,
        GatewayPagamentoConfiguracao configuracao,
        CancellationToken cancellationToken)
    {
        using var response = await EnviarAsync(HttpMethod.Get, $"/payments/{Uri.EscapeDataString(paymentId)}/pixQrCode", configuracao, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Resultado<PagamentoPixGateway>.Falha(await LerErroAsaasAsync(response, cancellationToken));
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        var payload = ObterString(root, "payload");
        if (string.IsNullOrWhiteSpace(payload))
        {
            return Resultado<PagamentoPixGateway>.Falha("O Asaas nao retornou o Pix copia e cola.");
        }

        return Resultado<PagamentoPixGateway>.Ok(new PagamentoPixGateway(
            "Asaas",
            paymentId,
            "PENDING",
            payload,
            ObterString(root, "encodedImage"),
            ObterData(root, "expirationDate"),
            null));
    }

    private async Task<HttpResponseMessage> EnviarAsync(
        HttpMethod method,
        string path,
        GatewayPagamentoConfiguracao configuracao,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"{ObterBaseUrl(configuracao.Producao)}{path}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("access_token", configuracao.AccessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: _jsonOptions);
        }

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static async Task<string> LerErroAsaasAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Asaas retornou erro {(int)response.StatusCode}.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array &&
                errors.GetArrayLength() > 0)
            {
                var first = errors[0];
                return ObterString(first, "description") ??
                    ObterString(first, "message") ??
                    $"Asaas retornou erro {(int)response.StatusCode}.";
            }

            return ObterString(document.RootElement, "message") ??
                ObterString(document.RootElement, "error") ??
                $"Asaas retornou erro {(int)response.StatusCode}.";
        }
        catch (JsonException)
        {
            return content.Length > 220 ? $"{content[..220]}..." : content;
        }
    }

    private static string ObterBaseUrl(bool producao) =>
        producao ? "https://api.asaas.com/v3" : "https://api-sandbox.asaas.com/v3";

    private static bool ProvedorAsaas(string provedor) =>
        string.Equals(provedor, "Asaas", StringComparison.OrdinalIgnoreCase);

    private static string SomenteDigitos(string? value) =>
        new((value ?? "").Where(char.IsDigit).ToArray());

    private static string? SomenteDigitosOuNull(string? value)
    {
        var digitos = SomenteDigitos(value);
        return digitos.Length == 0 ? null : digitos;
    }

    private static string? ObterString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString()
            : null;

    private static DateTime? ObterData(JsonElement element, string propertyName)
    {
        var value = ObterString(element, propertyName);
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }
}
