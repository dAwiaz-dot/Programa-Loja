using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using LojaSistema.Api.Models;
using LojaSistema.Api.Requests;
using LojaSistema.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string clienteScheme = "NanaCliente";
var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

var dataProtectionStorageRoot = builder.Configuration["NANA_STORAGE_ROOT"]?.Trim();
if (!string.IsNullOrWhiteSpace(dataProtectionStorageRoot))
{
    var keysDirectory = new DirectoryInfo(Path.Combine(dataProtectionStorageRoot, "keys"));
    builder.Services.AddDataProtection().PersistKeysToFileSystem(keysDirectory);
}

builder.Services.AddSingleton<LojaService>();
builder.Services.AddHostedService<BackupAutomaticoService>();
builder.Services.AddHttpClient<AsaasPagamentoService>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "NanaModas.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/login.html";
        options.AccessDeniedPath = "/login.html";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddCookie(clienteScheme, options =>
    {
        options.Cookie.Name = "NanaModas.Cliente";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanReadProducts", policy => policy.RequireRole("Admin", "Caixa", "Estoque"));
    options.AddPolicy("CanManageProducts", policy => policy.RequireRole("Admin", "Estoque"));
    options.AddPolicy("CanManageStorefront", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanUsePdv", policy => policy.RequireRole("Admin", "Caixa"));
    options.AddPolicy("CanManageStock", policy => policy.RequireRole("Admin", "Estoque"));
    options.AddPolicy("CanViewReports", policy => policy.RequireRole("Admin"));
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

var caminhoLogErros = Path.Combine(ObterPastaLogs(app.Environment, app.Configuration), "erros.log");
Directory.CreateDirectory(Path.GetDirectoryName(caminhoLogErros)!);

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is { } excecao)
        {
            RegistrarErroServidor(caminhoLogErros, context, excecao);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("""{"erro":"Ocorreu um erro inesperado. Tente novamente em instantes."}""");
    });
});

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
    {
        await ServirLojaInicial(context, app.Environment);
        return;
    }

    if (IsAdminStaticAsset(context.Request.Path) && context.User.Identity?.IsAuthenticated != true)
    {
        context.Response.Redirect("/login.html");
        return;
    }

    await next();
});

var uploadsDirectory = ObterPastaUploads(app.Environment, app.Configuration);
Directory.CreateDirectory(uploadsDirectory);

app.UseDefaultFiles();
if (!string.IsNullOrWhiteSpace(ObterStorageRoot(app.Configuration)))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsDirectory),
        RequestPath = "/uploads"
    });
}

app.UseStaticFiles();

app.MapGet("/admin/erros", () =>
{
    if (!File.Exists(caminhoLogErros))
    {
        return Results.Ok(Array.Empty<JsonElement>());
    }

    var ultimasLinhas = File.ReadAllLines(caminhoLogErros).TakeLast(200).Reverse();
    var registros = ultimasLinhas.Select(linha =>
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(linha);
        }
        catch (JsonException)
        {
            return default;
        }
    });
    return Results.Ok(registros);
}).RequireAuthorization("AdminOnly");

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    sistema = "Nana Modas",
    data = DateTime.UtcNow
}));

app.MapGet("/loja", () => Results.Redirect("/"));
app.MapGet("/admin", RedirecionarPainel);
app.MapGet("/painel", RedirecionarPainel);
app.MapGet("/pdv", RedirecionarPainel);

app.MapPost("/auth/login", async (LoginRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.AutenticarUsuarioPainel(request.Usuario, request.Senha);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    var usuarioPainel = resultado.Valor!;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuarioPainel.Id.ToString()),
        new(ClaimTypes.Name, usuarioPainel.Usuario),
        new(ClaimTypes.Role, usuarioPainel.Perfil),
        new("nome_exibicao", usuarioPainel.NomeExibicao)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties { IsPersistent = true });
    loja.RegistrarAtividadePainel(usuarioPainel.Usuario, "Login", "Acesso ao painel administrativo");
    return Results.Ok(new { usuario = usuarioPainel.Usuario, perfil = usuarioPainel.Perfil, nome = usuarioPainel.NomeExibicao });
}).RequireRateLimiting("login");

app.MapPost("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/auth/status", (HttpContext context) =>
{
    return Results.Ok(new
    {
        autenticado = context.User.Identity?.IsAuthenticated == true,
        usuario = context.User.Identity?.Name,
        perfil = context.User.FindFirstValue(ClaimTypes.Role),
        nome = context.User.FindFirstValue("nome_exibicao") ?? context.User.Identity?.Name
    });
}).RequireAuthorization();

app.MapGet("/usuarios-painel", (LojaService loja) =>
{
    return Results.Ok(loja.ListarUsuariosPainel());
}).RequireAuthorization("AdminOnly");

app.MapGet("/atividades-painel", (LojaService loja) =>
{
    return Results.Ok(loja.ListarAtividadesPainel());
}).RequireAuthorization("AdminOnly");

app.MapGet("/clientes-painel", (LojaService loja) =>
{
    return Results.Ok(loja.ListarClientesPainel());
}).RequireAuthorization("AdminOnly");

app.MapPost("/usuarios-painel", (CriarUsuarioPainelRequest request, LojaService loja) =>
{
    var resultado = loja.CriarUsuarioPainel(request);
    return resultado.Sucesso
        ? Results.Created($"/usuarios-painel/{resultado.Valor!.Id}", resultado.Valor)
        : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("AdminOnly");

app.MapPut("/usuarios-painel/{id:guid}", (Guid id, AtualizarUsuarioPainelRequest request, LojaService loja) =>
{
    var resultado = loja.AtualizarUsuarioPainel(id, request);
    return resultado.Sucesso ? Results.Ok(resultado.Valor) : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api", () => Results.Ok(new
{
    sistema = "Nana Modas API",
    descricao = "MVP de e-commerce, PDV e estoque em ASP.NET Core",
    rotas = new[]
    {
        "GET /health",
        "GET /categorias",
        "POST /categorias",
        "DELETE /categorias/{id}",
        "GET /produtos",
        "POST /produtos",
        "PUT /produtos/{id}",
        "DELETE /produtos/{id}",
        "PUT /produtos/{id}/vitrine",
        "GET /fornecedores",
        "POST /fornecedores",
        "PUT /fornecedores/{id}",
        "GET /loja-configuracao",
        "PUT /loja-configuracao",
        "GET /cupons",
        "POST /cupons",
        "PUT /cupons/{id}",
        "GET /opcoes-entrega",
        "POST /opcoes-entrega",
        "PUT /opcoes-entrega/{id}",
        "GET /usuarios-painel",
        "POST /usuarios-painel",
        "PUT /usuarios-painel/{id}",
        "GET /atividades-painel",
        "GET /clientes-painel",
        "POST /clientes/acessar",
        "POST /clientes/recuperar-senha",
        "POST /clientes/redefinir-senha",
        "POST /clientes/logout",
        "GET /clientes/status",
        "GET /clientes/pedidos",
        "POST /produtos/imagem",
        "POST /produtos/{id}/estoque/entrada",
        "GET /estoque/movimentacoes",
        "POST /pdv/vendas",
        "GET /pdv/vendas",
        "POST /pdv/vendas/{id}/devolucao",
        "POST /pdv/vendas/{id}/troca",
        "POST /pedidos-online",
        "POST /pagamentos/asaas/webhook",
        "GET /pedidos-online",
        "PUT /pedidos-online/{id}/status",
        "PUT /pedidos-online/{id}/pagamento",
        "GET /relatorios/resumo",
        "GET /backup/banco",
        "GET /backup/arquivos",
        "GET /backup/arquivos/{nomeArquivo}"
    }
})).RequireAuthorization();

app.MapGet("/loja-api/categorias", (LojaService loja) =>
{
    return Results.Ok(loja.ListarCategoriasLoja());
});

app.MapGet("/loja-api/produtos", (LojaService loja) =>
{
    return Results.Ok(loja.ListarProdutosLoja());
});

app.MapGet("/loja-api/configuracao", (LojaService loja) =>
{
    var config = loja.ObterConfiguracaoLoja();
    return Results.Ok(new
    {
        config.NomeCriadorSite,
        config.RazaoSocial,
        config.Cnpj,
        config.SiteUrlCanonica,
        config.PoliticaPrivacidade,
        config.PoliticaTrocaDevolucao,
        config.GoogleAnalyticsId,
        config.MetaPixelId,
        config.FreteValorPadrao,
        config.FreteGratisAcimaDe,
        config.PrazoMinimoDias,
        config.PrazoMaximoDias,
        config.MensagemFrete,
        config.MensagemLoginCliente,
        config.BannerEyebrow,
        config.BannerTitulo,
        config.BannerDescricao,
        config.BannerBotaoPrimario,
        config.BannerBotaoSecundario,
        config.BannerImagemUrl,
        config.PromocaoTopoTexto,
        config.CampanhaTitulo,
        config.CampanhaDescricao,
        config.CampanhaBotaoTexto,
        config.CampanhaImagemUrl,
        config.VitrineImagem1Url,
        config.VitrineImagem1Titulo,
        config.VitrineImagem2Url,
        config.VitrineImagem2Titulo,
        config.VitrineImagem3Url,
        config.VitrineImagem3Titulo,
        config.WhatsappLoja,
        config.InstagramLoja,
        config.EnderecoLoja,
        config.PixChave,
        config.PixNomeRecebedor,
        config.PixCidade,
        config.PixOnlineAtivo,
        config.CartaoOnlineAtivo,
        config.CheckoutCartaoNome,
        config.CheckoutCartaoUrl,
        config.MensagemPagamento,
        config.MensagemPagamentoCartao,
        config.GatewayPagamentoProvedor,
        GatewayPagamentoAtivo = config.GatewayPagamentoAtivo &&
            config.GatewayPagamentoAccessTokenConfigurado &&
            config.GatewayPagamentoWebhookSecretConfigurado &&
            string.Equals(config.GatewayPagamentoProvedor, "Asaas", StringComparison.OrdinalIgnoreCase),
        config.GatewayPagamentoProducao,
        config.GatewayPagamentoPublicKey,
        config.AtualizadoEm
    });
});

app.MapGet("/loja-api/cupons", (LojaService loja) =>
{
    return Results.Ok(loja.ListarCuponsLoja());
});

app.MapGet("/loja-api/entregas", (LojaService loja) =>
{
    return Results.Ok(loja.ListarOpcoesEntregaLoja());
});

app.MapPost("/clientes/acessar", async (AcessarClienteRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.AcessarCliente(request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    var cliente = resultado.Valor!.Cliente;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
        new(ClaimTypes.Name, cliente.Nome),
        new(ClaimTypes.Email, cliente.Email),
        new(ClaimTypes.Role, "Cliente")
    };

    var identity = new ClaimsIdentity(claims, clienteScheme);
    var principal = new ClaimsPrincipal(identity);
    await context.SignInAsync(clienteScheme, principal);

    return Results.Ok(resultado.Valor);
}).RequireRateLimiting("login");

app.MapPost("/clientes/recuperar-senha", (RecuperarSenhaClienteRequest request, LojaService loja) =>
{
    var resultado = loja.SolicitarRecuperacaoSenhaCliente(request);
    return resultado.Sucesso ? Results.Ok(new { mensagem = resultado.Valor }) : Results.BadRequest(new { erro = resultado.Erro });
}).RequireRateLimiting("login");

app.MapPost("/clientes/redefinir-senha", (RedefinirSenhaClienteRequest request, LojaService loja) =>
{
    var resultado = loja.RedefinirSenhaCliente(request);
    return resultado.Sucesso ? Results.Ok(resultado.Valor) : Results.BadRequest(new { erro = resultado.Erro });
}).RequireRateLimiting("login");

app.MapPost("/clientes/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(clienteScheme);
    return Results.Ok();
});

app.MapGet("/clientes/status", async (HttpContext context, LojaService loja) =>
{
    var authenticate = await context.AuthenticateAsync(clienteScheme);
    if (authenticate.Succeeded != true)
    {
        return Results.Ok(new { autenticado = false, cliente = (object?)null });
    }

    var idClaim = authenticate.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(idClaim, out var clienteId))
    {
        return Results.Ok(new { autenticado = false, cliente = (object?)null });
    }

    var cliente = loja.ObterCliente(clienteId);
    return cliente is null
        ? Results.Ok(new { autenticado = false, cliente = (object?)null })
        : Results.Ok(new { autenticado = true, cliente });
});

app.MapGet("/clientes/pedidos", async (HttpContext context, LojaService loja) =>
{
    var clienteId = await ObterClienteIdLogado(context, clienteScheme);
    if (clienteId is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(loja.ListarPedidosDoCliente(clienteId.Value));
});

app.MapGet("/categorias", (LojaService loja) =>
{
    return Results.Ok(loja.ListarCategorias());
}).RequireAuthorization("CanReadProducts");

app.MapPost("/categorias", (CriarCategoriaRequest request, LojaService loja) =>
{
    var resultado = loja.CriarCategoria(request);
    return resultado.Sucesso
        ? Results.Created($"/categorias/{resultado.Valor!.Id}", resultado.Valor)
        : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageProducts");

app.MapDelete("/categorias/{id:guid}", (Guid id, LojaService loja) =>
{
    var resultado = loja.ExcluirCategoria(id);
    return resultado.Sucesso
        ? Results.NoContent()
        : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageProducts");

app.MapGet("/produtos", (LojaService loja, bool? apenasAtivos) =>
{
    return Results.Ok(loja.ListarProdutos(apenasAtivos ?? true));
}).RequireAuthorization("CanReadProducts");

app.MapGet("/produtos/{id:guid}", (Guid id, LojaService loja) =>
{
    var produto = loja.ObterProduto(id);
    return produto is null ? Results.NotFound(new { erro = "Produto nao encontrado." }) : Results.Ok(produto);
}).RequireAuthorization("CanReadProducts");

app.MapPost("/produtos/imagem", async (HttpRequest request, IWebHostEnvironment environment, IConfiguration configuration) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { erro = "Envie uma imagem usando formulario multipart." });
    }

    var form = await request.ReadFormAsync();
    var arquivo = form.Files["imagem"];
    if (arquivo is null || arquivo.Length == 0)
    {
        return Results.BadRequest(new { erro = "Selecione uma imagem para enviar." });
    }

    const long tamanhoMaximo = 5 * 1024 * 1024;
    if (arquivo.Length > tamanhoMaximo)
    {
        return Results.BadRequest(new { erro = "A imagem deve ter no maximo 5 MB." });
    }

    var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
    var extensoesPermitidas = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
    if (!extensoesPermitidas.Contains(extensao) || !arquivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { erro = "Envie uma imagem JPG, PNG ou WebP." });
    }

    var pastaUploads = ObterPastaUploads(environment, configuration);
    Directory.CreateDirectory(pastaUploads);

    var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";
    var caminhoArquivo = Path.Combine(pastaUploads, nomeArquivo);

    await using var stream = File.Create(caminhoArquivo);
    await arquivo.CopyToAsync(stream);

    var imagemUrl = $"/uploads/{nomeArquivo}";
    return Results.Created(imagemUrl, new { imagemUrl });
}).RequireAuthorization("CanManageProducts");

app.MapPost("/produtos", (CriarProdutoRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.CriarProduto(request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Produto criado", resultado.Valor!.Nome);
    return Results.Created($"/produtos/{resultado.Valor!.Id}", resultado.Valor);
}).RequireAuthorization("CanManageProducts");

app.MapPut("/produtos/{id:guid}", (Guid id, AtualizarProdutoRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.AtualizarProduto(id, request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Produto atualizado", resultado.Valor!.Nome);
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("CanManageProducts");

app.MapDelete("/produtos/{id:guid}", (Guid id, LojaService loja, HttpContext context) =>
{
    var resultado = loja.ExcluirProduto(id);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Produto excluido", resultado.Valor);
    return Results.NoContent();
}).RequireAuthorization("CanManageProducts");

app.MapPut("/produtos/{id:guid}/vitrine", (Guid id, AtualizarVitrineProdutoRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.AtualizarVitrineProduto(id, request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Vitrine atualizada", resultado.Valor!.Nome);
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("CanManageStorefront");

app.MapGet("/loja-configuracao", (LojaService loja) =>
{
    return Results.Ok(loja.ObterConfiguracaoLoja());
}).RequireAuthorization("CanManageStorefront");

app.MapPut("/loja-configuracao", (AtualizarLojaConfiguracaoRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.AtualizarConfiguracaoLoja(request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Configuração do site", "Configurações atualizadas");
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("CanManageStorefront");

app.MapPost("/loja-configuracao/testar-email", (TestarEmailRequest request, LojaService loja) =>
{
    var resultado = loja.TestarEmailConfiguracao(request.Destino);
    return resultado.Sucesso ? Results.Ok(new { mensagem = resultado.Valor }) : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageStorefront");

app.MapGet("/cupons", (LojaService loja) =>
{
    return Results.Ok(loja.ListarCupons());
}).RequireAuthorization("CanManageStorefront");

app.MapPost("/cupons", (CupomDescontoRequest request, LojaService loja) =>
{
    var resultado = loja.CriarCupom(request);
    return resultado.Sucesso
        ? Results.Created($"/cupons/{resultado.Valor!.Id}", resultado.Valor)
        : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageStorefront");

app.MapPut("/cupons/{id:guid}", (Guid id, CupomDescontoRequest request, LojaService loja) =>
{
    var resultado = loja.AtualizarCupom(id, request);
    return resultado.Sucesso ? Results.Ok(resultado.Valor) : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageStorefront");

app.MapGet("/opcoes-entrega", (LojaService loja, bool? apenasAtivas) =>
{
    return Results.Ok(loja.ListarOpcoesEntrega(apenasAtivas ?? false));
}).RequireAuthorization("CanManageStorefront");

app.MapPost("/opcoes-entrega", (OpcaoEntregaRequest request, LojaService loja) =>
{
    var resultado = loja.CriarOpcaoEntrega(request);
    return resultado.Sucesso
        ? Results.Created($"/opcoes-entrega/{resultado.Valor!.Id}", resultado.Valor)
        : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageStorefront");

app.MapPut("/opcoes-entrega/{id:guid}", (Guid id, OpcaoEntregaRequest request, LojaService loja) =>
{
    var resultado = loja.AtualizarOpcaoEntrega(id, request);
    return resultado.Sucesso ? Results.Ok(resultado.Valor) : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageStorefront");

app.MapGet("/fornecedores", (LojaService loja, bool? apenasAtivos) =>
{
    return Results.Ok(loja.ListarFornecedores(apenasAtivos ?? false));
}).RequireAuthorization("CanManageStock");

app.MapPost("/fornecedores", (CriarFornecedorRequest request, LojaService loja) =>
{
    var resultado = loja.CriarFornecedor(request);
    return resultado.Sucesso
        ? Results.Created($"/fornecedores/{resultado.Valor!.Id}", resultado.Valor)
        : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageStock");

app.MapPut("/fornecedores/{id:guid}", (Guid id, AtualizarFornecedorRequest request, LojaService loja) =>
{
    var resultado = loja.AtualizarFornecedor(id, request);
    return resultado.Sucesso ? Results.Ok(resultado.Valor) : Results.BadRequest(new { erro = resultado.Erro });
}).RequireAuthorization("CanManageStock");

app.MapPost("/produtos/{id:guid}/estoque/entrada", (Guid id, EntradaEstoqueRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.RegistrarEntradaEstoque(id, request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Entrada de estoque", $"{resultado.Valor!.Nome} · {request.Quantidade} un.");
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("CanManageStock");

app.MapGet("/estoque/movimentacoes", (LojaService loja) =>
{
    return Results.Ok(loja.ListarMovimentacoesEstoque());
}).RequireAuthorization("CanManageStock");

app.MapPost("/pdv/vendas", (RegistrarVendaLojaRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.RegistrarVendaLoja(request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Venda PDV", resultado.Valor!.Total.ToString("C"));
    return Results.Created($"/pdv/vendas/{resultado.Valor!.Id}", resultado.Valor);
}).RequireAuthorization("CanUsePdv");

app.MapGet("/pdv/vendas", (LojaService loja) =>
{
    return Results.Ok(loja.ListarVendasLoja());
}).RequireAuthorization("CanUsePdv");

app.MapPost("/pdv/vendas/{id:guid}/devolucao", (Guid id, DevolverVendaLojaRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.DevolverVendaLoja(id, request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Devolução PDV", resultado.Valor!.Id.ToString()[..8].ToUpperInvariant());
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("CanUsePdv");

app.MapPost("/pdv/vendas/{id:guid}/troca", (Guid id, TrocarVendaLojaRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.TrocarVendaLoja(id, request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Troca PDV", resultado.Valor!.VendaTroca.Id.ToString()[..8].ToUpperInvariant());
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("CanUsePdv");

app.MapPost("/pedidos-online", async (
    RegistrarPedidoOnlineRequest request,
    LojaService loja,
    AsaasPagamentoService asaas,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    var clienteId = await ObterClienteIdLogado(context, clienteScheme);
    var gateway = loja.ObterGatewayPagamentoConfiguracao();
    if (DeveGerarPixGateway(request.FormaPagamento, gateway) && !DocumentoValidoAsaas(request.DocumentoCliente))
    {
        return Results.BadRequest(new { erro = "Informe CPF ou CNPJ para gerar o QR Code Pix." });
    }

    var resultado = loja.RegistrarPedidoOnline(request, clienteId);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    var pedido = resultado.Valor!;
    if (DeveGerarPixGateway(request.FormaPagamento, gateway))
    {
        var pix = await asaas.CriarPixAsync(pedido, gateway, cancellationToken);
        if (!pix.Sucesso)
        {
            loja.AtualizarStatusPedidoOnline(pedido.Id, new AtualizarStatusPedidoOnlineRequest(StatusPedidoOnline.Cancelado));
            return Results.BadRequest(new { erro = pix.Erro ?? "Nao foi possivel gerar o QR Code Pix." });
        }

        var pagamento = loja.RegistrarPixGatewayPedido(pedido.Id, pix.Valor!);
        if (pagamento.Sucesso)
        {
            pedido = pagamento.Valor!;
        }
    }

    return Results.Created($"/pedidos-online/{pedido.Id}", pedido);
});

app.MapPost("/pagamentos/asaas/webhook", async (HttpContext context, LojaService loja) =>
{
    var tokenRecebido = context.Request.Headers["asaas-access-token"].FirstOrDefault();
    if (!loja.WebhookGatewayAutorizado(tokenRecebido))
    {
        return Results.Unauthorized();
    }

    try
    {
        using var document = await JsonDocument.ParseAsync(context.Request.Body);
        var root = document.RootElement;
        var evento = ObterJsonString(root, "event");
        var payment = root.TryGetProperty("payment", out var paymentElement) ? paymentElement : default;
        var pagamentoId = payment.ValueKind == JsonValueKind.Object ? ObterJsonString(payment, "id") : null;
        var referenciaExterna = payment.ValueKind == JsonValueKind.Object ? ObterJsonString(payment, "externalReference") : null;
        var status = payment.ValueKind == JsonValueKind.Object ? ObterJsonString(payment, "status") : null;

        var resultado = loja.ProcessarWebhookPagamentoAsaas(evento, pagamentoId, referenciaExterna, status);
        return Results.Ok(new { received = true, pedidoAtualizado = resultado.Sucesso });
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { erro = "Webhook Asaas invalido." });
    }
});

app.MapGet("/pedidos-online", (LojaService loja) =>
{
    return Results.Ok(loja.ListarPedidosOnline());
}).RequireAuthorization("AdminOnly");

app.MapPut("/pedidos-online/{id:guid}/status", (Guid id, AtualizarStatusPedidoOnlineRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.AtualizarStatusPedidoOnline(id, request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Status pedido online", $"{resultado.Valor!.Id.ToString()[..8].ToUpperInvariant()} · {resultado.Valor.Status}");
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("AdminOnly");

app.MapPut("/pedidos-online/{id:guid}/rastreamento", (Guid id, AtualizarRastreamentoPedidoOnlineRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.AtualizarRastreamentoPedidoOnline(id, request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Rastreio pedido online", resultado.Valor!.Id.ToString()[..8].ToUpperInvariant());
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("AdminOnly");

app.MapPut("/pedidos-online/{id:guid}/pagamento", (Guid id, AtualizarPagamentoPedidoOnlineRequest request, LojaService loja, HttpContext context) =>
{
    var resultado = loja.AtualizarPagamentoPedidoOnline(id, request);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Pagamento pedido online", resultado.Valor!.Id.ToString()[..8].ToUpperInvariant());
    return Results.Ok(resultado.Valor);
}).RequireAuthorization("AdminOnly");

app.MapGet("/relatorios/resumo", (LojaService loja) =>
{
    return Results.Ok(loja.GerarResumo());
}).RequireAuthorization("CanViewReports");

app.MapGet("/backup/banco", (LojaService loja, HttpContext context) =>
{
    var backup = loja.CriarBackupBanco();
    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Backup do banco", backup.NomeArquivo);
    return Results.File(backup.Conteudo, "application/octet-stream", backup.NomeArquivo);
}).RequireAuthorization("AdminOnly");

app.MapGet("/backup/arquivos", (LojaService loja) =>
{
    return Results.Ok(loja.ListarBackups());
}).RequireAuthorization("AdminOnly");

app.MapGet("/backup/arquivos/{nomeArquivo}", (string nomeArquivo, LojaService loja, HttpContext context) =>
{
    var resultado = loja.ObterBackupArquivo(nomeArquivo);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.Erro });
    }

    loja.RegistrarAtividadePainel(UsuarioPainelAtual(context), "Download backup automático", resultado.Valor.NomeArquivo);
    return Results.File(resultado.Valor.Conteudo, "application/octet-stream", resultado.Valor.NomeArquivo);
}).RequireAuthorization("AdminOnly");

app.Run();

static bool IsAdminStaticAsset(PathString path)
{
    return path == "/index.html" ||
        path == "/app.js" ||
        path == "/styles.css";
}

static async Task ServirLojaInicial(HttpContext context, IWebHostEnvironment environment)
{
    context.Response.ContentType = "text/html; charset=utf-8";
    if (HttpMethods.IsHead(context.Request.Method))
    {
        return;
    }

    var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    await context.Response.SendFileAsync(Path.Combine(webRoot, "loja.html"));
}

static IResult RedirecionarPainel(HttpContext context)
{
    return context.User.Identity?.IsAuthenticated == true
        ? Results.Redirect("/index.html")
        : Results.Redirect("/login.html");
}

static bool IsApiRequest(HttpRequest request)
{
    return request.Path.StartsWithSegments("/api") ||
        request.Path.StartsWithSegments("/auth") ||
        request.Path.StartsWithSegments("/clientes") ||
        request.Path.StartsWithSegments("/categorias") ||
        request.Path.StartsWithSegments("/produtos") ||
        request.Path.StartsWithSegments("/fornecedores") ||
        request.Path.StartsWithSegments("/cupons") ||
        request.Path.StartsWithSegments("/opcoes-entrega") ||
        request.Path.StartsWithSegments("/usuarios-painel") ||
        request.Path.StartsWithSegments("/atividades-painel") ||
        request.Path.StartsWithSegments("/clientes-painel") ||
        request.Path.StartsWithSegments("/loja-configuracao") ||
        request.Path.StartsWithSegments("/estoque") ||
        request.Path.StartsWithSegments("/pdv") ||
        request.Path.StartsWithSegments("/relatorios") ||
        request.Path.StartsWithSegments("/backup") ||
        request.Path.StartsWithSegments("/pagamentos") ||
        request.Path.StartsWithSegments("/pedidos-online");
}

static async Task<Guid?> ObterClienteIdLogado(HttpContext context, string scheme)
{
    var authenticate = await context.AuthenticateAsync(scheme);
    var idClaim = authenticate.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(idClaim, out var clienteId) ? clienteId : null;
}

static string UsuarioPainelAtual(HttpContext context)
{
    return context.User.Identity?.Name ?? "painel";
}

static bool DeveGerarPixGateway(FormaPagamento formaPagamento, GatewayPagamentoConfiguracao gateway)
{
    return formaPagamento == FormaPagamento.Pix &&
        gateway.Ativo &&
        !string.IsNullOrWhiteSpace(gateway.AccessToken) &&
        string.Equals(gateway.Provedor, "Asaas", StringComparison.OrdinalIgnoreCase);
}

static bool DocumentoValidoAsaas(string? documento)
{
    var digitos = new string((documento ?? "").Where(char.IsDigit).ToArray());
    return digitos.Length is 11 or 14;
}

static string ObterPastaUploads(IWebHostEnvironment environment, IConfiguration configuration)
{
    var storageRoot = ObterStorageRoot(configuration);
    if (!string.IsNullOrWhiteSpace(storageRoot))
    {
        return Path.Combine(storageRoot, "uploads");
    }

    var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    return Path.Combine(webRoot, "uploads");
}

static string? ObterStorageRoot(IConfiguration configuration)
{
    return configuration["NANA_STORAGE_ROOT"]?.Trim();
}

static string ObterPastaLogs(IWebHostEnvironment environment, IConfiguration configuration)
{
    var storageRoot = ObterStorageRoot(configuration);
    var baseDir = !string.IsNullOrWhiteSpace(storageRoot)
        ? Path.Combine(storageRoot, "Data")
        : Path.Combine(environment.ContentRootPath, "Data");
    return Path.Combine(baseDir, "logs");
}

static void RegistrarErroServidor(string caminhoLog, HttpContext context, Exception excecao)
{
    try
    {
        lock (ErroLogSync.Lock)
        {
            var linha = JsonSerializer.Serialize(new
            {
                quando = DateTime.UtcNow,
                metodo = context.Request.Method,
                caminho = context.Request.Path.ToString(),
                tipo = excecao.GetType().Name,
                mensagem = excecao.Message
            });

            File.AppendAllText(caminhoLog, linha + Environment.NewLine);
            LimitarTamanhoLogErros(caminhoLog);
        }
    }
    catch
    {
        // Nunca deixar uma falha ao gravar o log derrubar o tratamento de erro em si.
    }
}

static void LimitarTamanhoLogErros(string caminhoLog)
{
    const int maximoLinhas = 1000;
    const int manterLinhas = 500;
    var linhas = File.ReadAllLines(caminhoLog);
    if (linhas.Length > maximoLinhas)
    {
        File.WriteAllLines(caminhoLog, linhas[^manterLinhas..]);
    }
}

static string? ObterJsonString(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
        ? property.GetString()
        : null;
}

public sealed record LoginRequest(string? Usuario, string? Senha);

static class ErroLogSync
{
    public static readonly object Lock = new();
}
