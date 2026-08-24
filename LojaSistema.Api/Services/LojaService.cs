using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LojaSistema.Api.Models;
using LojaSistema.Api.Requests;
using LojaSistema.Api.Responses;
using Microsoft.Data.Sqlite;

namespace LojaSistema.Api.Services;

public sealed class LojaService
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Categoria> _categorias = [];
    private readonly Dictionary<Guid, Produto> _produtos = [];
    private readonly Dictionary<Guid, Cliente> _clientes = [];
    private readonly Dictionary<Guid, UsuarioPainel> _usuariosPainel = [];
    private readonly Dictionary<Guid, Fornecedor> _fornecedores = [];
    private readonly Dictionary<Guid, CupomDesconto> _cupons = [];
    private readonly Dictionary<Guid, OpcaoEntrega> _opcoesEntrega = [];
    private readonly List<VendaLoja> _vendasLoja = [];
    private readonly List<PedidoOnline> _pedidosOnline = [];
    private readonly List<EstoqueMovimentacao> _movimentacoes = [];
    private readonly List<AtividadePainel> _atividadesPainel = [];
    private readonly LojaConfiguracao _configuracaoLoja = new();
    private readonly string _connectionString;
    private readonly string _databasePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private const int SenhaIteracoes = 100_000;
    private const int SenhaSaltBytes = 16;
    private const int SenhaHashBytes = 32;
    private const int SmtpTimeoutMs = 45_000;
    private static readonly HttpClient EmailHttpClient = new()
    {
        Timeout = TimeSpan.FromMilliseconds(SmtpTimeoutMs)
    };
    private const string MigracaoZerarEstoqueEntrega = "2026-07-06-zerar-estoque-entrega";
    private const string MigracaoRenomearEntregaLocal = "2026-07-06-renomear-entrega-local";

    public LojaService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var storageRoot = configuration["NANA_STORAGE_ROOT"]?.Trim();
        var dataDirectory = !string.IsNullOrWhiteSpace(storageRoot)
            ? Path.Combine(storageRoot, "Data")
            : Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);
        _databasePath = Path.Combine(dataDirectory, "nana-modas.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath
        }.ToString();

        InicializarBanco();
        CarregarDados();

        var precisaSalvar = false;
        if (_categorias.Count == 0 && _produtos.Count == 0)
        {
            CriarDadosIniciais();
            precisaSalvar = true;
        }

        if (_cupons.Count == 0)
        {
            CriarCupomPadrao();
            precisaSalvar = true;
        }

        if (_opcoesEntrega.Count == 0)
        {
            CriarOpcoesEntregaPadrao();
            precisaSalvar = true;
        }

        if (_usuariosPainel.Count == 0)
        {
            CriarUsuariosPainelPadrao();
            precisaSalvar = true;
        }

        if (string.Equals(_configuracaoLoja.BannerImagemUrl, "/uploads/eafbc4c1fec34d65b5dc742950ecb385.jpeg", StringComparison.OrdinalIgnoreCase))
        {
            _configuracaoLoja.BannerImagemUrl = "";
            precisaSalvar = true;
        }

        if (_configuracaoLoja.GatewayPagamentoAtivo && !GatewayPagamentoMinimoConfigurado())
        {
            _configuracaoLoja.GatewayPagamentoAtivo = false;
            precisaSalvar = true;
        }

        var estoqueZeradoParaEntrega = ZerarEstoqueParaEntregaSePendente();
        precisaSalvar |= estoqueZeradoParaEntrega;
        var entregaLocalRenomeada = RenomearEntregaLocalSePendente();
        precisaSalvar |= entregaLocalRenomeada;

        if (precisaSalvar)
        {
            SalvarTudo();
        }

        if (estoqueZeradoParaEntrega)
        {
            RegistrarMigracaoAplicada(MigracaoZerarEstoqueEntrega);
        }

        if (entregaLocalRenomeada)
        {
            RegistrarMigracaoAplicada(MigracaoRenomearEntregaLocal);
        }
    }

    public Resultado<UsuarioPainelResponse> AutenticarUsuarioPainel(string? usuarioEntrada, string? senha)
    {
        var usuario = NormalizarUsuarioPainel(usuarioEntrada);
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha))
        {
            return Resultado<UsuarioPainelResponse>.Falha("Usuario ou senha incorretos.");
        }

        lock (_sync)
        {
            var usuarioPainel = _usuariosPainel.Values
                .FirstOrDefault(item => string.Equals(item.Usuario, usuario, StringComparison.OrdinalIgnoreCase));

            if (usuarioPainel is null || !usuarioPainel.Ativo || !VerificarSenha(senha, usuarioPainel.SenhaHash))
            {
                return Resultado<UsuarioPainelResponse>.Falha("Usuario ou senha incorretos.");
            }

            return Resultado<UsuarioPainelResponse>.Ok(CriarUsuarioPainelResponse(usuarioPainel));
        }
    }

    public IReadOnlyList<UsuarioPainelResponse> ListarUsuariosPainel()
    {
        lock (_sync)
        {
            return _usuariosPainel.Values
                .OrderByDescending(usuario => usuario.Ativo)
                .ThenBy(usuario => PerfilOrdem(usuario.Perfil))
                .ThenBy(usuario => usuario.Usuario)
                .Select(CriarUsuarioPainelResponse)
                .ToList();
        }
    }

    public IReadOnlyList<AtividadePainel> ListarAtividadesPainel()
    {
        lock (_sync)
        {
            return _atividadesPainel
                .OrderByDescending(atividade => atividade.CriadaEm)
                .Take(80)
                .ToList();
        }
    }

    public void RegistrarAtividadePainel(string? usuario, string acao, string? detalhe)
    {
        lock (_sync)
        {
            _atividadesPainel.Add(new AtividadePainel
            {
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "sistema" : usuario,
                Acao = acao,
                Detalhe = NormalizarTextoOpcional(detalhe)
            });

            if (_atividadesPainel.Count > 500)
            {
                _atividadesPainel.RemoveRange(0, _atividadesPainel.Count - 500);
            }

            SalvarTudo();
        }
    }

    public Resultado<UsuarioPainelResponse> CriarUsuarioPainel(CriarUsuarioPainelRequest request)
    {
        var validacao = ValidarUsuarioPainel(request.Usuario, request.NomeExibicao, request.Perfil, request.Senha, senhaObrigatoria: true, out var usuario, out var nome, out var perfil);
        if (validacao is not null)
        {
            return Resultado<UsuarioPainelResponse>.Falha(validacao);
        }

        lock (_sync)
        {
            if (_usuariosPainel.Values.Any(item => string.Equals(item.Usuario, usuario, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<UsuarioPainelResponse>.Falha("Ja existe um usuario com esse login.");
            }

            var usuarioPainel = new UsuarioPainel
            {
                Usuario = usuario,
                NomeExibicao = nome,
                Perfil = perfil,
                SenhaHash = CriarHashSenha(request.Senha!),
                Ativo = request.Ativo
            };

            _usuariosPainel[usuarioPainel.Id] = usuarioPainel;
            SalvarTudo();
            return Resultado<UsuarioPainelResponse>.Ok(CriarUsuarioPainelResponse(usuarioPainel));
        }
    }

    public Resultado<UsuarioPainelResponse> AtualizarUsuarioPainel(Guid id, AtualizarUsuarioPainelRequest request)
    {
        var validacao = ValidarUsuarioPainel(request.Usuario, request.NomeExibicao, request.Perfil, request.Senha, senhaObrigatoria: false, out var usuario, out var nome, out var perfil);
        if (validacao is not null)
        {
            return Resultado<UsuarioPainelResponse>.Falha(validacao);
        }

        lock (_sync)
        {
            if (!_usuariosPainel.TryGetValue(id, out var usuarioPainel))
            {
                return Resultado<UsuarioPainelResponse>.Falha("Usuario nao encontrado.");
            }

            if (_usuariosPainel.Values.Any(item => item.Id != id && string.Equals(item.Usuario, usuario, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<UsuarioPainelResponse>.Falha("Ja existe um usuario com esse login.");
            }

            var deixariaSemAdmin = usuarioPainel.Ativo &&
                string.Equals(usuarioPainel.Perfil, "Admin", StringComparison.OrdinalIgnoreCase) &&
                (!request.Ativo || !string.Equals(perfil, "Admin", StringComparison.OrdinalIgnoreCase)) &&
                _usuariosPainel.Values.Count(item => item.Ativo && string.Equals(item.Perfil, "Admin", StringComparison.OrdinalIgnoreCase)) <= 1;

            if (deixariaSemAdmin)
            {
                return Resultado<UsuarioPainelResponse>.Falha("Mantenha pelo menos um administrador ativo.");
            }

            usuarioPainel.Usuario = usuario;
            usuarioPainel.NomeExibicao = nome;
            usuarioPainel.Perfil = perfil;
            usuarioPainel.Ativo = request.Ativo;
            usuarioPainel.AtualizadoEm = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Senha))
            {
                usuarioPainel.SenhaHash = CriarHashSenha(request.Senha);
            }

            SalvarTudo();
            return Resultado<UsuarioPainelResponse>.Ok(CriarUsuarioPainelResponse(usuarioPainel));
        }
    }

    public IReadOnlyList<Categoria> ListarCategorias()
    {
        lock (_sync)
        {
            return _categorias.Values
                .OrderBy(categoria => categoria.Nome)
                .ToList();
        }
    }

    public IReadOnlyList<Categoria> ListarCategoriasLoja()
    {
        lock (_sync)
        {
            var categoriasPublicadas = _produtos.Values
                .Where(produto => produto.Ativo && produto.PublicadoNaLoja && produto.QuantidadeEmEstoque > 0)
                .Select(produto => produto.CategoriaId)
                .ToHashSet();

            foreach (var categoria in _categorias.Values)
            {
                if (categoria.CategoriaPaiId is Guid paiId && categoriasPublicadas.Contains(categoria.Id))
                {
                    categoriasPublicadas.Add(paiId);
                }
            }

            return _categorias.Values
                .Where(categoria => categoriasPublicadas.Contains(categoria.Id))
                .OrderBy(categoria => categoria.Nome)
                .ToList();
        }
    }

    public Resultado<Categoria> CriarCategoria(CriarCategoriaRequest request)
    {
        var nome = NormalizarTexto(request.Nome);
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Resultado<Categoria>.Falha("Informe o nome da categoria.");
        }

        lock (_sync)
        {
            if (_categorias.Values.Any(categoria => string.Equals(categoria.Nome, nome, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<Categoria>.Falha("Ja existe uma categoria com esse nome.");
            }

            Guid? categoriaPaiId = null;
            if (!string.IsNullOrWhiteSpace(request.CategoriaPaiId))
            {
                if (!Guid.TryParse(request.CategoriaPaiId, out var paiId) || !_categorias.TryGetValue(paiId, out var pai))
                {
                    return Resultado<Categoria>.Falha("Categoria pai nao encontrada.");
                }

                if (pai.CategoriaPaiId is not null)
                {
                    return Resultado<Categoria>.Falha("Nao e possivel criar subcategoria dentro de outra subcategoria.");
                }

                categoriaPaiId = paiId;
            }

            var categoria = new Categoria { Nome = nome, CategoriaPaiId = categoriaPaiId };
            _categorias[categoria.Id] = categoria;
            SalvarTudo();
            return Resultado<Categoria>.Ok(categoria);
        }
    }

    public Resultado<bool> ExcluirCategoria(Guid id)
    {
        lock (_sync)
        {
            if (!_categorias.TryGetValue(id, out _))
            {
                return Resultado<bool>.Falha("Categoria nao encontrada.");
            }

            if (_categorias.Values.Any(categoria => categoria.CategoriaPaiId == id))
            {
                return Resultado<bool>.Falha("Exclua ou mova as subcategorias antes de excluir esta categoria.");
            }

            if (_produtos.Values.Any(produto => produto.CategoriaId == id))
            {
                return Resultado<bool>.Falha("Existem produtos cadastrados nesta categoria. Mova ou exclua os produtos antes.");
            }

            _categorias.Remove(id);
            SalvarTudo();
            return Resultado<bool>.Ok(true);
        }
    }

    public IReadOnlyList<Fornecedor> ListarFornecedores(bool apenasAtivos)
    {
        lock (_sync)
        {
            return _fornecedores.Values
                .Where(fornecedor => !apenasAtivos || fornecedor.Ativo)
                .OrderBy(fornecedor => fornecedor.Nome)
                .ToList();
        }
    }

    public Resultado<Fornecedor> CriarFornecedor(CriarFornecedorRequest request)
    {
        var nome = NormalizarTexto(request.Nome);
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Resultado<Fornecedor>.Falha("Informe o nome do fornecedor.");
        }

        lock (_sync)
        {
            if (_fornecedores.Values.Any(fornecedor => string.Equals(fornecedor.Nome, nome, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<Fornecedor>.Falha("Ja existe um fornecedor com esse nome.");
            }

            var fornecedor = new Fornecedor
            {
                Nome = nome,
                Documento = NormalizarTextoOpcional(request.Documento),
                Telefone = NormalizarTextoOpcional(request.Telefone),
                Email = NormalizarTextoOpcional(request.Email)
            };

            _fornecedores[fornecedor.Id] = fornecedor;
            SalvarTudo();
            return Resultado<Fornecedor>.Ok(fornecedor);
        }
    }

    public Resultado<Fornecedor> AtualizarFornecedor(Guid id, AtualizarFornecedorRequest request)
    {
        var nome = NormalizarTexto(request.Nome);
        if (string.IsNullOrWhiteSpace(nome))
        {
            return Resultado<Fornecedor>.Falha("Informe o nome do fornecedor.");
        }

        lock (_sync)
        {
            if (!_fornecedores.TryGetValue(id, out var fornecedor))
            {
                return Resultado<Fornecedor>.Falha("Fornecedor nao encontrado.");
            }

            if (_fornecedores.Values.Any(item => item.Id != id && string.Equals(item.Nome, nome, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<Fornecedor>.Falha("Ja existe um fornecedor com esse nome.");
            }

            fornecedor.Nome = nome;
            fornecedor.Documento = NormalizarTextoOpcional(request.Documento);
            fornecedor.Telefone = NormalizarTextoOpcional(request.Telefone);
            fornecedor.Email = NormalizarTextoOpcional(request.Email);
            fornecedor.Ativo = request.Ativo;
            fornecedor.AtualizadoEm = DateTime.UtcNow;

            SalvarTudo();
            return Resultado<Fornecedor>.Ok(fornecedor);
        }
    }

    public IReadOnlyList<ProdutoResponse> ListarProdutos(bool apenasAtivos)
    {
        lock (_sync)
        {
            return _produtos.Values
                .Where(produto => !apenasAtivos || produto.Ativo)
                .OrderBy(produto => produto.Nome)
                .Select(CriarProdutoResponse)
                .ToList();
        }
    }

    public IReadOnlyList<ProdutoLojaResponse> ListarProdutosLoja()
    {
        lock (_sync)
        {
            return _produtos.Values
                .Where(produto => produto.Ativo && produto.PublicadoNaLoja)
                .OrderByDescending(produto => produto.DestaqueLoja)
                .ThenBy(produto => produto.OrdemLoja)
                .ThenBy(ObterNomeProdutoLoja)
                .Select(CriarProdutoLojaResponse)
                .ToList();
        }
    }

    public LojaConfiguracaoResponse ObterConfiguracaoLoja()
    {
        lock (_sync)
        {
            return CriarLojaConfiguracaoResponse();
        }
    }

    public IReadOnlyList<CupomDescontoResponse> ListarCupons()
    {
        lock (_sync)
        {
            return _cupons.Values
                .OrderByDescending(CupomDisponivelNaLoja)
                .ThenBy(cupom => cupom.Codigo)
                .Select(CriarCupomDescontoResponse)
                .ToList();
        }
    }

    public IReadOnlyList<CupomLojaResponse> ListarCuponsLoja()
    {
        lock (_sync)
        {
            return _cupons.Values
                .Where(CupomDisponivelNaLoja)
                .OrderByDescending(cupom => cupom.PercentualDesconto)
                .ThenBy(cupom => cupom.ValorMinimoPedido)
                .Select(cupom => new CupomLojaResponse(
                    cupom.Codigo,
                    cupom.Descricao,
                    cupom.PercentualDesconto,
                    cupom.ValorMinimoPedido,
                    cupom.ValidoAte))
                .ToList();
        }
    }

    public Resultado<CupomDescontoResponse> CriarCupom(CupomDescontoRequest request)
    {
        var validacao = ValidarCupom(request, out var codigo);
        if (validacao is not null)
        {
            return Resultado<CupomDescontoResponse>.Falha(validacao);
        }

        lock (_sync)
        {
            if (_cupons.Values.Any(cupom => string.Equals(cupom.Codigo, codigo, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<CupomDescontoResponse>.Falha("Ja existe um cupom com esse codigo.");
            }

            var cupom = new CupomDesconto
            {
                Codigo = codigo,
                Descricao = NormalizarTextoOpcional(request.Descricao),
                PercentualDesconto = request.PercentualDesconto,
                ValorMinimoPedido = request.ValorMinimoPedido,
                Ativo = request.Ativo,
                ValidoAte = request.ValidoAte
            };

            _cupons[cupom.Id] = cupom;
            SalvarTudo();
            return Resultado<CupomDescontoResponse>.Ok(CriarCupomDescontoResponse(cupom));
        }
    }

    public Resultado<CupomDescontoResponse> AtualizarCupom(Guid id, CupomDescontoRequest request)
    {
        var validacao = ValidarCupom(request, out var codigo);
        if (validacao is not null)
        {
            return Resultado<CupomDescontoResponse>.Falha(validacao);
        }

        lock (_sync)
        {
            if (!_cupons.TryGetValue(id, out var cupom))
            {
                return Resultado<CupomDescontoResponse>.Falha("Cupom nao encontrado.");
            }

            if (_cupons.Values.Any(item => item.Id != id && string.Equals(item.Codigo, codigo, StringComparison.OrdinalIgnoreCase)))
            {
                return Resultado<CupomDescontoResponse>.Falha("Ja existe um cupom com esse codigo.");
            }

            cupom.Codigo = codigo;
            cupom.Descricao = NormalizarTextoOpcional(request.Descricao);
            cupom.PercentualDesconto = request.PercentualDesconto;
            cupom.ValorMinimoPedido = request.ValorMinimoPedido;
            cupom.Ativo = request.Ativo;
            cupom.ValidoAte = request.ValidoAte;
            cupom.AtualizadoEm = DateTime.UtcNow;

            SalvarTudo();
            return Resultado<CupomDescontoResponse>.Ok(CriarCupomDescontoResponse(cupom));
        }
    }

    public IReadOnlyList<OpcaoEntregaResponse> ListarOpcoesEntrega(bool apenasAtivas)
    {
        lock (_sync)
        {
            return _opcoesEntrega.Values
                .Where(opcao => !apenasAtivas || opcao.Ativo)
                .OrderBy(opcao => opcao.Ordem)
                .ThenBy(opcao => opcao.Nome)
                .Select(CriarOpcaoEntregaResponse)
                .ToList();
        }
    }

    public IReadOnlyList<OpcaoEntregaResponse> ListarOpcoesEntregaLoja()
    {
        lock (_sync)
        {
            return _opcoesEntrega.Values
                .Where(opcao => opcao.Ativo)
                .OrderBy(opcao => opcao.Ordem)
                .ThenBy(opcao => opcao.Valor)
                .Select(CriarOpcaoEntregaResponse)
                .ToList();
        }
    }

    public Resultado<OpcaoEntregaResponse> CriarOpcaoEntrega(OpcaoEntregaRequest request)
    {
        var validacao = ValidarOpcaoEntrega(request, out var tipo, out var cepInicial, out var cepFinal);
        if (validacao is not null)
        {
            return Resultado<OpcaoEntregaResponse>.Falha(validacao);
        }

        lock (_sync)
        {
            var opcao = new OpcaoEntrega
            {
                Nome = NormalizarTexto(request.Nome),
                Tipo = tipo,
                Descricao = NormalizarTextoOpcional(request.Descricao),
                Valor = request.Valor,
                FreteGratisAcimaDe = request.FreteGratisAcimaDe,
                PrazoMinimoDias = request.PrazoMinimoDias,
                PrazoMaximoDias = request.PrazoMaximoDias,
                CepInicial = cepInicial,
                CepFinal = cepFinal,
                Cidades = NormalizarListaTexto(request.Cidades),
                Bairros = NormalizarListaTexto(request.Bairros),
                Estados = NormalizarListaEstados(request.Estados),
                Ativo = request.Ativo,
                Ordem = request.Ordem
            };

            _opcoesEntrega[opcao.Id] = opcao;
            SalvarTudo();
            return Resultado<OpcaoEntregaResponse>.Ok(CriarOpcaoEntregaResponse(opcao));
        }
    }

    public Resultado<OpcaoEntregaResponse> AtualizarOpcaoEntrega(Guid id, OpcaoEntregaRequest request)
    {
        var validacao = ValidarOpcaoEntrega(request, out var tipo, out var cepInicial, out var cepFinal);
        if (validacao is not null)
        {
            return Resultado<OpcaoEntregaResponse>.Falha(validacao);
        }

        lock (_sync)
        {
            if (!_opcoesEntrega.TryGetValue(id, out var opcao))
            {
                return Resultado<OpcaoEntregaResponse>.Falha("Opcao de entrega nao encontrada.");
            }

            opcao.Nome = NormalizarTexto(request.Nome);
            opcao.Tipo = tipo;
            opcao.Descricao = NormalizarTextoOpcional(request.Descricao);
            opcao.Valor = request.Valor;
            opcao.FreteGratisAcimaDe = request.FreteGratisAcimaDe;
            opcao.PrazoMinimoDias = request.PrazoMinimoDias;
            opcao.PrazoMaximoDias = request.PrazoMaximoDias;
            opcao.CepInicial = cepInicial;
            opcao.CepFinal = cepFinal;
            opcao.Cidades = NormalizarListaTexto(request.Cidades);
            opcao.Bairros = NormalizarListaTexto(request.Bairros);
            opcao.Estados = NormalizarListaEstados(request.Estados);
            opcao.Ativo = request.Ativo;
            opcao.Ordem = request.Ordem;
            opcao.AtualizadoEm = DateTime.UtcNow;

            SalvarTudo();
            return Resultado<OpcaoEntregaResponse>.Ok(CriarOpcaoEntregaResponse(opcao));
        }
    }

    public ClienteResponse? ObterCliente(Guid id)
    {
        lock (_sync)
        {
            return _clientes.TryGetValue(id, out var cliente) ? CriarClienteResponse(cliente) : null;
        }
    }

    public Resultado<ClienteAcessoResponse> AcessarCliente(AcessarClienteRequest request)
    {
        var nome = NormalizarTexto(request.Nome);
        var email = NormalizarEmail(request.Email);
        var telefone = NormalizarTextoOpcional(request.Telefone);

        if (string.IsNullOrWhiteSpace(nome))
        {
            return Resultado<ClienteAcessoResponse>.Falha("Informe o nome completo.");
        }

        if (!EmailValido(email))
        {
            return Resultado<ClienteAcessoResponse>.Falha("Informe um e-mail valido.");
        }

        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 6)
        {
            return Resultado<ClienteAcessoResponse>.Falha("A senha precisa ter pelo menos 6 caracteres.");
        }

        lock (_sync)
        {
            var cliente = _clientes.Values.FirstOrDefault(item => string.Equals(item.Email, email, StringComparison.OrdinalIgnoreCase));
            if (cliente is not null)
            {
                if (!VerificarSenha(request.Senha, cliente.SenhaHash))
                {
                    return Resultado<ClienteAcessoResponse>.Falha("E-mail ou senha incorretos.");
                }

                cliente.Nome = nome;
                cliente.Telefone = telefone;
                cliente.AtualizadoEm = DateTime.UtcNow;
                SalvarTudo();
                return Resultado<ClienteAcessoResponse>.Ok(new ClienteAcessoResponse(false, CriarClienteResponse(cliente)));
            }

            cliente = new Cliente
            {
                Nome = nome,
                Email = email,
                Telefone = telefone,
                SenhaHash = CriarHashSenha(request.Senha)
            };

            _clientes[cliente.Id] = cliente;
            SalvarTudo();
            return Resultado<ClienteAcessoResponse>.Ok(new ClienteAcessoResponse(true, CriarClienteResponse(cliente)));
        }
    }

    public Resultado<string> SolicitarRecuperacaoSenhaCliente(RecuperarSenhaClienteRequest request)
    {
        var email = NormalizarEmailOpcional(request.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return Resultado<string>.Falha("Informe um e-mail valido.");
        }

        lock (_sync)
        {
            var cliente = _clientes.Values.FirstOrDefault(item => string.Equals(item.Email, email, StringComparison.OrdinalIgnoreCase));
            if (cliente is null)
            {
                return Resultado<string>.Ok("Se o e-mail estiver cadastrado, enviaremos um código de recuperação.");
            }

            var codigo = RandomNumberGenerator.GetInt32(100000, 999999).ToString(CultureInfo.InvariantCulture);
            cliente.CodigoRecuperacaoHash = CriarHashSenha(codigo);
            cliente.CodigoRecuperacaoExpiraEm = DateTime.UtcNow.AddMinutes(20);
            cliente.AtualizadoEm = DateTime.UtcNow;
            SalvarTudo();

            var mensagem = "Se o e-mail estiver cadastrado, enviaremos um código de recuperação.";
            if (EmailConfigurado())
            {
                TentarEnviarEmailSimples(
                    cliente.Email,
                    "Código de recuperação - Nana Modas",
                    $"Olá, {cliente.Nome}.{Environment.NewLine}{Environment.NewLine}Seu código de recuperação é: {codigo}{Environment.NewLine}Ele vale por 20 minutos.");
            }
            else
            {
                mensagem = $"Código de recuperação gerado para teste local: {codigo}";
            }

            return Resultado<string>.Ok(mensagem);
        }
    }

    public Resultado<ClienteResponse> RedefinirSenhaCliente(RedefinirSenhaClienteRequest request)
    {
        var email = NormalizarEmailOpcional(request.Email);
        var codigo = NormalizarTextoOpcional(request.Codigo);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(codigo))
        {
            return Resultado<ClienteResponse>.Falha("Informe e-mail e código de recuperação.");
        }

        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 6)
        {
            return Resultado<ClienteResponse>.Falha("A nova senha precisa ter pelo menos 6 caracteres.");
        }

        lock (_sync)
        {
            var cliente = _clientes.Values.FirstOrDefault(item => string.Equals(item.Email, email, StringComparison.OrdinalIgnoreCase));
            if (cliente is null ||
                string.IsNullOrWhiteSpace(cliente.CodigoRecuperacaoHash) ||
                cliente.CodigoRecuperacaoExpiraEm is null ||
                cliente.CodigoRecuperacaoExpiraEm < DateTime.UtcNow ||
                !VerificarSenha(codigo, cliente.CodigoRecuperacaoHash))
            {
                return Resultado<ClienteResponse>.Falha("Código inválido ou expirado.");
            }

            cliente.SenhaHash = CriarHashSenha(request.NovaSenha);
            cliente.CodigoRecuperacaoHash = null;
            cliente.CodigoRecuperacaoExpiraEm = null;
            cliente.AtualizadoEm = DateTime.UtcNow;
            SalvarTudo();
            return Resultado<ClienteResponse>.Ok(CriarClienteResponse(cliente));
        }
    }

    public IReadOnlyList<PedidoClienteResponse> ListarPedidosDoCliente(Guid clienteId)
    {
        lock (_sync)
        {
            if (!_clientes.TryGetValue(clienteId, out var cliente))
            {
                return [];
            }

            return _pedidosOnline
                .Where(pedido =>
                    pedido.ClienteId == clienteId ||
                    string.Equals(pedido.EmailCliente, cliente.Email, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(pedido => pedido.CriadoEm)
                .Select(CriarPedidoClienteResponse)
                .ToList();
        }
    }

    public IReadOnlyList<ClientePainelResponse> ListarClientesPainel()
    {
        lock (_sync)
        {
            return _clientes.Values
                .Select(cliente =>
                {
                    var pedidos = _pedidosOnline
                        .Where(pedido =>
                            pedido.ClienteId == cliente.Id ||
                            string.Equals(pedido.EmailCliente, cliente.Email, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(pedido => pedido.CriadoEm)
                        .ToList();
                    var pedidosValidos = pedidos
                        .Where(pedido => pedido.Status != StatusPedidoOnline.Cancelado)
                        .ToList();
                    var ultimoPedido = pedidos.FirstOrDefault();

                    return new ClientePainelResponse(
                        cliente.Id,
                        cliente.Nome,
                        cliente.Email,
                        cliente.Telefone,
                        pedidos.Count,
                        pedidosValidos.Count,
                        pedidosValidos.Sum(pedido => pedido.Total),
                        ultimoPedido?.CriadoEm,
                        ultimoPedido?.Status,
                        cliente.CriadoEm,
                        cliente.AtualizadoEm);
                })
                .OrderByDescending(cliente => cliente.UltimoPedidoEm ?? cliente.AtualizadoEm)
                .ThenBy(cliente => cliente.Nome)
                .ToList();
        }
    }

    public Resultado<LojaConfiguracaoResponse> AtualizarConfiguracaoLoja(AtualizarLojaConfiguracaoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NomeCriadorSite))
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Informe quem criou o site.");
        }

        if (string.IsNullOrWhiteSpace(request.PoliticaPrivacidade))
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Informe a politica de privacidade.");
        }

        if (request.FreteValorPadrao < 0 || request.FreteGratisAcimaDe < 0)
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Os valores de frete nao podem ser negativos.");
        }

        if (request.PrazoMinimoDias < 0 || request.PrazoMaximoDias < request.PrazoMinimoDias)
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Informe um prazo de entrega valido.");
        }

        var pixOnlineAtivo = request.PixOnlineAtivo ?? true;
        var cartaoOnlineAtivo = request.CartaoOnlineAtivo ?? true;
        if (!pixOnlineAtivo && !cartaoOnlineAtivo)
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Mantenha pelo menos uma forma de pagamento ativa no site.");
        }

        var checkoutCartaoUrl = NormalizarTextoOpcional(request.CheckoutCartaoUrl) ?? "";
        if (!string.IsNullOrWhiteSpace(checkoutCartaoUrl) && !UrlValida(checkoutCartaoUrl))
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Informe um link de pagamento valido, começando com http:// ou https://.");
        }

        var emailNotificacoesAtivo = request.EmailNotificacoesAtivo ?? false;
        var emailProvedor = NormalizarProvedorEmail(request.EmailProvedor);
        var emailRemetente = NormalizarEmailOpcional(request.EmailRemetente);
        var emailPedidosDestino = NormalizarEmailOpcional(request.EmailPedidosDestino);
        var smtpHost = NormalizarTextoOpcional(request.SmtpHost) ?? "";
        var smtpPorta = request.SmtpPorta ?? 587;
        if (smtpPorta <= 0 || smtpPorta > 65535)
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Informe uma porta SMTP valida.");
        }

        var backupIntervaloHoras = request.BackupIntervaloHoras ?? 24;
        if (backupIntervaloHoras < 1 || backupIntervaloHoras > 720)
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Informe um intervalo de backup entre 1 e 720 horas.");
        }

        var gatewayWebhookUrl = NormalizarTextoOpcional(request.GatewayPagamentoWebhookUrl) ?? "";
        if (!string.IsNullOrWhiteSpace(gatewayWebhookUrl) && !UrlValida(gatewayWebhookUrl))
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Informe uma URL de webhook valida para o pagamento.");
        }

        if (emailNotificacoesAtivo &&
            (string.IsNullOrWhiteSpace(emailRemetente) ||
             string.IsNullOrWhiteSpace(emailPedidosDestino)))
        {
            return Resultado<LojaConfiguracaoResponse>.Falha("Para ativar e-mails, informe remetente e e-mail dos pedidos.");
        }

        lock (_sync)
        {
            var gatewayPagamentoProvedor = NormalizarTextoOpcional(request.GatewayPagamentoProvedor) ?? "";
            var gatewayPagamentoAtivo = request.GatewayPagamentoAtivo ?? false;
            var gatewayPagamentoAccessToken = NormalizarTextoOpcional(request.GatewayPagamentoAccessToken) ?? _configuracaoLoja.GatewayPagamentoAccessToken;
            var gatewayPagamentoWebhookSecret = NormalizarTextoOpcional(request.GatewayPagamentoWebhookSecret) ?? _configuracaoLoja.GatewayPagamentoWebhookSecret;
            var brevoApiKey = NormalizarTextoOpcional(request.BrevoApiKey) ?? _configuracaoLoja.BrevoApiKey;
            var smtpUsuario = NormalizarTextoOpcional(request.SmtpUsuario) ?? "";
            var smtpSenha = NormalizarTextoOpcional(request.SmtpSenha) ?? _configuracaoLoja.SmtpSenha;

            if (emailNotificacoesAtivo && emailProvedor == "Brevo" && string.IsNullOrWhiteSpace(brevoApiKey))
            {
                return Resultado<LojaConfiguracaoResponse>.Falha("Informe a API key da Brevo para ativar e-mails por Brevo.");
            }

            if (emailNotificacoesAtivo && emailProvedor == "Smtp" && string.IsNullOrWhiteSpace(smtpHost))
            {
                return Resultado<LojaConfiguracaoResponse>.Falha("Informe o servidor SMTP para ativar e-mails por SMTP.");
            }

            if (emailNotificacoesAtivo &&
                emailProvedor == "Smtp" &&
                !string.IsNullOrWhiteSpace(smtpUsuario) &&
                string.IsNullOrWhiteSpace(smtpSenha))
            {
                return Resultado<LojaConfiguracaoResponse>.Falha("Informe a senha SMTP. No Gmail use a senha de app, nao a senha normal da conta.");
            }

            if (gatewayPagamentoAtivo)
            {
                if (!string.Equals(gatewayPagamentoProvedor, "Asaas", StringComparison.OrdinalIgnoreCase))
                {
                    return Resultado<LojaConfiguracaoResponse>.Falha("Para ativar pagamento automatico, escolha o provedor Asaas.");
                }

                if (string.IsNullOrWhiteSpace(gatewayPagamentoAccessToken))
                {
                    return Resultado<LojaConfiguracaoResponse>.Falha("Informe o token do Asaas antes de ativar o gateway.");
                }

                if (string.IsNullOrWhiteSpace(gatewayPagamentoWebhookSecret))
                {
                    return Resultado<LojaConfiguracaoResponse>.Falha("Informe o segredo do webhook Asaas antes de ativar confirmação automática.");
                }
            }

            _configuracaoLoja.NomeCriadorSite = NormalizarTexto(request.NomeCriadorSite);
            _configuracaoLoja.PoliticaPrivacidade = NormalizarTexto(request.PoliticaPrivacidade);
            _configuracaoLoja.FreteValorPadrao = request.FreteValorPadrao;
            _configuracaoLoja.FreteGratisAcimaDe = request.FreteGratisAcimaDe;
            _configuracaoLoja.PrazoMinimoDias = request.PrazoMinimoDias;
            _configuracaoLoja.PrazoMaximoDias = request.PrazoMaximoDias;
            _configuracaoLoja.MensagemFrete = NormalizarTextoOpcional(request.MensagemFrete) ?? "";
            _configuracaoLoja.MensagemLoginCliente = NormalizarTextoOpcional(request.MensagemLoginCliente) ?? "";
            _configuracaoLoja.BannerEyebrow = NormalizarTextoOpcional(request.BannerEyebrow) ?? "Coleção pronta entrega";
            _configuracaoLoja.BannerTitulo = NormalizarTextoOpcional(request.BannerTitulo) ?? "Nana Modas";
            _configuracaoLoja.BannerDescricao = NormalizarTextoOpcional(request.BannerDescricao) ?? "Peças selecionadas, estética premium e compra online integrada ao estoque da loja física.";
            _configuracaoLoja.BannerBotaoPrimario = NormalizarTextoOpcional(request.BannerBotaoPrimario) ?? "Ver coleção";
            _configuracaoLoja.BannerBotaoSecundario = NormalizarTextoOpcional(request.BannerBotaoSecundario) ?? "Minha sacola";
            _configuracaoLoja.BannerImagemUrl = NormalizarTextoOpcional(request.BannerImagemUrl) ?? "";
            _configuracaoLoja.PromocaoTopoTexto = NormalizarTextoOpcional(request.PromocaoTopoTexto) ?? "Compra segura Nana Modas: estoque real, atendimento direto e pagamento por Pix ou cartão.";
            _configuracaoLoja.CampanhaTitulo = NormalizarTextoOpcional(request.CampanhaTitulo) ?? "Coleção premium pronta entrega";
            _configuracaoLoja.CampanhaDescricao = NormalizarTextoOpcional(request.CampanhaDescricao) ?? "";
            _configuracaoLoja.CampanhaBotaoTexto = NormalizarTextoOpcional(request.CampanhaBotaoTexto) ?? "Ver novidades";
            _configuracaoLoja.CampanhaImagemUrl = NormalizarTextoOpcional(request.CampanhaImagemUrl) ?? "";
            _configuracaoLoja.VitrineImagem1Url = NormalizarTextoOpcional(request.VitrineImagem1Url) ?? "";
            _configuracaoLoja.VitrineImagem1Titulo = NormalizarTextoOpcional(request.VitrineImagem1Titulo) ?? "Novidades";
            _configuracaoLoja.VitrineImagem2Url = NormalizarTextoOpcional(request.VitrineImagem2Url) ?? "";
            _configuracaoLoja.VitrineImagem2Titulo = NormalizarTextoOpcional(request.VitrineImagem2Titulo) ?? "Promoções";
            _configuracaoLoja.VitrineImagem3Url = NormalizarTextoOpcional(request.VitrineImagem3Url) ?? "";
            _configuracaoLoja.VitrineImagem3Titulo = NormalizarTextoOpcional(request.VitrineImagem3Titulo) ?? "Mais desejados";
            _configuracaoLoja.WhatsappLoja = NormalizarTextoOpcional(request.WhatsappLoja) ?? "";
            _configuracaoLoja.InstagramLoja = NormalizarTextoOpcional(request.InstagramLoja) ?? "";
            _configuracaoLoja.EnderecoLoja = NormalizarTextoOpcional(request.EnderecoLoja) ?? "";
            _configuracaoLoja.PixChave = NormalizarTextoOpcional(request.PixChave) ?? "";
            _configuracaoLoja.PixNomeRecebedor = NormalizarTextoOpcional(request.PixNomeRecebedor) ?? "NANA MODAS";
            _configuracaoLoja.PixCidade = NormalizarTextoOpcional(request.PixCidade)?.ToUpperInvariant() ?? "SAO PAULO";
            _configuracaoLoja.PixOnlineAtivo = pixOnlineAtivo;
            _configuracaoLoja.CartaoOnlineAtivo = cartaoOnlineAtivo;
            _configuracaoLoja.CheckoutCartaoNome = NormalizarTextoOpcional(request.CheckoutCartaoNome) ?? "Link de pagamento";
            _configuracaoLoja.CheckoutCartaoUrl = checkoutCartaoUrl;
            _configuracaoLoja.MensagemPagamento = NormalizarTextoOpcional(request.MensagemPagamento) ?? "";
            _configuracaoLoja.MensagemPagamentoCartao = NormalizarTextoOpcional(request.MensagemPagamentoCartao) ?? "";
            _configuracaoLoja.EmailNotificacoesAtivo = emailNotificacoesAtivo;
            _configuracaoLoja.EmailProvedor = emailProvedor;
            _configuracaoLoja.EmailRemetente = emailRemetente ?? "";
            _configuracaoLoja.EmailPedidosDestino = emailPedidosDestino ?? "";
            _configuracaoLoja.BrevoApiKey = brevoApiKey;
            _configuracaoLoja.SmtpHost = smtpHost;
            _configuracaoLoja.SmtpPorta = smtpPorta;
            _configuracaoLoja.SmtpUsuario = smtpUsuario;
            _configuracaoLoja.SmtpSenha = smtpSenha;
            _configuracaoLoja.SmtpSsl = request.SmtpSsl ?? true;
            _configuracaoLoja.BackupAutomaticoAtivo = request.BackupAutomaticoAtivo ?? true;
            _configuracaoLoja.BackupIntervaloHoras = backupIntervaloHoras;
            _configuracaoLoja.GatewayPagamentoProvedor = gatewayPagamentoProvedor;
            _configuracaoLoja.GatewayPagamentoAtivo = gatewayPagamentoAtivo;
            _configuracaoLoja.GatewayPagamentoProducao = request.GatewayPagamentoProducao ?? false;
            _configuracaoLoja.GatewayPagamentoPublicKey = NormalizarTextoOpcional(request.GatewayPagamentoPublicKey) ?? "";
            _configuracaoLoja.GatewayPagamentoAccessToken = gatewayPagamentoAccessToken;
            _configuracaoLoja.GatewayPagamentoWebhookSecret = gatewayPagamentoWebhookSecret;
            _configuracaoLoja.GatewayPagamentoWebhookUrl = gatewayWebhookUrl;
            _configuracaoLoja.RazaoSocial = NormalizarTextoOpcional(request.RazaoSocial) ?? "";
            _configuracaoLoja.Cnpj = NormalizarTextoOpcional(request.Cnpj) ?? "";
            _configuracaoLoja.SiteUrlCanonica = NormalizarTextoOpcional(request.SiteUrlCanonica) ?? "";
            _configuracaoLoja.PoliticaTrocaDevolucao = NormalizarTextoOpcional(request.PoliticaTrocaDevolucao) ?? _configuracaoLoja.PoliticaTrocaDevolucao;
            _configuracaoLoja.GoogleAnalyticsId = NormalizarTextoOpcional(request.GoogleAnalyticsId) ?? "";
            _configuracaoLoja.MetaPixelId = NormalizarTextoOpcional(request.MetaPixelId) ?? "";
            _configuracaoLoja.BackupEmailAtivo = request.BackupEmailAtivo ?? false;
            _configuracaoLoja.BackupEmailDestino = NormalizarEmailOpcional(request.BackupEmailDestino) ?? "";
            _configuracaoLoja.AtualizadoEm = DateTime.UtcNow;

            SalvarTudo();
            return Resultado<LojaConfiguracaoResponse>.Ok(CriarLojaConfiguracaoResponse());
        }
    }

    public ProdutoResponse? ObterProduto(Guid id)
    {
        lock (_sync)
        {
            return _produtos.TryGetValue(id, out var produto) ? CriarProdutoResponse(produto) : null;
        }
    }

    public Resultado<ProdutoResponse> CriarProduto(CriarProdutoRequest request)
    {
        var validacao = ValidarProduto(request.Nome, request.CategoriaId, request.Preco, request.Custo, request.QuantidadeInicial);
        if (validacao is not null)
        {
            return Resultado<ProdutoResponse>.Falha(validacao);
        }

        lock (_sync)
        {
            var tamanhos = NormalizarListaTexto(request.Tamanhos);
            var cores = NormalizarListaTexto(request.Cores);
            var modelos = NormalizarListaTexto(request.Modelos);
            var variacoes = NormalizarVariacoesEstoque(request.VariacoesEstoque, tamanhos, cores, modelos, out var erroVariacoes);
            if (erroVariacoes is not null)
            {
                return Resultado<ProdutoResponse>.Falha(erroVariacoes);
            }

            var produto = new Produto
            {
                Nome = NormalizarTexto(request.Nome),
                CategoriaId = request.CategoriaId,
                Sku = NormalizarTextoOpcional(request.Sku),
                Preco = request.Preco,
                Custo = Math.Max(0, request.Custo),
                QuantidadeEmEstoque = variacoes.Count > 0 ? variacoes.Sum(variacao => variacao.Quantidade) : request.QuantidadeInicial,
                Descricao = NormalizarTextoOpcional(request.Descricao),
                ImagemUrl = NormalizarTextoOpcional(request.ImagemUrl),
                ImagensExtras = NormalizarListaDeImagens(request.ImagensExtras),
                Tamanhos = tamanhos,
                Cores = cores,
                Modelos = modelos,
                VariacoesEstoque = variacoes,
                GuiaMedidas = NormalizarTextoOpcional(request.GuiaMedidas)
            };

            _produtos[produto.Id] = produto;

            if (produto.QuantidadeEmEstoque > 0)
            {
                RegistrarMovimentacao(produto, TipoMovimentacaoEstoque.Entrada, produto.QuantidadeEmEstoque, "Cadastro do produto", null);
            }

            SalvarTudo();
            return Resultado<ProdutoResponse>.Ok(CriarProdutoResponse(produto));
        }
    }

    public Resultado<ProdutoResponse> AtualizarProduto(Guid id, AtualizarProdutoRequest request)
    {
        var validacao = ValidarProduto(request.Nome, request.CategoriaId, request.Preco, request.Custo, 0);
        if (validacao is not null)
        {
            return Resultado<ProdutoResponse>.Falha(validacao);
        }

        lock (_sync)
        {
            if (!_produtos.TryGetValue(id, out var produto))
            {
                return Resultado<ProdutoResponse>.Falha("Produto nao encontrado.");
            }

            produto.Nome = NormalizarTexto(request.Nome);
            produto.CategoriaId = request.CategoriaId;
            produto.Sku = NormalizarTextoOpcional(request.Sku);
            produto.Preco = request.Preco;
            produto.Custo = Math.Max(0, request.Custo);
            produto.Ativo = request.Ativo;
            produto.Descricao = NormalizarTextoOpcional(request.Descricao);
            produto.ImagemUrl = NormalizarTextoOpcional(request.ImagemUrl);
            produto.ImagensExtras = NormalizarListaDeImagens(request.ImagensExtras);
            produto.Tamanhos = NormalizarListaTexto(request.Tamanhos);
            produto.Cores = NormalizarListaTexto(request.Cores);
            produto.Modelos = NormalizarListaTexto(request.Modelos);
            var variacoes = NormalizarVariacoesEstoque(request.VariacoesEstoque, produto.Tamanhos, produto.Cores, produto.Modelos, out var erroVariacoes);
            if (erroVariacoes is not null)
            {
                return Resultado<ProdutoResponse>.Falha(erroVariacoes);
            }

            produto.VariacoesEstoque = variacoes;
            if (produto.VariacoesEstoque.Count > 0)
            {
                produto.QuantidadeEmEstoque = produto.VariacoesEstoque.Sum(variacao => variacao.Quantidade);
            }
            else
            {
                var quantidadeAnterior = produto.QuantidadeEmEstoque;
                var quantidadeNova = Math.Max(0, request.QuantidadeEmEstoque);
                if (quantidadeNova != quantidadeAnterior)
                {
                    produto.QuantidadeEmEstoque = quantidadeNova;
                    RegistrarMovimentacao(
                        produto,
                        TipoMovimentacaoEstoque.Ajuste,
                        quantidadeNova - quantidadeAnterior,
                        "Ajuste manual no cadastro do produto",
                        null);
                }
            }
            produto.GuiaMedidas = NormalizarTextoOpcional(request.GuiaMedidas);
            produto.AtualizadoEm = DateTime.UtcNow;

            SalvarTudo();
            return Resultado<ProdutoResponse>.Ok(CriarProdutoResponse(produto));
        }
    }

    public Resultado<string> ExcluirProduto(Guid id)
    {
        lock (_sync)
        {
            if (!_produtos.TryGetValue(id, out var produto))
            {
                return Resultado<string>.Falha("Produto nao encontrado.");
            }

            _produtos.Remove(id);
            SalvarTudo();
            return Resultado<string>.Ok(produto.Nome);
        }
    }

    public Resultado<ProdutoResponse> AtualizarVitrineProduto(Guid id, AtualizarVitrineProdutoRequest request)
    {
        if (request.PrecoLoja is <= 0)
        {
            return Resultado<ProdutoResponse>.Falha("O preco do site deve ser maior que zero ou ficar em branco.");
        }

        lock (_sync)
        {
            if (!_produtos.TryGetValue(id, out var produto))
            {
                return Resultado<ProdutoResponse>.Falha("Produto nao encontrado.");
            }

            produto.PublicadoNaLoja = request.PublicadoNaLoja;
            produto.DestaqueLoja = request.DestaqueLoja;
            produto.OrdemLoja = Math.Max(0, request.OrdemLoja);
            produto.NomeLoja = NormalizarTextoOpcional(request.NomeLoja);
            produto.DescricaoLoja = NormalizarTextoOpcional(request.DescricaoLoja);
            produto.PrecoLoja = request.PrecoLoja;
            produto.ImagemLojaUrl = NormalizarTextoOpcional(request.ImagemLojaUrl);
            produto.ImagensLojaExtras = NormalizarListaDeImagens(request.ImagensLojaExtras);
            produto.AtualizadoEm = DateTime.UtcNow;

            SalvarTudo();
            return Resultado<ProdutoResponse>.Ok(CriarProdutoResponse(produto));
        }
    }

    public Resultado<ProdutoResponse> RegistrarEntradaEstoque(Guid produtoId, EntradaEstoqueRequest request)
    {
        if (request.Quantidade <= 0)
        {
            return Resultado<ProdutoResponse>.Falha("A quantidade de entrada deve ser maior que zero.");
        }

        if (request.CustoUnitario is < 0)
        {
            return Resultado<ProdutoResponse>.Falha("O custo unitario nao pode ser negativo.");
        }

        lock (_sync)
        {
            if (!_produtos.TryGetValue(produtoId, out var produto))
            {
                return Resultado<ProdutoResponse>.Falha("Produto nao encontrado.");
            }

            var tamanho = NormalizarTextoOpcional(request.Tamanho);
            var cor = NormalizarTextoOpcional(request.Cor);
            var modelo = NormalizarTextoOpcional(request.Modelo);
            var temVariacaoInformada = !string.IsNullOrWhiteSpace(tamanho) ||
                !string.IsNullOrWhiteSpace(cor) ||
                !string.IsNullOrWhiteSpace(modelo);
            if (produto.VariacoesEstoque.Count > 0 && !temVariacaoInformada)
            {
                return Resultado<ProdutoResponse>.Falha("Escolha a variacao para registrar entrada desse produto.");
            }

            var validacaoVariacao = ValidarVariacaoEntrada(produto, tamanho, cor, modelo);
            if (validacaoVariacao is not null)
            {
                return Resultado<ProdutoResponse>.Falha(validacaoVariacao);
            }

            Fornecedor? fornecedor = null;
            if (request.FornecedorId.HasValue)
            {
                if (!_fornecedores.TryGetValue(request.FornecedorId.Value, out fornecedor))
                {
                    return Resultado<ProdutoResponse>.Falha("Fornecedor nao encontrado.");
                }

                if (!fornecedor.Ativo)
                {
                    return Resultado<ProdutoResponse>.Falha("Fornecedor inativo.");
                }
            }

            var custoUnitario = request.CustoUnitario;
            if (custoUnitario is > 0)
            {
                produto.Custo = custoUnitario.Value;
            }

            AdicionarEstoqueProduto(produto, tamanho, cor, modelo, request.Quantidade);
            produto.AtualizadoEm = DateTime.UtcNow;

            var detalhesOrigem = new List<string>();
            var observacao = NormalizarTextoOpcional(request.Observacao);
            var documento = NormalizarTextoOpcional(request.Documento);
            if (observacao is not null)
            {
                detalhesOrigem.Add(observacao);
            }

            if (fornecedor is not null)
            {
                detalhesOrigem.Add($"Fornecedor: {fornecedor.Nome}");
            }

            if (documento is not null)
            {
                detalhesOrigem.Add($"Doc: {documento}");
            }

            var variacaoTexto = CriarTextoVariacao(tamanho, cor, modelo);
            if (variacaoTexto is not null)
            {
                detalhesOrigem.Add($"Variacao: {variacaoTexto}");
            }

            var origem = detalhesOrigem.Count == 0
                ? "Entrada manual de estoque"
                : $"Entrada manual: {string.Join(" | ", detalhesOrigem)}";

            RegistrarMovimentacao(
                produto,
                TipoMovimentacaoEstoque.Entrada,
                request.Quantidade,
                origem,
                null,
                null,
                fornecedor?.Id,
                fornecedor?.Nome,
                custoUnitario,
                documento);
            SalvarTudo();
            return Resultado<ProdutoResponse>.Ok(CriarProdutoResponse(produto));
        }
    }

    public IReadOnlyList<EstoqueMovimentacao> ListarMovimentacoesEstoque()
    {
        lock (_sync)
        {
            return _movimentacoes
                .OrderByDescending(movimentacao => movimentacao.CriadaEm)
                .ToList();
        }
    }

    public Resultado<VendaLoja> RegistrarVendaLoja(RegistrarVendaLojaRequest request)
    {
        if (request.Itens.Count == 0)
        {
            return Resultado<VendaLoja>.Falha("Informe pelo menos um item para vender.");
        }

        if (request.Itens.Any(item => item.Quantidade <= 0))
        {
            return Resultado<VendaLoja>.Falha("Todos os itens precisam ter quantidade maior que zero.");
        }

        if (request.Desconto < 0)
        {
            return Resultado<VendaLoja>.Falha("O desconto nao pode ser negativo.");
        }

        lock (_sync)
        {
            var itensAgrupados = request.Itens
                .Select(item => new ItemVendaLojaRequest(
                    item.ProdutoId,
                    item.Quantidade,
                    NormalizarTextoOpcional(item.Tamanho),
                    NormalizarTextoOpcional(item.Cor),
                    NormalizarTextoOpcional(item.Modelo)))
                .GroupBy(item => new { item.ProdutoId, item.Tamanho, item.Cor, item.Modelo })
                .Select(grupo => new ItemVendaLojaRequest(
                    grupo.Key.ProdutoId,
                    grupo.Sum(item => item.Quantidade),
                    grupo.Key.Tamanho,
                    grupo.Key.Cor,
                    grupo.Key.Modelo))
                .ToList();

            foreach (var item in itensAgrupados)
            {
                if (!_produtos.TryGetValue(item.ProdutoId, out var produto))
                {
                    return Resultado<VendaLoja>.Falha($"Produto {item.ProdutoId} nao encontrado.");
                }

                if (!produto.Ativo)
                {
                    return Resultado<VendaLoja>.Falha($"Produto {produto.Nome} esta inativo.");
                }

                var validacaoVariacao = ValidarVariacaoVenda(produto, item);
                if (validacaoVariacao is not null)
                {
                    return Resultado<VendaLoja>.Falha(validacaoVariacao);
                }

                var disponivel = ObterEstoqueDisponivel(produto, item.Tamanho, item.Cor, item.Modelo);
                if (disponivel < item.Quantidade)
                {
                    return Resultado<VendaLoja>.Falha($"Estoque insuficiente para {produto.Nome}. Disponivel: {disponivel}.");
                }
            }

            var itensVenda = itensAgrupados.Select(item =>
            {
                var produto = _produtos[item.ProdutoId];
                return new ItemVendaLoja
                {
                    ProdutoId = produto.Id,
                    ProdutoNome = produto.Nome,
                    Tamanho = item.Tamanho,
                    Cor = item.Cor,
                    Modelo = item.Modelo,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = produto.Preco,
                    CustoUnitario = produto.Custo
                };
            }).ToList();

            var totalBruto = itensVenda.Sum(item => item.Subtotal);
            if (request.Desconto > totalBruto)
            {
                return Resultado<VendaLoja>.Falha("O desconto nao pode ser maior que o total da venda.");
            }

            var desconto = request.Desconto;
            var totalLiquido = totalBruto - desconto;
            var valorRecebido = request.ValorRecebido > 0 ? request.ValorRecebido : totalLiquido;
            if (request.FormaPagamento == FormaPagamento.Dinheiro && valorRecebido < totalLiquido)
            {
                return Resultado<VendaLoja>.Falha("O valor recebido nao pode ser menor que o total em dinheiro.");
            }

            var venda = new VendaLoja
            {
                FormaPagamento = request.FormaPagamento,
                Itens = itensVenda,
                Desconto = desconto,
                ValorRecebido = valorRecebido,
                Observacao = NormalizarTextoOpcional(request.Observacao)
            };

            foreach (var item in itensAgrupados)
            {
                var produto = _produtos[item.ProdutoId];
                BaixarEstoqueProduto(produto, item.Tamanho, item.Cor, item.Modelo, item.Quantidade);
                produto.AtualizadoEm = DateTime.UtcNow;
                RegistrarMovimentacao(produto, TipoMovimentacaoEstoque.SaidaVendaLoja, item.Quantidade, "Venda presencial", venda.Id);
            }

            _vendasLoja.Add(venda);
            SalvarTudo();
            return Resultado<VendaLoja>.Ok(venda);
        }
    }

    public IReadOnlyList<VendaLoja> ListarVendasLoja()
    {
        lock (_sync)
        {
            return _vendasLoja
                .OrderByDescending(venda => venda.CriadaEm)
                .ToList();
        }
    }

    public Resultado<VendaLoja> DevolverVendaLoja(Guid id, DevolverVendaLojaRequest request)
    {
        lock (_sync)
        {
            var venda = _vendasLoja.FirstOrDefault(item => item.Id == id);
            if (venda is null)
            {
                return Resultado<VendaLoja>.Falha("Venda nao encontrada.");
            }

            if (venda.Devolvida)
            {
                return Resultado<VendaLoja>.Falha("Essa venda ja foi devolvida.");
            }

            var resultadoDevolucao = AplicarDevolucaoVenda(venda, request.Itens, request.Motivo, "Devolucao venda PDV");
            if (!resultadoDevolucao.Sucesso)
            {
                return Resultado<VendaLoja>.Falha(resultadoDevolucao.Erro ?? "Nao foi possivel registrar a devolucao.");
            }

            SalvarTudo();
            return Resultado<VendaLoja>.Ok(venda);
        }
    }

    public Resultado<TrocaVendaLojaResponse> TrocarVendaLoja(Guid id, TrocarVendaLojaRequest request)
    {
        if (request.ItensNovos is null || request.ItensNovos.Count == 0)
        {
            return Resultado<TrocaVendaLojaResponse>.Falha("Informe pelo menos um item novo para a troca.");
        }

        if (request.ItensNovos.Any(item => item.Quantidade <= 0))
        {
            return Resultado<TrocaVendaLojaResponse>.Falha("Todos os itens novos precisam ter quantidade maior que zero.");
        }

        lock (_sync)
        {
            var vendaOriginal = _vendasLoja.FirstOrDefault(item => item.Id == id);
            if (vendaOriginal is null)
            {
                return Resultado<TrocaVendaLojaResponse>.Falha("Venda nao encontrada.");
            }

            if (vendaOriginal.Devolvida)
            {
                return Resultado<TrocaVendaLojaResponse>.Falha("Essa venda ja foi devolvida.");
            }

            var itensParaDevolverResultado = PrepararItensDevolucao(vendaOriginal, request.ItensDevolvidos);
            if (!itensParaDevolverResultado.Sucesso)
            {
                return Resultado<TrocaVendaLojaResponse>.Falha(itensParaDevolverResultado.Erro ?? "Itens de devolucao invalidos.");
            }

            var itensParaDevolver = itensParaDevolverResultado.Valor!;
            var itensNovos = request.ItensNovos
                .Select(item => new ItemVendaLojaRequest(
                    item.ProdutoId,
                    item.Quantidade,
                    NormalizarTextoOpcional(item.Tamanho),
                    NormalizarTextoOpcional(item.Cor),
                    NormalizarTextoOpcional(item.Modelo)))
                .GroupBy(item => new { item.ProdutoId, item.Tamanho, item.Cor, item.Modelo })
                .Select(grupo => new ItemVendaLojaRequest(
                    grupo.Key.ProdutoId,
                    grupo.Sum(item => item.Quantidade),
                    grupo.Key.Tamanho,
                    grupo.Key.Cor,
                    grupo.Key.Modelo))
                .ToList();

            var quantidadeDevolvidaPorProduto = itensParaDevolver
                .GroupBy(item => CriarChaveItemVenda(item.Item.ProdutoId, item.Item.Tamanho, item.Item.Cor, item.Item.Modelo))
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(item => item.Quantidade));

            foreach (var item in itensNovos)
            {
                if (!_produtos.TryGetValue(item.ProdutoId, out var produto))
                {
                    return Resultado<TrocaVendaLojaResponse>.Falha($"Produto {item.ProdutoId} nao encontrado.");
                }

                if (!produto.Ativo)
                {
                    return Resultado<TrocaVendaLojaResponse>.Falha($"Produto {produto.Nome} esta inativo.");
                }

                var validacaoVariacao = ValidarVariacaoVenda(produto, item);
                if (validacaoVariacao is not null)
                {
                    return Resultado<TrocaVendaLojaResponse>.Falha(validacaoVariacao);
                }

                var chaveItem = CriarChaveItemVenda(item.ProdutoId, item.Tamanho, item.Cor, item.Modelo);
                var quantidadeDisponivel = ObterEstoqueDisponivel(produto, item.Tamanho, item.Cor, item.Modelo) +
                    quantidadeDevolvidaPorProduto.GetValueOrDefault(chaveItem);
                if (quantidadeDisponivel < item.Quantidade)
                {
                    return Resultado<TrocaVendaLojaResponse>.Falha($"Estoque insuficiente para {produto.Nome}. Disponivel apos devolucao: {quantidadeDisponivel}.");
                }
            }

            var resultadoDevolucao = AplicarDevolucaoVenda(vendaOriginal, request.ItensDevolvidos, request.Motivo, "Troca PDV - devolucao");
            if (!resultadoDevolucao.Sucesso)
            {
                return Resultado<TrocaVendaLojaResponse>.Falha(resultadoDevolucao.Erro ?? "Nao foi possivel devolver os itens da troca.");
            }

            var itensVendaTroca = itensNovos.Select(item =>
            {
                var produto = _produtos[item.ProdutoId];
                return new ItemVendaLoja
                {
                    ProdutoId = produto.Id,
                    ProdutoNome = produto.Nome,
                    Tamanho = item.Tamanho,
                    Cor = item.Cor,
                    Modelo = item.Modelo,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = produto.Preco,
                    CustoUnitario = produto.Custo
                };
            }).ToList();

            var totalTroca = itensVendaTroca.Sum(item => item.Subtotal);
            var observacoes = new[]
            {
                $"Troca da venda {vendaOriginal.Id.ToString()[..8].ToUpperInvariant()}",
                NormalizarTextoOpcional(request.Observacao)
            }.Where(item => !string.IsNullOrWhiteSpace(item));

            var vendaTroca = new VendaLoja
            {
                FormaPagamento = request.FormaPagamento,
                Itens = itensVendaTroca,
                Desconto = 0,
                ValorRecebido = request.ValorRecebido > 0 ? request.ValorRecebido : totalTroca,
                Observacao = string.Join(" | ", observacoes)
            };

            foreach (var item in itensNovos)
            {
                var produto = _produtos[item.ProdutoId];
                BaixarEstoqueProduto(produto, item.Tamanho, item.Cor, item.Modelo, item.Quantidade);
                produto.AtualizadoEm = DateTime.UtcNow;
                RegistrarMovimentacao(produto, TipoMovimentacaoEstoque.SaidaVendaLoja, item.Quantidade, "Troca PDV - nova peça", vendaTroca.Id);
            }

            _vendasLoja.Add(vendaTroca);
            SalvarTudo();
            return Resultado<TrocaVendaLojaResponse>.Ok(new TrocaVendaLojaResponse(vendaOriginal, vendaTroca));
        }
    }

    private Resultado<List<(ItemVendaLoja Item, int Quantidade)>> PrepararItensDevolucao(
        VendaLoja venda,
        IReadOnlyList<ItemDevolucaoVendaLojaRequest>? itens)
    {
        var itensParaDevolver = new List<(ItemVendaLoja Item, int Quantidade)>();

        if (itens is null || itens.Count == 0)
        {
            itensParaDevolver = venda.Itens
                .Where(item => item.QuantidadeLiquida > 0)
                .Select(item => (item, item.QuantidadeLiquida))
                .ToList();
        }
        else
        {
            foreach (var itemRequest in itens
                .Where(item => item.Quantidade > 0)
                .Select(item => new ItemDevolucaoVendaLojaRequest(
                    item.ProdutoId,
                    item.Quantidade,
                    NormalizarTextoOpcional(item.Tamanho),
                    NormalizarTextoOpcional(item.Cor),
                    NormalizarTextoOpcional(item.Modelo)))
                .GroupBy(item => new { item.ProdutoId, item.Tamanho, item.Cor, item.Modelo })
                .Select(grupo => new ItemDevolucaoVendaLojaRequest(
                    grupo.Key.ProdutoId,
                    grupo.Sum(item => item.Quantidade),
                    grupo.Key.Tamanho,
                    grupo.Key.Cor,
                    grupo.Key.Modelo)))
            {
                var itemVenda = venda.Itens.FirstOrDefault(item =>
                    item.ProdutoId == itemRequest.ProdutoId &&
                    VariacaoIgual(item.Tamanho, itemRequest.Tamanho) &&
                    VariacaoIgual(item.Cor, itemRequest.Cor) &&
                    VariacaoIgual(item.Modelo, itemRequest.Modelo));
                if (itemVenda is null)
                {
                    return Resultado<List<(ItemVendaLoja Item, int Quantidade)>>.Falha("Produto informado nao pertence a essa venda.");
                }

                if (itemRequest.Quantidade > itemVenda.QuantidadeLiquida)
                {
                    return Resultado<List<(ItemVendaLoja Item, int Quantidade)>>.Falha($"A quantidade devolvida de {itemVenda.ProdutoNome} nao pode passar de {itemVenda.QuantidadeLiquida}.");
                }

                itensParaDevolver.Add((itemVenda, itemRequest.Quantidade));
            }
        }

        if (itensParaDevolver.Count == 0)
        {
            return Resultado<List<(ItemVendaLoja Item, int Quantidade)>>.Falha("Informe pelo menos um item para devolucao.");
        }

        foreach (var (item, _) in itensParaDevolver)
        {
            if (!_produtos.ContainsKey(item.ProdutoId))
            {
                return Resultado<List<(ItemVendaLoja Item, int Quantidade)>>.Falha($"Produto {item.ProdutoNome} nao encontrado para devolver ao estoque.");
            }
        }

        return Resultado<List<(ItemVendaLoja Item, int Quantidade)>>.Ok(itensParaDevolver);
    }

    private Resultado<VendaLoja> AplicarDevolucaoVenda(
        VendaLoja venda,
        IReadOnlyList<ItemDevolucaoVendaLojaRequest>? itens,
        string? motivo,
        string origem)
    {
        var itensParaDevolverResultado = PrepararItensDevolucao(venda, itens);
        if (!itensParaDevolverResultado.Sucesso)
        {
            return Resultado<VendaLoja>.Falha(itensParaDevolverResultado.Erro ?? "Itens de devolucao invalidos.");
        }

        foreach (var (item, quantidade) in itensParaDevolverResultado.Valor!)
        {
            var produto = _produtos[item.ProdutoId];
            DevolverEstoqueProduto(produto, item.Tamanho, item.Cor, item.Modelo, quantidade);
            produto.AtualizadoEm = DateTime.UtcNow;
            item.QuantidadeDevolvida += quantidade;

            RegistrarMovimentacao(
                produto,
                TipoMovimentacaoEstoque.Entrada,
                quantidade,
                origem,
                venda.Id);
        }

        venda.Devolvida = venda.Itens.All(item => item.QuantidadeLiquida == 0);
        venda.DevolvidaEm = DateTime.UtcNow;
        venda.MotivoDevolucao = NormalizarTextoOpcional(motivo);
        return Resultado<VendaLoja>.Ok(venda);
    }

    public Resultado<PedidoOnline> RegistrarPedidoOnline(RegistrarPedidoOnlineRequest request, Guid? clienteId = null)
    {
        var validacaoCliente = ValidarClientePedido(request);
        if (validacaoCliente is not null)
        {
            return Resultado<PedidoOnline>.Falha(validacaoCliente);
        }

        if (request.Itens.Count == 0)
        {
            return Resultado<PedidoOnline>.Falha("Informe pelo menos um item para comprar.");
        }

        if (request.Itens.Any(item => item.Quantidade <= 0))
        {
            return Resultado<PedidoOnline>.Falha("Todos os itens precisam ter quantidade maior que zero.");
        }

        lock (_sync)
        {
            var itensAgrupados = request.Itens
                .Select(item => new ItemPedidoOnlineRequest(
                    item.ProdutoId,
                    item.Quantidade,
                    NormalizarTextoOpcional(item.Tamanho),
                    NormalizarTextoOpcional(item.Cor),
                    NormalizarTextoOpcional(item.Modelo)))
                .GroupBy(item => new { item.ProdutoId, item.Tamanho, item.Cor, item.Modelo })
                .Select(grupo => new ItemPedidoOnlineRequest(
                    grupo.Key.ProdutoId,
                    grupo.Sum(item => item.Quantidade),
                    grupo.Key.Tamanho,
                    grupo.Key.Cor,
                    grupo.Key.Modelo))
                .ToList();

            foreach (var item in itensAgrupados)
            {
                if (!_produtos.TryGetValue(item.ProdutoId, out var produto))
                {
                    return Resultado<PedidoOnline>.Falha($"Produto {item.ProdutoId} nao encontrado.");
                }

                if (!produto.Ativo)
                {
                    return Resultado<PedidoOnline>.Falha($"Produto {produto.Nome} esta indisponivel.");
                }

                if (!produto.PublicadoNaLoja)
                {
                    return Resultado<PedidoOnline>.Falha($"Produto {produto.Nome} nao esta publicado no site.");
                }

                var validacaoVariacao = ValidarVariacaoPedido(produto, item);
                if (validacaoVariacao is not null)
                {
                    return Resultado<PedidoOnline>.Falha(validacaoVariacao);
                }
            }

            foreach (var item in itensAgrupados)
            {
                var produto = _produtos[item.ProdutoId];
                var disponivel = ObterEstoqueDisponivel(produto, item.Tamanho, item.Cor, item.Modelo);
                if (disponivel < item.Quantidade)
                {
                    return Resultado<PedidoOnline>.Falha($"Estoque insuficiente para {produto.Nome}. Disponivel: {disponivel}.");
                }
            }

            var itensPedido = itensAgrupados.Select(item =>
            {
                var produto = _produtos[item.ProdutoId];
                return new ItemPedidoOnline
                {
                    ProdutoId = produto.Id,
                    ProdutoNome = ObterNomeProdutoLoja(produto),
                    Tamanho = item.Tamanho,
                    Cor = item.Cor,
                    Modelo = item.Modelo,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = ObterPrecoProdutoLoja(produto),
                    CustoUnitario = produto.Custo
                };
            }).ToList();

            var subtotalPedido = itensPedido.Sum(item => item.Subtotal);
            var resultadoCupom = CalcularCupomPedido(request.CupomCodigo, subtotalPedido);
            if (!resultadoCupom.Sucesso)
            {
                return Resultado<PedidoOnline>.Falha(resultadoCupom.Erro ?? "Cupom invalido.");
            }

            var (cupomCodigo, desconto) = resultadoCupom.Valor;
            var resultadoEntrega = CalcularEntregaPedido(request, Math.Max(0, subtotalPedido - desconto));
            if (!resultadoEntrega.Sucesso)
            {
                return Resultado<PedidoOnline>.Falha(resultadoEntrega.Erro ?? "Opcao de entrega invalida.");
            }

            var entrega = resultadoEntrega.Valor!;

            var exigeEndereco = !string.Equals(entrega.Tipo, "Retirada", StringComparison.OrdinalIgnoreCase);
            if (exigeEndereco && !EnderecoPedidoInformado(request))
            {
                return Resultado<PedidoOnline>.Falha("Informe o endereco de entrega.");
            }

            var pedido = new PedidoOnline
            {
                NomeCliente = NormalizarTexto(request.NomeCliente),
                EmailCliente = NormalizarTexto(request.EmailCliente),
                TelefoneCliente = NormalizarTextoOpcional(request.TelefoneCliente),
                DocumentoCliente = NormalizarDocumento(request.DocumentoCliente),
                ClienteId = clienteId.HasValue && _clientes.ContainsKey(clienteId.Value) ? clienteId : null,
                EnderecoEntrega = CriarEnderecoEntrega(request),
                CepEntrega = NormalizarTextoOpcional(request.CepEntrega),
                RuaEntrega = NormalizarTextoOpcional(request.RuaEntrega),
                NumeroEntrega = NormalizarTextoOpcional(request.NumeroEntrega),
                ComplementoEntrega = NormalizarTextoOpcional(request.ComplementoEntrega),
                BairroEntrega = NormalizarTextoOpcional(request.BairroEntrega),
                CidadeEntrega = NormalizarTextoOpcional(request.CidadeEntrega),
                EstadoEntrega = NormalizarTextoOpcional(request.EstadoEntrega)?.ToUpperInvariant(),
                Observacao = NormalizarTextoOpcional(request.Observacao),
                CupomCodigo = cupomCodigo,
                Desconto = desconto,
                OpcaoEntregaId = entrega.OpcaoEntregaId,
                EntregaNome = entrega.Nome,
                EntregaValor = entrega.Valor,
                EntregaPrazoMinimoDias = entrega.PrazoMinimoDias,
                EntregaPrazoMaximoDias = entrega.PrazoMaximoDias,
                FormaPagamento = request.FormaPagamento,
                Status = StatusPedidoOnline.Recebido,
                Itens = itensPedido
            };

            foreach (var item in itensAgrupados)
            {
                var produto = _produtos[item.ProdutoId];
                BaixarEstoqueProduto(produto, item.Tamanho, item.Cor, item.Modelo, item.Quantidade);
                produto.AtualizadoEm = DateTime.UtcNow;
                RegistrarMovimentacao(produto, TipoMovimentacaoEstoque.SaidaPedidoOnline, item.Quantidade, "Pedido online", null, pedido.Id);
            }

            _pedidosOnline.Add(pedido);
            SalvarTudo();
            TentarEnviarEmailPedido(pedido, "Novo pedido online Nana Modas", CriarResumoPedidoEmail(pedido));
            return Resultado<PedidoOnline>.Ok(pedido);
        }
    }

    public GatewayPagamentoConfiguracao ObterGatewayPagamentoConfiguracao()
    {
        lock (_sync)
        {
            return new GatewayPagamentoConfiguracao(
                _configuracaoLoja.GatewayPagamentoProvedor,
                _configuracaoLoja.GatewayPagamentoAtivo && GatewayPagamentoMinimoConfigurado(),
                _configuracaoLoja.GatewayPagamentoProducao,
                _configuracaoLoja.GatewayPagamentoAccessToken,
                _configuracaoLoja.GatewayPagamentoWebhookSecret);
        }
    }

    private bool GatewayPagamentoMinimoConfigurado()
    {
        return string.Equals(_configuracaoLoja.GatewayPagamentoProvedor, "Asaas", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(_configuracaoLoja.GatewayPagamentoAccessToken) &&
            !string.IsNullOrWhiteSpace(_configuracaoLoja.GatewayPagamentoWebhookSecret);
    }

    public Resultado<PedidoOnline> RegistrarPixGatewayPedido(Guid id, PagamentoPixGateway pagamento)
    {
        lock (_sync)
        {
            var pedido = _pedidosOnline.FirstOrDefault(item => item.Id == id);
            if (pedido is null)
            {
                return Resultado<PedidoOnline>.Falha("Pedido online nao encontrado.");
            }

            pedido.GatewayPagamentoProvedor = pagamento.Provedor;
            pedido.GatewayPagamentoId = pagamento.PagamentoId;
            pedido.GatewayPagamentoStatus = pagamento.Status;
            pedido.PixCopiaECola = pagamento.PixCopiaECola;
            pedido.PixQrCodeBase64 = pagamento.PixQrCodeBase64;
            pedido.PixExpiraEm = pagamento.PixExpiraEm;
            pedido.UrlPagamento = pagamento.UrlPagamento;
            pedido.ReferenciaPagamento = pagamento.PagamentoId;
            pedido.ObservacaoPagamento = "Pix dinamico gerado automaticamente.";
            pedido.PagamentoAtualizadoEm = DateTime.UtcNow;

            SalvarTudo();
            return Resultado<PedidoOnline>.Ok(pedido);
        }
    }

    public Resultado<PedidoOnline> ProcessarWebhookPagamentoAsaas(string? evento, string? pagamentoId, string? referenciaExterna, string? status)
    {
        lock (_sync)
        {
            var pedido = _pedidosOnline.FirstOrDefault(item =>
                (!string.IsNullOrWhiteSpace(pagamentoId) && string.Equals(item.GatewayPagamentoId, pagamentoId, StringComparison.OrdinalIgnoreCase)) ||
                (Guid.TryParse(referenciaExterna, out var pedidoId) && item.Id == pedidoId));

            if (pedido is null)
            {
                return Resultado<PedidoOnline>.Falha("Pedido online nao encontrado para o webhook.");
            }

            pedido.GatewayPagamentoProvedor = "Asaas";
            pedido.GatewayPagamentoId ??= pagamentoId;
            pedido.GatewayPagamentoStatus = NormalizarTextoOpcional(status) ?? NormalizarTextoOpcional(evento) ?? pedido.GatewayPagamentoStatus;
            pedido.PagamentoAtualizadoEm = DateTime.UtcNow;

            if (WebhookPagamentoConfirmado(evento, status))
            {
                if (pedido.Status == StatusPedidoOnline.Cancelado)
                {
                    return Resultado<PedidoOnline>.Falha("Pagamento recebido para pedido cancelado.");
                }

                pedido.Status = StatusPedidoOnline.Pago;
                pedido.PagamentoConfirmadoEm ??= DateTime.UtcNow;
                pedido.ObservacaoPagamento = "Pagamento confirmado automaticamente pelo Asaas.";
            }

            SalvarTudo();
            if (pedido.PagamentoConfirmadoEm is not null)
            {
                TentarEnviarEmailPedido(
                    pedido,
                    "Pagamento confirmado - Nana Modas",
                    CriarResumoPedidoEmail(pedido));
            }

            return Resultado<PedidoOnline>.Ok(pedido);
        }
    }

    public bool WebhookGatewayAutorizado(string? tokenRecebido)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(_configuracaoLoja.GatewayPagamentoWebhookSecret) ||
                string.IsNullOrWhiteSpace(tokenRecebido))
            {
                return false;
            }

            var esperado = Encoding.UTF8.GetBytes(_configuracaoLoja.GatewayPagamentoWebhookSecret);
            var recebido = Encoding.UTF8.GetBytes(tokenRecebido);
            return esperado.Length == recebido.Length &&
                CryptographicOperations.FixedTimeEquals(esperado, recebido);
        }
    }

    public IReadOnlyList<PedidoOnline> ListarPedidosOnline()
    {
        lock (_sync)
        {
            return _pedidosOnline
                .OrderByDescending(pedido => pedido.CriadoEm)
                .ToList();
        }
    }

    public Resultado<PedidoOnline> AtualizarStatusPedidoOnline(Guid id, AtualizarStatusPedidoOnlineRequest request)
    {
        lock (_sync)
        {
            var pedido = _pedidosOnline.FirstOrDefault(item => item.Id == id);
            if (pedido is null)
            {
                return Resultado<PedidoOnline>.Falha("Pedido online nao encontrado.");
            }

            var statusAnterior = pedido.Status;
            if (statusAnterior == request.Status)
            {
                return Resultado<PedidoOnline>.Ok(pedido);
            }

            if (request.Status == StatusPedidoOnline.Cancelado)
            {
                var validacaoCancelamento = ValidarProdutosDoPedido(pedido);
                if (validacaoCancelamento is not null)
                {
                    return Resultado<PedidoOnline>.Falha(validacaoCancelamento);
                }

                DevolverEstoquePedidoCancelado(pedido);
            }

            if (statusAnterior == StatusPedidoOnline.Cancelado && request.Status != StatusPedidoOnline.Cancelado)
            {
                var validacaoReativacao = ValidarEstoqueParaReativarPedido(pedido);
                if (validacaoReativacao is not null)
                {
                    return Resultado<PedidoOnline>.Falha(validacaoReativacao);
                }

                BaixarEstoquePedidoReativado(pedido);
            }

            pedido.Status = request.Status;
            if (request.Status == StatusPedidoOnline.Pago && pedido.PagamentoConfirmadoEm is null)
            {
                pedido.PagamentoConfirmadoEm = DateTime.UtcNow;
                pedido.PagamentoAtualizadoEm = pedido.PagamentoConfirmadoEm;
            }

            SalvarTudo();
            TentarEnviarEmailPedido(
                pedido,
                $"Pedido Nana Modas atualizado: {pedido.Status}",
                CriarResumoPedidoEmail(pedido));
            return Resultado<PedidoOnline>.Ok(pedido);
        }
    }

    public Resultado<PedidoOnline> AtualizarPagamentoPedidoOnline(Guid id, AtualizarPagamentoPedidoOnlineRequest request)
    {
        lock (_sync)
        {
            var pedido = _pedidosOnline.FirstOrDefault(item => item.Id == id);
            if (pedido is null)
            {
                return Resultado<PedidoOnline>.Falha("Pedido online nao encontrado.");
            }

            pedido.ReferenciaPagamento = NormalizarTextoOpcional(request.ReferenciaPagamento);
            pedido.ObservacaoPagamento = NormalizarTextoOpcional(request.ObservacaoPagamento);
            pedido.PagamentoAtualizadoEm = DateTime.UtcNow;

            if (request.ConfirmarPagamento)
            {
                if (pedido.Status == StatusPedidoOnline.Cancelado)
                {
                    return Resultado<PedidoOnline>.Falha("Nao e possivel confirmar pagamento de pedido cancelado.");
                }

                pedido.Status = StatusPedidoOnline.Pago;
                pedido.PagamentoConfirmadoEm ??= DateTime.UtcNow;
            }

            SalvarTudo();
            if (request.ConfirmarPagamento)
            {
                TentarEnviarEmailPedido(
                    pedido,
                    "Pagamento confirmado - Nana Modas",
                    CriarResumoPedidoEmail(pedido));
            }
            return Resultado<PedidoOnline>.Ok(pedido);
        }
    }

    public Resultado<PedidoOnline> AtualizarRastreamentoPedidoOnline(Guid id, AtualizarRastreamentoPedidoOnlineRequest request)
    {
        lock (_sync)
        {
            var pedido = _pedidosOnline.FirstOrDefault(item => item.Id == id);
            if (pedido is null)
            {
                return Resultado<PedidoOnline>.Falha("Pedido online nao encontrado.");
            }

            pedido.CodigoRastreio = NormalizarTextoOpcional(request.CodigoRastreio);
            pedido.ObservacaoEntrega = NormalizarTextoOpcional(request.ObservacaoEntrega);
            pedido.RastreamentoAtualizadoEm = DateTime.UtcNow;

            SalvarTudo();
            return Resultado<PedidoOnline>.Ok(pedido);
        }
    }

    public RelatorioResumoResponse GerarResumo()
    {
        lock (_sync)
        {
            var produtos = _produtos.Values.ToList();
            var vendasLojaValidas = _vendasLoja
                .Where(venda => !venda.Devolvida)
                .ToList();
            var faturamentoLoja = vendasLojaValidas.Sum(venda => venda.Total);
            var pedidosOnlineValidos = _pedidosOnline
                .Where(pedido => pedido.Status != StatusPedidoOnline.Cancelado)
                .ToList();
            var pedidosOnlineFaturados = pedidosOnlineValidos
                .Where(pedido => pedido.Status != StatusPedidoOnline.Recebido)
                .ToList();
            var faturamentoOnline = pedidosOnlineFaturados.Sum(pedido => pedido.Total);
            var faturamentoTotal = faturamentoLoja + faturamentoOnline;
            var descontosLoja = vendasLojaValidas.Sum(venda => venda.DescontoLiquido);
            var descontosOnline = pedidosOnlineFaturados.Sum(pedido => pedido.Desconto);
            var descontosTotal = descontosLoja + descontosOnline;
            var custoEstoque = produtos.Sum(produto => produto.Custo * produto.QuantidadeEmEstoque);
            var custoVendasLoja = vendasLojaValidas.Sum(venda => venda.Itens.Sum(item => ObterCustoTotalItem(item.ProdutoId, item.CustoUnitario, item.QuantidadeLiquida)));
            var custoPedidosOnline = pedidosOnlineFaturados.Sum(pedido => pedido.Itens.Sum(item => ObterCustoTotalItem(item.ProdutoId, item.CustoUnitario, item.Quantidade)));
            var custoVendidoTotal = custoVendasLoja + custoPedidosOnline;
            var lucroEstimado = faturamentoTotal - custoVendidoTotal;
            var margemLucroPercentual = faturamentoTotal > 0 ? Math.Round((lucroEstimado / faturamentoTotal) * 100, 2) : 0;
            var totalTransacoes = vendasLojaValidas.Count + pedidosOnlineValidos.Count;
            var ticketMedio = totalTransacoes > 0 ? faturamentoTotal / totalTransacoes : 0;
            var vendasPorPagamento = vendasLojaValidas
                .Select(venda => new { FormaPagamento = venda.FormaPagamento.ToString(), venda.Total })
                .Concat(pedidosOnlineFaturados.Select(pedido => new { FormaPagamento = pedido.FormaPagamento.ToString(), pedido.Total }))
                .GroupBy(item => item.FormaPagamento)
                .Select(grupo => new ResumoFormaPagamentoResponse(grupo.Key, grupo.Count(), grupo.Sum(item => item.Total)))
                .OrderByDescending(item => item.Total)
                .ToList();
            var produtosMaisVendidos = vendasLojaValidas
                .SelectMany(venda => venda.Itens.Select(item => new { item.ProdutoId, item.ProdutoNome, Quantidade = item.QuantidadeLiquida, Subtotal = item.SubtotalLiquido }))
                .Concat(pedidosOnlineFaturados.SelectMany(pedido => pedido.Itens.Select(item => new { item.ProdutoId, item.ProdutoNome, item.Quantidade, item.Subtotal })))
                .Where(item => item.Quantidade > 0)
                .GroupBy(item => item.ProdutoId)
                .Select(grupo => new ResumoProdutoVendidoResponse(
                    grupo.Key,
                    grupo.First().ProdutoNome,
                    grupo.Sum(item => item.Quantidade),
                    grupo.Sum(item => item.Subtotal)))
                .OrderByDescending(item => item.Quantidade)
                .ThenByDescending(item => item.Total)
                .Take(5)
                .ToList();
            var produtoMaisVendido = produtosMaisVendidos.FirstOrDefault();
            var vendasPorDia = vendasLojaValidas
                .Select(venda => new
                {
                    Data = venda.CriadaEm.Date,
                    VendasPdv = 1,
                    PedidosOnline = 0,
                    Faturamento = venda.Total,
                    Custo = venda.Itens.Sum(item => ObterCustoTotalItem(item.ProdutoId, item.CustoUnitario, item.QuantidadeLiquida))
                })
                .Concat(pedidosOnlineFaturados.Select(pedido => new
                {
                    Data = pedido.CriadoEm.Date,
                    VendasPdv = 0,
                    PedidosOnline = 1,
                    Faturamento = pedido.Total,
                    Custo = pedido.Itens.Sum(item => ObterCustoTotalItem(item.ProdutoId, item.CustoUnitario, item.Quantidade))
                }))
                .GroupBy(item => item.Data)
                .Select(grupo =>
                {
                    var faturamento = grupo.Sum(item => item.Faturamento);
                    var custo = grupo.Sum(item => item.Custo);
                    var vendasPdv = grupo.Sum(item => item.VendasPdv);
                    var pedidosOnline = grupo.Sum(item => item.PedidosOnline);

                    return new ResumoVendasDiaResponse(
                        grupo.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        vendasPdv,
                        pedidosOnline,
                        vendasPdv + pedidosOnline,
                        faturamento,
                        custo,
                        faturamento - custo);
                })
                .OrderByDescending(item => item.Data)
                .Take(14)
                .ToList();
            var entradasPorFornecedor = _movimentacoes
                .Where(movimentacao => movimentacao.Tipo == TipoMovimentacaoEstoque.Entrada)
                .GroupBy(movimentacao => string.IsNullOrWhiteSpace(movimentacao.FornecedorNome) ? "Sem fornecedor" : movimentacao.FornecedorNome)
                .Select(grupo => new ResumoEntradaFornecedorResponse(
                    grupo.Key!,
                    grupo.Count(),
                    grupo.Sum(item => item.Quantidade),
                    grupo.Sum(item => (item.CustoUnitario ?? ObterCustoProduto(item.ProdutoId)) * item.Quantidade),
                    grupo.Max(item => item.CriadaEm).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
                .OrderByDescending(item => item.CustoTotal)
                .ThenByDescending(item => item.Unidades)
                .Take(8)
                .ToList();
            var produtosEstoqueBaixo = produtos
                .Where(produto => produto.Ativo && produto.QuantidadeEmEstoque <= 3)
                .OrderBy(produto => produto.QuantidadeEmEstoque)
                .ThenBy(produto => produto.Nome)
                .Select(produto => new ResumoEstoqueBaixoResponse(
                    produto.Id,
                    produto.Nome,
                    _categorias.TryGetValue(produto.CategoriaId, out var categoria) ? categoria.Nome : "Sem categoria",
                    produto.QuantidadeEmEstoque))
                .ToList();

            return new RelatorioResumoResponse(
                ProdutosCadastrados: produtos.Count,
                ProdutosAtivos: produtos.Count(produto => produto.Ativo),
                ProdutosComEstoqueBaixo: produtos.Count(produto => produto.QuantidadeEmEstoque <= 3),
                VendasLoja: vendasLojaValidas.Count,
                FaturamentoLoja: faturamentoLoja,
                PedidosOnline: _pedidosOnline.Count,
                FaturamentoOnline: faturamentoOnline,
                FaturamentoTotal: faturamentoTotal,
                DescontosLoja: descontosLoja,
                DescontosOnline: descontosOnline,
                DescontosTotal: descontosTotal,
                CustoEstoque: custoEstoque,
                CustoVendidoTotal: custoVendidoTotal,
                LucroEstimado: lucroEstimado,
                MargemLucroPercentual: margemLucroPercentual,
                TicketMedio: ticketMedio,
                ProdutoMaisVendido: produtoMaisVendido?.ProdutoNome,
                QuantidadeProdutoMaisVendido: produtoMaisVendido?.Quantidade ?? 0,
                VendasPorPagamento: vendasPorPagamento,
                ProdutosMaisVendidos: produtosMaisVendidos,
                VendasPorDia: vendasPorDia,
                EntradasPorFornecedor: entradasPorFornecedor,
                ProdutosEstoqueBaixo: produtosEstoqueBaixo);
        }
    }

    public (byte[] Conteudo, string NomeArquivo) CriarBackupBanco()
    {
        lock (_sync)
        {
            SalvarTudo();
            var nomeArquivo = $"nana-modas-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db";
            return (File.ReadAllBytes(_databasePath), nomeArquivo);
        }
    }

    public IReadOnlyList<BackupArquivoResponse> ListarBackups()
    {
        lock (_sync)
        {
            var backupDirectory = ObterDiretorioBackups();
            Directory.CreateDirectory(backupDirectory);
            return Directory.GetFiles(backupDirectory, "*.db")
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new BackupArquivoResponse(
                        info.Name,
                        info.Length,
                        info.CreationTimeUtc,
                        info.Name.StartsWith("nana-modas-auto-", StringComparison.OrdinalIgnoreCase));
                })
                .OrderByDescending(item => item.CriadoEm)
                .ToList();
        }
    }

    public Resultado<(byte[] Conteudo, string NomeArquivo)> ObterBackupArquivo(string nomeArquivo)
    {
        lock (_sync)
        {
            var nomeSeguro = Path.GetFileName(nomeArquivo);
            if (string.IsNullOrWhiteSpace(nomeSeguro) ||
                !nomeSeguro.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                return Resultado<(byte[] Conteudo, string NomeArquivo)>.Falha("Backup inválido.");
            }

            var caminho = Path.Combine(ObterDiretorioBackups(), nomeSeguro);
            if (!File.Exists(caminho))
            {
                return Resultado<(byte[] Conteudo, string NomeArquivo)>.Falha("Backup não encontrado.");
            }

            return Resultado<(byte[] Conteudo, string NomeArquivo)>.Ok((File.ReadAllBytes(caminho), nomeSeguro));
        }
    }

    public string? CriarBackupAutomaticoSeNecessario()
    {
        // O envio por e-mail faz I/O de rede (SMTP/Brevo) e não pode acontecer dentro do
        // lock: _sync é usado por praticamente toda operação do serviço, e travar a loja
        // inteira até o e-mail terminar de enviar deixaria o site inteiro lento/travado.
        string? destino;
        lock (_sync)
        {
            if (!_configuracaoLoja.BackupAutomaticoAtivo)
            {
                return null;
            }

            var agora = DateTime.UtcNow;
            var intervalo = TimeSpan.FromHours(Math.Clamp(_configuracaoLoja.BackupIntervaloHoras, 1, 720));
            if (_configuracaoLoja.BackupUltimoEm is not null && agora - _configuracaoLoja.BackupUltimoEm < intervalo)
            {
                return null;
            }

            SalvarTudo();
            var backupDirectory = ObterDiretorioBackups();
            Directory.CreateDirectory(backupDirectory);
            var nomeArquivo = $"nana-modas-auto-{agora:yyyyMMdd-HHmmss}.db";
            destino = Path.Combine(backupDirectory, nomeArquivo);
            File.Copy(_databasePath, destino, overwrite: true);
            _configuracaoLoja.BackupUltimoEm = agora;
            _configuracaoLoja.AtualizadoEm = agora;
            SalvarTudo();
        }

        EnviarBackupPorEmailSeConfigurado(destino);
        return destino;
    }

    // Falha ao enviar o backup por e-mail nunca deve invalidar o backup em si
    // (o arquivo já está salvo em disco de qualquer forma).
    private void EnviarBackupPorEmailSeConfigurado(string caminhoArquivoBackup)
    {
        string? destino;
        bool provedorBrevo;
        lock (_sync)
        {
            if (!_configuracaoLoja.BackupEmailAtivo)
            {
                return;
            }

            destino = NormalizarEmailOpcional(_configuracaoLoja.BackupEmailDestino)
                ?? NormalizarEmailOpcional(_configuracaoLoja.EmailPedidosDestino);
            provedorBrevo = _configuracaoLoja.EmailProvedor == "Brevo";

            if (string.IsNullOrWhiteSpace(destino) || string.IsNullOrWhiteSpace(_configuracaoLoja.EmailRemetente))
            {
                return;
            }

            if (provedorBrevo && string.IsNullOrWhiteSpace(_configuracaoLoja.BrevoApiKey))
            {
                return;
            }

            if (!provedorBrevo && string.IsNullOrWhiteSpace(_configuracaoLoja.SmtpHost))
            {
                return;
            }
        }

        try
        {
            EnviarEmail(
                [destino],
                $"Backup Nana Modas - {DateTime.Now:dd/MM/yyyy HH:mm}",
                "Segue em anexo o backup automático do banco de dados da Nana Modas.",
                caminhoAnexo: caminhoArquivoBackup);
        }
        catch (Exception ex)
        {
            RegistrarAtividadePainel("sistema", "Falha no envio do backup por e-mail", ex.Message);
        }
    }

    private string ObterDiretorioBackups()
    {
        return Path.Combine(Path.GetDirectoryName(_databasePath) ?? ".", "backups");
    }

    public Resultado<string> TestarEmailConfiguracao(string? destinoInformado)
    {
        lock (_sync)
        {
            var destino = NormalizarEmailOpcional(destinoInformado) ?? NormalizarEmailOpcional(_configuracaoLoja.EmailPedidosDestino);
            if (string.IsNullOrWhiteSpace(destino))
            {
                return Resultado<string>.Falha("Informe um e-mail de destino valido.");
            }

            var erroConfiguracao = ValidarConfiguracaoEmail();
            if (erroConfiguracao is not null)
            {
                return Resultado<string>.Falha(erroConfiguracao);
            }

            try
            {
                EnviarEmail(
                    [destino],
                    "Teste de e-mail - Nana Modas",
                    $"Teste enviado pelo painel Nana Modas em {DateTime.Now:dd/MM/yyyy HH:mm}.");
                return Resultado<string>.Ok($"E-mail de teste enviado para {destino}.");
            }
            catch (Exception ex)
            {
                return Resultado<string>.Falha(CriarMensagemErroEmail(ex));
            }
        }
    }

    private SmtpClient CriarClienteSmtp()
    {
        var smtp = new SmtpClient(_configuracaoLoja.SmtpHost, _configuracaoLoja.SmtpPorta)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _configuracaoLoja.SmtpSsl,
            Timeout = SmtpTimeoutMs,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_configuracaoLoja.SmtpUsuario))
        {
            smtp.Credentials = new NetworkCredential(_configuracaoLoja.SmtpUsuario, _configuracaoLoja.SmtpSenha);
        }

        return smtp;
    }

    private void EnviarEmail(IEnumerable<string> destinos, string assunto, string corpo, string? responderPara = null, string? caminhoAnexo = null)
    {
        var destinatarios = destinos
            .Select(NormalizarEmailOpcional)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (destinatarios.Count == 0)
        {
            return;
        }

        if (_configuracaoLoja.EmailProvedor == "Brevo")
        {
            EnviarEmailBrevo(destinatarios, assunto, corpo, responderPara, caminhoAnexo);
            return;
        }

        using var mensagem = new MailMessage
        {
            From = new MailAddress(_configuracaoLoja.EmailRemetente),
            Subject = assunto,
            Body = corpo,
            IsBodyHtml = false
        };

        foreach (var destino in destinatarios)
        {
            mensagem.To.Add(destino);
        }

        var replyTo = NormalizarEmailOpcional(responderPara);
        if (replyTo is not null)
        {
            mensagem.ReplyToList.Add(replyTo);
        }

        Attachment? anexo = null;
        if (!string.IsNullOrWhiteSpace(caminhoAnexo) && File.Exists(caminhoAnexo))
        {
            anexo = new Attachment(caminhoAnexo);
            mensagem.Attachments.Add(anexo);
        }

        try
        {
            using var smtp = CriarClienteSmtp();
            smtp.Send(mensagem);
        }
        finally
        {
            anexo?.Dispose();
        }
    }

    private void EnviarEmailBrevo(IReadOnlyCollection<string> destinos, string assunto, string corpo, string? responderPara, string? caminhoAnexo = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new
            {
                email = _configuracaoLoja.EmailRemetente,
                name = "Nana Modas"
            },
            ["to"] = destinos.Select(email => new { email }).ToList(),
            ["subject"] = assunto,
            ["htmlContent"] = CriarHtmlEmail(corpo),
            ["textContent"] = corpo
        };

        var replyTo = NormalizarEmailOpcional(responderPara);
        if (replyTo is not null)
        {
            payload["replyTo"] = new { email = replyTo };
        }

        if (!string.IsNullOrWhiteSpace(caminhoAnexo) && File.Exists(caminhoAnexo))
        {
            payload["attachment"] = new[]
            {
                new
                {
                    content = Convert.ToBase64String(File.ReadAllBytes(caminhoAnexo)),
                    name = Path.GetFileName(caminhoAnexo)
                }
            };
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("api-key", _configuracaoLoja.BrevoApiKey);

        using var response = EmailHttpClient.SendAsync(request).GetAwaiter().GetResult();
        var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(CriarMensagemErroBrevo(response.StatusCode, responseBody));
        }
    }

    private static string CriarHtmlEmail(string texto)
    {
        var corpo = WebUtility.HtmlEncode(texto).Replace("\n", "<br>", StringComparison.Ordinal);
        return $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #1f1f1f; line-height: 1.5;">
            <p>{corpo}</p>
            </body>
            </html>
            """;
    }

    private static string CriarMensagemErroBrevo(HttpStatusCode statusCode, string responseBody)
    {
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "A Brevo recusou a API key. Confira se a chave foi copiada inteira em SMTP & API > API Keys.";
        }

        if (responseBody.Contains("sender", StringComparison.OrdinalIgnoreCase))
        {
            return "A Brevo recusou o remetente. Verifique na Brevo se o e-mail remetente foi confirmado/validado.";
        }

        return $"A Brevo nao aceitou o envio ({(int)statusCode}). Verifique a API key e o remetente na Brevo.";
    }

    private static string CriarMensagemErroEmail(Exception ex)
    {
        var mensagem = ex.Message;
        var detalhe = ex.InnerException?.Message;
        var textoCompleto = $"{mensagem} {detalhe}";

        if (textoCompleto.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            textoCompleto.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return "Nao foi possivel conectar ao serviço de e-mail dentro do tempo limite. Confira a API key/remetente da Brevo ou as credenciais SMTP.";
        }

        if (textoCompleto.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
            textoCompleto.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
            textoCompleto.Contains("5.7", StringComparison.OrdinalIgnoreCase))
        {
            return "O Gmail recusou o login. Use uma senha de app do Google no campo Senha SMTP, nao a senha normal do Gmail.";
        }

        return $"Nao foi possivel enviar o e-mail: {mensagem}";
    }

    private void InicializarBanco()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS SistemaMigracoes (
                Chave TEXT PRIMARY KEY,
                AplicadaEm TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS Categorias (
                Id TEXT PRIMARY KEY,
                Nome TEXT NOT NULL,
                CategoriaPaiId TEXT NULL,
                Ativa INTEGER NOT NULL,
                CriadaEm TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS Clientes (
                Id TEXT PRIMARY KEY,
                Nome TEXT NOT NULL,
                Email TEXT NOT NULL UNIQUE,
                Telefone TEXT NULL,
                SenhaHash TEXT NOT NULL,
                CodigoRecuperacaoHash TEXT NULL,
                CodigoRecuperacaoExpiraEm TEXT NULL,
                CriadoEm TEXT NOT NULL,
                AtualizadoEm TEXT NOT NULL
            );
            """);
        GarantirColuna(connection, "Clientes", "CodigoRecuperacaoHash", "TEXT NULL");
        GarantirColuna(connection, "Clientes", "CodigoRecuperacaoExpiraEm", "TEXT NULL");

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS UsuariosPainel (
                Id TEXT PRIMARY KEY,
                Usuario TEXT NOT NULL UNIQUE,
                NomeExibicao TEXT NOT NULL,
                Perfil TEXT NOT NULL,
                SenhaHash TEXT NOT NULL,
                Ativo INTEGER NOT NULL,
                CriadoEm TEXT NOT NULL,
                AtualizadoEm TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS Fornecedores (
                Id TEXT PRIMARY KEY,
                Nome TEXT NOT NULL,
                Documento TEXT NULL,
                Telefone TEXT NULL,
                Email TEXT NULL,
                Ativo INTEGER NOT NULL,
                CriadoEm TEXT NOT NULL,
                AtualizadoEm TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS Produtos (
                Id TEXT PRIMARY KEY,
                Nome TEXT NOT NULL,
                CategoriaId TEXT NOT NULL,
                Sku TEXT NULL,
                Descricao TEXT NULL,
                ImagemUrl TEXT NULL,
                ImagensExtrasJson TEXT NOT NULL,
                TamanhosJson TEXT NOT NULL,
                CoresJson TEXT NOT NULL,
                ModelosJson TEXT NOT NULL,
                VariacoesEstoqueJson TEXT NOT NULL,
                GuiaMedidas TEXT NULL,
                PublicadoNaLoja INTEGER NOT NULL,
                DestaqueLoja INTEGER NOT NULL,
                OrdemLoja INTEGER NOT NULL,
                NomeLoja TEXT NULL,
                DescricaoLoja TEXT NULL,
                PrecoLoja TEXT NULL,
                ImagemLojaUrl TEXT NULL,
                ImagensLojaExtrasJson TEXT NOT NULL,
                Preco TEXT NOT NULL,
                Custo TEXT NOT NULL,
                QuantidadeEmEstoque INTEGER NOT NULL,
                Ativo INTEGER NOT NULL,
                CriadoEm TEXT NOT NULL,
                AtualizadoEm TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS VendasLoja (
                Id TEXT PRIMARY KEY,
                FormaPagamento TEXT NOT NULL,
                ItensJson TEXT NOT NULL,
                Desconto TEXT NOT NULL,
                ValorRecebido TEXT NOT NULL,
                Observacao TEXT NULL,
                Devolvida INTEGER NOT NULL,
                DevolvidaEm TEXT NULL,
                MotivoDevolucao TEXT NULL,
                CriadaEm TEXT NOT NULL
            );
            """);

        GarantirColuna(connection, "VendasLoja", "Desconto", "TEXT NOT NULL DEFAULT '0'");
        GarantirColuna(connection, "VendasLoja", "ValorRecebido", "TEXT NOT NULL DEFAULT '0'");
        GarantirColuna(connection, "VendasLoja", "Observacao", "TEXT NULL");
        GarantirColuna(connection, "VendasLoja", "Devolvida", "INTEGER NOT NULL DEFAULT 0");
        GarantirColuna(connection, "VendasLoja", "DevolvidaEm", "TEXT NULL");
        GarantirColuna(connection, "VendasLoja", "MotivoDevolucao", "TEXT NULL");

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS PedidosOnline (
                Id TEXT PRIMARY KEY,
                NomeCliente TEXT NOT NULL,
                EmailCliente TEXT NOT NULL,
                TelefoneCliente TEXT NULL,
                DocumentoCliente TEXT NULL,
                ClienteId TEXT NULL,
                EnderecoEntrega TEXT NOT NULL,
                CepEntrega TEXT NULL,
                RuaEntrega TEXT NULL,
                NumeroEntrega TEXT NULL,
                ComplementoEntrega TEXT NULL,
                BairroEntrega TEXT NULL,
                CidadeEntrega TEXT NULL,
                EstadoEntrega TEXT NULL,
                Observacao TEXT NULL,
                CupomCodigo TEXT NULL,
                Desconto TEXT NOT NULL,
                OpcaoEntregaId TEXT NULL,
                EntregaNome TEXT NULL,
                EntregaValor TEXT NOT NULL,
                EntregaPrazoMinimoDias INTEGER NULL,
                EntregaPrazoMaximoDias INTEGER NULL,
                CodigoRastreio TEXT NULL,
                ObservacaoEntrega TEXT NULL,
                RastreamentoAtualizadoEm TEXT NULL,
                ReferenciaPagamento TEXT NULL,
                ObservacaoPagamento TEXT NULL,
                GatewayPagamentoProvedor TEXT NULL,
                GatewayPagamentoId TEXT NULL,
                GatewayPagamentoStatus TEXT NULL,
                PixCopiaECola TEXT NULL,
                PixQrCodeBase64 TEXT NULL,
                PixExpiraEm TEXT NULL,
                UrlPagamento TEXT NULL,
                PagamentoAtualizadoEm TEXT NULL,
                PagamentoConfirmadoEm TEXT NULL,
                FormaPagamento TEXT NOT NULL,
                Status TEXT NOT NULL,
                ItensJson TEXT NOT NULL,
                CriadoEm TEXT NOT NULL
            );
            """);

        GarantirColuna(connection, "PedidosOnline", "ClienteId", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "DocumentoCliente", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "CepEntrega", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "RuaEntrega", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "NumeroEntrega", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "ComplementoEntrega", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "BairroEntrega", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "CidadeEntrega", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "EstadoEntrega", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "Observacao", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "CupomCodigo", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "Desconto", "TEXT NOT NULL DEFAULT '0'");
        GarantirColuna(connection, "PedidosOnline", "OpcaoEntregaId", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "EntregaNome", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "EntregaValor", "TEXT NOT NULL DEFAULT '0'");
        GarantirColuna(connection, "PedidosOnline", "EntregaPrazoMinimoDias", "INTEGER NULL");
        GarantirColuna(connection, "PedidosOnline", "EntregaPrazoMaximoDias", "INTEGER NULL");
        GarantirColuna(connection, "PedidosOnline", "CodigoRastreio", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "ObservacaoEntrega", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "RastreamentoAtualizadoEm", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "ReferenciaPagamento", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "ObservacaoPagamento", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "GatewayPagamentoProvedor", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "GatewayPagamentoId", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "GatewayPagamentoStatus", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "PixCopiaECola", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "PixQrCodeBase64", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "PixExpiraEm", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "UrlPagamento", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "PagamentoAtualizadoEm", "TEXT NULL");
        GarantirColuna(connection, "PedidosOnline", "PagamentoConfirmadoEm", "TEXT NULL");
        GarantirColuna(connection, "Categorias", "CategoriaPaiId", "TEXT NULL");
        GarantirColuna(connection, "Produtos", "Sku", "TEXT NULL");
        GarantirColuna(connection, "Produtos", "TamanhosJson", "TEXT NOT NULL DEFAULT '[]'");
        GarantirColuna(connection, "Produtos", "CoresJson", "TEXT NOT NULL DEFAULT '[]'");
        GarantirColuna(connection, "Produtos", "ModelosJson", "TEXT NOT NULL DEFAULT '[]'");
        GarantirColuna(connection, "Produtos", "VariacoesEstoqueJson", "TEXT NOT NULL DEFAULT '[]'");
        GarantirColuna(connection, "Produtos", "GuiaMedidas", "TEXT NULL");
        GarantirColuna(connection, "Produtos", "Custo", "TEXT NOT NULL DEFAULT '0'");

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS EstoqueMovimentacoes (
                Id TEXT PRIMARY KEY,
                ProdutoId TEXT NOT NULL,
                ProdutoNome TEXT NOT NULL,
                Tipo TEXT NOT NULL,
                Quantidade INTEGER NOT NULL,
                EstoqueAposMovimento INTEGER NOT NULL,
                Origem TEXT NOT NULL,
                VendaLojaId TEXT NULL,
                PedidoOnlineId TEXT NULL,
                FornecedorId TEXT NULL,
                FornecedorNome TEXT NULL,
                CustoUnitario TEXT NULL,
                Documento TEXT NULL,
                CriadaEm TEXT NOT NULL
            );
            """);

        GarantirColuna(connection, "EstoqueMovimentacoes", "FornecedorId", "TEXT NULL");
        GarantirColuna(connection, "EstoqueMovimentacoes", "FornecedorNome", "TEXT NULL");
        GarantirColuna(connection, "EstoqueMovimentacoes", "CustoUnitario", "TEXT NULL");
        GarantirColuna(connection, "EstoqueMovimentacoes", "Documento", "TEXT NULL");

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS AtividadesPainel (
                Id TEXT PRIMARY KEY,
                Usuario TEXT NOT NULL,
                Acao TEXT NOT NULL,
                Detalhe TEXT NULL,
                CriadaEm TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS LojaConfiguracao (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                NomeCriadorSite TEXT NOT NULL,
                PoliticaPrivacidade TEXT NOT NULL,
                FreteValorPadrao TEXT NOT NULL,
                FreteGratisAcimaDe TEXT NOT NULL,
                PrazoMinimoDias INTEGER NOT NULL,
                PrazoMaximoDias INTEGER NOT NULL,
                MensagemFrete TEXT NOT NULL,
                MensagemLoginCliente TEXT NOT NULL,
                BannerEyebrow TEXT NOT NULL,
                BannerTitulo TEXT NOT NULL,
                BannerDescricao TEXT NOT NULL,
                BannerBotaoPrimario TEXT NOT NULL,
                BannerBotaoSecundario TEXT NOT NULL,
                BannerImagemUrl TEXT NOT NULL,
                PromocaoTopoTexto TEXT NOT NULL,
                CampanhaTitulo TEXT NOT NULL,
                CampanhaDescricao TEXT NOT NULL,
                CampanhaBotaoTexto TEXT NOT NULL,
                CampanhaImagemUrl TEXT NOT NULL,
                VitrineImagem1Url TEXT NOT NULL,
                VitrineImagem1Titulo TEXT NOT NULL,
                VitrineImagem2Url TEXT NOT NULL,
                VitrineImagem2Titulo TEXT NOT NULL,
                VitrineImagem3Url TEXT NOT NULL,
                VitrineImagem3Titulo TEXT NOT NULL,
                WhatsappLoja TEXT NOT NULL,
                InstagramLoja TEXT NOT NULL,
                EnderecoLoja TEXT NOT NULL,
                PixChave TEXT NOT NULL,
                PixNomeRecebedor TEXT NOT NULL,
                PixCidade TEXT NOT NULL,
                PixOnlineAtivo INTEGER NOT NULL,
                CartaoOnlineAtivo INTEGER NOT NULL,
                CheckoutCartaoNome TEXT NOT NULL,
                CheckoutCartaoUrl TEXT NOT NULL,
                MensagemPagamento TEXT NOT NULL,
                MensagemPagamentoCartao TEXT NOT NULL,
                EmailNotificacoesAtivo INTEGER NOT NULL,
                EmailProvedor TEXT NOT NULL,
                EmailRemetente TEXT NOT NULL,
                EmailPedidosDestino TEXT NOT NULL,
                BrevoApiKey TEXT NOT NULL,
                SmtpHost TEXT NOT NULL,
                SmtpPorta INTEGER NOT NULL,
                SmtpUsuario TEXT NOT NULL,
                SmtpSenha TEXT NOT NULL,
                SmtpSsl INTEGER NOT NULL,
                BackupAutomaticoAtivo INTEGER NOT NULL,
                BackupIntervaloHoras INTEGER NOT NULL,
                BackupUltimoEm TEXT NULL,
                GatewayPagamentoProvedor TEXT NOT NULL,
                GatewayPagamentoAtivo INTEGER NOT NULL,
                GatewayPagamentoProducao INTEGER NOT NULL,
                GatewayPagamentoPublicKey TEXT NOT NULL,
                GatewayPagamentoAccessToken TEXT NOT NULL,
                GatewayPagamentoWebhookSecret TEXT NOT NULL,
                GatewayPagamentoWebhookUrl TEXT NOT NULL,
                AtualizadoEm TEXT NOT NULL
            );
            """);

        GarantirColuna(connection, "LojaConfiguracao", "BannerEyebrow", "TEXT NOT NULL DEFAULT 'Coleção pronta entrega'");
        GarantirColuna(connection, "LojaConfiguracao", "BannerTitulo", "TEXT NOT NULL DEFAULT 'Nana Modas'");
        GarantirColuna(connection, "LojaConfiguracao", "BannerDescricao", "TEXT NOT NULL DEFAULT 'Peças selecionadas, estética premium e compra online integrada ao estoque da loja física.'");
        GarantirColuna(connection, "LojaConfiguracao", "BannerBotaoPrimario", "TEXT NOT NULL DEFAULT 'Ver coleção'");
        GarantirColuna(connection, "LojaConfiguracao", "BannerBotaoSecundario", "TEXT NOT NULL DEFAULT 'Minha sacola'");
        GarantirColuna(connection, "LojaConfiguracao", "BannerImagemUrl", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "PromocaoTopoTexto", "TEXT NOT NULL DEFAULT 'Compra segura Nana Modas: estoque real, atendimento direto e pagamento por Pix ou cartão.'");
        GarantirColuna(connection, "LojaConfiguracao", "CampanhaTitulo", "TEXT NOT NULL DEFAULT 'Coleção premium pronta entrega'");
        GarantirColuna(connection, "LojaConfiguracao", "CampanhaDescricao", "TEXT NOT NULL DEFAULT 'Banners, promoções e vitrines podem ser trocados no painel sem mexer nas fotos dos produtos.'");
        GarantirColuna(connection, "LojaConfiguracao", "CampanhaBotaoTexto", "TEXT NOT NULL DEFAULT 'Ver novidades'");
        GarantirColuna(connection, "LojaConfiguracao", "CampanhaImagemUrl", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "VitrineImagem1Url", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "VitrineImagem1Titulo", "TEXT NOT NULL DEFAULT 'Novidades'");
        GarantirColuna(connection, "LojaConfiguracao", "VitrineImagem2Url", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "VitrineImagem2Titulo", "TEXT NOT NULL DEFAULT 'Promoções'");
        GarantirColuna(connection, "LojaConfiguracao", "VitrineImagem3Url", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "VitrineImagem3Titulo", "TEXT NOT NULL DEFAULT 'Mais desejados'");
        GarantirColuna(connection, "LojaConfiguracao", "WhatsappLoja", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "InstagramLoja", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "EnderecoLoja", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "PixChave", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "PixNomeRecebedor", "TEXT NOT NULL DEFAULT 'NANA MODAS'");
        GarantirColuna(connection, "LojaConfiguracao", "PixCidade", "TEXT NOT NULL DEFAULT 'SAO PAULO'");
        GarantirColuna(connection, "LojaConfiguracao", "PixOnlineAtivo", "INTEGER NOT NULL DEFAULT 1");
        GarantirColuna(connection, "LojaConfiguracao", "CartaoOnlineAtivo", "INTEGER NOT NULL DEFAULT 1");
        GarantirColuna(connection, "LojaConfiguracao", "CheckoutCartaoNome", "TEXT NOT NULL DEFAULT 'Link de pagamento'");
        GarantirColuna(connection, "LojaConfiguracao", "CheckoutCartaoUrl", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "MensagemPagamento", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "MensagemPagamentoCartao", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "EmailNotificacoesAtivo", "INTEGER NOT NULL DEFAULT 0");
        GarantirColuna(connection, "LojaConfiguracao", "EmailProvedor", "TEXT NOT NULL DEFAULT 'Brevo'");
        GarantirColuna(connection, "LojaConfiguracao", "EmailRemetente", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "EmailPedidosDestino", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "BrevoApiKey", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "SmtpHost", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "SmtpPorta", "INTEGER NOT NULL DEFAULT 587");
        GarantirColuna(connection, "LojaConfiguracao", "SmtpUsuario", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "SmtpSenha", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "SmtpSsl", "INTEGER NOT NULL DEFAULT 1");
        GarantirColuna(connection, "LojaConfiguracao", "BackupAutomaticoAtivo", "INTEGER NOT NULL DEFAULT 1");
        GarantirColuna(connection, "LojaConfiguracao", "BackupIntervaloHoras", "INTEGER NOT NULL DEFAULT 24");
        GarantirColuna(connection, "LojaConfiguracao", "BackupUltimoEm", "TEXT NULL");
        GarantirColuna(connection, "LojaConfiguracao", "GatewayPagamentoProvedor", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "GatewayPagamentoAtivo", "INTEGER NOT NULL DEFAULT 0");
        GarantirColuna(connection, "LojaConfiguracao", "GatewayPagamentoProducao", "INTEGER NOT NULL DEFAULT 0");
        GarantirColuna(connection, "LojaConfiguracao", "GatewayPagamentoPublicKey", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "GatewayPagamentoAccessToken", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "GatewayPagamentoWebhookSecret", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "GatewayPagamentoWebhookUrl", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "RazaoSocial", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "Cnpj", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "SiteUrlCanonica", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "PoliticaTrocaDevolucao", "TEXT NOT NULL DEFAULT 'Você pode desistir da compra em até 7 dias corridos após o recebimento, sem precisar justificar o motivo, conforme o art. 49 do Código de Defesa do Consumidor. Nesse caso, o valor pago é reembolsado integralmente, incluindo o frete. Para trocas por tamanho/cor ou devolução por defeito, entre em contato pelos canais de atendimento da loja informando o número do pedido; o produto deve estar sem uso, com etiqueta e embalagem originais.'");
        GarantirColuna(connection, "LojaConfiguracao", "GoogleAnalyticsId", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "MetaPixelId", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna(connection, "LojaConfiguracao", "BackupEmailAtivo", "INTEGER NOT NULL DEFAULT 0");
        GarantirColuna(connection, "LojaConfiguracao", "BackupEmailDestino", "TEXT NOT NULL DEFAULT ''");

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS CuponsDesconto (
                Id TEXT PRIMARY KEY,
                Codigo TEXT NOT NULL,
                Descricao TEXT NULL,
                PercentualDesconto TEXT NOT NULL,
                ValorMinimoPedido TEXT NOT NULL,
                Ativo INTEGER NOT NULL,
                ValidoAte TEXT NULL,
                CriadoEm TEXT NOT NULL,
                AtualizadoEm TEXT NOT NULL
            );
            """);

        ExecuteNonQuery(connection, null, """
            CREATE TABLE IF NOT EXISTS OpcoesEntrega (
                Id TEXT PRIMARY KEY,
                Nome TEXT NOT NULL,
                Tipo TEXT NOT NULL,
                Descricao TEXT NULL,
                Valor TEXT NOT NULL,
                FreteGratisAcimaDe TEXT NOT NULL,
                PrazoMinimoDias INTEGER NOT NULL,
                PrazoMaximoDias INTEGER NOT NULL,
                CepInicial TEXT NULL,
                CepFinal TEXT NULL,
                CidadesJson TEXT NOT NULL,
                BairrosJson TEXT NOT NULL,
                EstadosJson TEXT NOT NULL,
                Ativo INTEGER NOT NULL,
                Ordem INTEGER NOT NULL,
                CriadoEm TEXT NOT NULL,
                AtualizadoEm TEXT NOT NULL
            );
            """);
    }

    private bool ZerarEstoqueParaEntregaSePendente()
    {
        if (MigracaoAplicada(MigracaoZerarEstoqueEntrega))
        {
            return false;
        }

        var produtosAlterados = 0;
        var unidadesRemovidas = 0;
        foreach (var produto in _produtos.Values)
        {
            var estoqueVariacoes = produto.VariacoesEstoque.Sum(variacao => variacao.Quantidade);
            var estoqueAnterior = Math.Max(produto.QuantidadeEmEstoque, estoqueVariacoes);
            if (estoqueAnterior <= 0)
            {
                continue;
            }

            foreach (var variacao in produto.VariacoesEstoque)
            {
                variacao.Quantidade = 0;
            }

            produto.QuantidadeEmEstoque = 0;
            produto.AtualizadoEm = DateTime.UtcNow;
            produtosAlterados += 1;
            unidadesRemovidas += estoqueAnterior;
            RegistrarMovimentacao(
                produto,
                TipoMovimentacaoEstoque.Ajuste,
                estoqueAnterior,
                "Zeragem de estoque para entrega do sistema",
                null);
        }

        _atividadesPainel.Add(new AtividadePainel
        {
            Usuario = "sistema",
            Acao = "Estoque zerado para entrega",
            Detalhe = $"{produtosAlterados} produto(s) e {unidadesRemovidas} unidade(s) ajustados para zero."
        });

        return true;
    }

    private bool RenomearEntregaLocalSePendente()
    {
        if (MigracaoAplicada(MigracaoRenomearEntregaLocal))
        {
            return false;
        }

        var opcoesAlteradas = _opcoesEntrega.Values
            .Where(opcao =>
                string.Equals(opcao.Tipo, "EntregaLocal", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(opcao.Nome, "Davi Silva Dias", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var opcao in opcoesAlteradas)
        {
            opcao.Nome = "Entrega local Nana Modas";
            opcao.Descricao = "Entrega combinada pela Nana Modas em Alfenas e regiões atendidas.";
            opcao.AtualizadoEm = DateTime.UtcNow;
        }

        if (opcoesAlteradas.Count > 0)
        {
            _atividadesPainel.Add(new AtividadePainel
            {
                Usuario = "sistema",
                Acao = "Entrega local atualizada",
                Detalhe = "A opção de entrega foi renomeada para Entrega local Nana Modas."
            });
        }

        return true;
    }

    private bool MigracaoAplicada(string chave)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = CreateCommand(
            connection,
            null,
            "SELECT COUNT(1) FROM SistemaMigracoes WHERE Chave = $Chave;");
        Add(command, "$Chave", chave);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    private void RegistrarMigracaoAplicada(string chave)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = CreateCommand(
            connection,
            null,
            """
            INSERT OR IGNORE INTO SistemaMigracoes (Chave, AplicadaEm)
            VALUES ($Chave, $AplicadaEm);
            """);
        Add(command, "$Chave", chave);
        Add(command, "$AplicadaEm", DateTime.UtcNow);
        command.ExecuteNonQuery();
    }

    private void CarregarDados()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        CarregarCategorias(connection);
        CarregarClientes(connection);
        CarregarUsuariosPainel(connection);
        CarregarFornecedores(connection);
        CarregarProdutos(connection);
        CarregarVendasLoja(connection);
        CarregarPedidosOnline(connection);
        CarregarMovimentacoes(connection);
        CarregarAtividadesPainel(connection);
        CarregarConfiguracaoLoja(connection);
        CarregarCupons(connection);
        CarregarOpcoesEntrega(connection);
    }

    private void SalvarTudo()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, transaction, "DELETE FROM Categorias;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM Clientes;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM UsuariosPainel;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM Fornecedores;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM Produtos;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM VendasLoja;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM PedidosOnline;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM EstoqueMovimentacoes;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM AtividadesPainel;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM LojaConfiguracao;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM CuponsDesconto;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM OpcoesEntrega;");

        foreach (var categoria in _categorias.Values)
        {
            SalvarCategoria(connection, transaction, categoria);
        }

        foreach (var cliente in _clientes.Values)
        {
            SalvarCliente(connection, transaction, cliente);
        }

        foreach (var usuario in _usuariosPainel.Values)
        {
            SalvarUsuarioPainel(connection, transaction, usuario);
        }

        foreach (var fornecedor in _fornecedores.Values)
        {
            SalvarFornecedor(connection, transaction, fornecedor);
        }

        foreach (var produto in _produtos.Values)
        {
            SalvarProduto(connection, transaction, produto);
        }

        foreach (var venda in _vendasLoja)
        {
            SalvarVendaLoja(connection, transaction, venda);
        }

        foreach (var pedido in _pedidosOnline)
        {
            SalvarPedidoOnline(connection, transaction, pedido);
        }

        foreach (var movimentacao in _movimentacoes)
        {
            SalvarMovimentacao(connection, transaction, movimentacao);
        }

        foreach (var atividade in _atividadesPainel.TakeLast(500))
        {
            SalvarAtividadePainel(connection, transaction, atividade);
        }

        SalvarConfiguracaoLoja(connection, transaction);

        foreach (var cupom in _cupons.Values)
        {
            SalvarCupom(connection, transaction, cupom);
        }

        foreach (var opcao in _opcoesEntrega.Values)
        {
            SalvarOpcaoEntrega(connection, transaction, opcao);
        }

        transaction.Commit();
    }

    private void CarregarCategorias(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM Categorias;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var categoria = new Categoria
            {
                Id = ReadGuid(reader, "Id"),
                Nome = ReadString(reader, "Nome"),
                CategoriaPaiId = ReadNullableGuid(reader, "CategoriaPaiId"),
                Ativa = ReadBool(reader, "Ativa"),
                CriadaEm = ReadDateTime(reader, "CriadaEm")
            };

            _categorias[categoria.Id] = categoria;
        }
    }

    private void CarregarClientes(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM Clientes;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var cliente = new Cliente
            {
                Id = ReadGuid(reader, "Id"),
                Nome = ReadString(reader, "Nome"),
                Email = ReadString(reader, "Email"),
                Telefone = ReadNullableString(reader, "Telefone"),
                SenhaHash = ReadString(reader, "SenhaHash"),
                CodigoRecuperacaoHash = ReadNullableString(reader, "CodigoRecuperacaoHash"),
                CodigoRecuperacaoExpiraEm = ReadNullableDateTime(reader, "CodigoRecuperacaoExpiraEm"),
                CriadoEm = ReadDateTime(reader, "CriadoEm"),
                AtualizadoEm = ReadDateTime(reader, "AtualizadoEm")
            };

            _clientes[cliente.Id] = cliente;
        }
    }

    private void CarregarUsuariosPainel(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM UsuariosPainel;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var usuario = new UsuarioPainel
            {
                Id = ReadGuid(reader, "Id"),
                Usuario = ReadString(reader, "Usuario"),
                NomeExibicao = ReadString(reader, "NomeExibicao"),
                Perfil = ReadString(reader, "Perfil"),
                SenhaHash = ReadString(reader, "SenhaHash"),
                Ativo = ReadBool(reader, "Ativo"),
                CriadoEm = ReadDateTime(reader, "CriadoEm"),
                AtualizadoEm = ReadDateTime(reader, "AtualizadoEm")
            };

            _usuariosPainel[usuario.Id] = usuario;
        }
    }

    private void CarregarFornecedores(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM Fornecedores;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var fornecedor = new Fornecedor
            {
                Id = ReadGuid(reader, "Id"),
                Nome = ReadString(reader, "Nome"),
                Documento = ReadNullableString(reader, "Documento"),
                Telefone = ReadNullableString(reader, "Telefone"),
                Email = ReadNullableString(reader, "Email"),
                Ativo = ReadBool(reader, "Ativo"),
                CriadoEm = ReadDateTime(reader, "CriadoEm"),
                AtualizadoEm = ReadDateTime(reader, "AtualizadoEm")
            };

            _fornecedores[fornecedor.Id] = fornecedor;
        }
    }

    private void CarregarProdutos(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM Produtos;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var produto = new Produto
            {
                Id = ReadGuid(reader, "Id"),
                Nome = ReadString(reader, "Nome"),
                CategoriaId = ReadGuid(reader, "CategoriaId"),
                Sku = ReadNullableString(reader, "Sku"),
                Descricao = ReadNullableString(reader, "Descricao"),
                ImagemUrl = ReadNullableString(reader, "ImagemUrl"),
                ImagensExtras = DeserializeJson(ReadString(reader, "ImagensExtrasJson"), new List<string>()),
                Tamanhos = DeserializeJson(ReadString(reader, "TamanhosJson"), new List<string>()),
                Cores = DeserializeJson(ReadString(reader, "CoresJson"), new List<string>()),
                Modelos = DeserializeJson(ReadString(reader, "ModelosJson"), new List<string>()),
                VariacoesEstoque = DeserializeJson(ReadString(reader, "VariacoesEstoqueJson"), new List<ProdutoVariacaoEstoque>()),
                GuiaMedidas = ReadNullableString(reader, "GuiaMedidas"),
                PublicadoNaLoja = ReadBool(reader, "PublicadoNaLoja"),
                DestaqueLoja = ReadBool(reader, "DestaqueLoja"),
                OrdemLoja = ReadInt(reader, "OrdemLoja"),
                NomeLoja = ReadNullableString(reader, "NomeLoja"),
                DescricaoLoja = ReadNullableString(reader, "DescricaoLoja"),
                PrecoLoja = ReadNullableDecimal(reader, "PrecoLoja"),
                ImagemLojaUrl = ReadNullableString(reader, "ImagemLojaUrl"),
                ImagensLojaExtras = DeserializeJson(ReadString(reader, "ImagensLojaExtrasJson"), new List<string>()),
                Preco = ReadDecimal(reader, "Preco"),
                Custo = ReadDecimal(reader, "Custo"),
                QuantidadeEmEstoque = ReadInt(reader, "QuantidadeEmEstoque"),
                Ativo = ReadBool(reader, "Ativo"),
                CriadoEm = ReadDateTime(reader, "CriadoEm"),
                AtualizadoEm = ReadDateTime(reader, "AtualizadoEm")
            };

            _produtos[produto.Id] = produto;
        }
    }

    private void CarregarVendasLoja(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM VendasLoja;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var venda = new VendaLoja
            {
                Id = ReadGuid(reader, "Id"),
                FormaPagamento = ReadEnum(reader, "FormaPagamento", FormaPagamento.Pix),
                Itens = DeserializeJson(ReadString(reader, "ItensJson"), new List<ItemVendaLoja>()),
                Desconto = ReadDecimal(reader, "Desconto"),
                ValorRecebido = ReadDecimal(reader, "ValorRecebido"),
                Observacao = ReadNullableString(reader, "Observacao"),
                Devolvida = ReadBool(reader, "Devolvida"),
                DevolvidaEm = ReadNullableDateTime(reader, "DevolvidaEm"),
                MotivoDevolucao = ReadNullableString(reader, "MotivoDevolucao"),
                CriadaEm = ReadDateTime(reader, "CriadaEm")
            };

            if (venda.Devolvida && venda.Itens.All(item => item.QuantidadeDevolvida == 0))
            {
                foreach (var item in venda.Itens)
                {
                    item.QuantidadeDevolvida = item.Quantidade;
                }
            }

            _vendasLoja.Add(venda);
        }
    }

    private void CarregarPedidosOnline(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM PedidosOnline;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            _pedidosOnline.Add(new PedidoOnline
            {
                Id = ReadGuid(reader, "Id"),
                NomeCliente = ReadString(reader, "NomeCliente"),
                EmailCliente = ReadString(reader, "EmailCliente"),
                TelefoneCliente = ReadNullableString(reader, "TelefoneCliente"),
                DocumentoCliente = ReadNullableString(reader, "DocumentoCliente"),
                ClienteId = ReadNullableGuid(reader, "ClienteId"),
                EnderecoEntrega = ReadString(reader, "EnderecoEntrega"),
                CepEntrega = ReadNullableString(reader, "CepEntrega"),
                RuaEntrega = ReadNullableString(reader, "RuaEntrega"),
                NumeroEntrega = ReadNullableString(reader, "NumeroEntrega"),
                ComplementoEntrega = ReadNullableString(reader, "ComplementoEntrega"),
                BairroEntrega = ReadNullableString(reader, "BairroEntrega"),
                CidadeEntrega = ReadNullableString(reader, "CidadeEntrega"),
                EstadoEntrega = ReadNullableString(reader, "EstadoEntrega"),
                Observacao = ReadNullableString(reader, "Observacao"),
                CupomCodigo = ReadNullableString(reader, "CupomCodigo"),
                Desconto = ReadDecimal(reader, "Desconto"),
                OpcaoEntregaId = ReadNullableGuid(reader, "OpcaoEntregaId"),
                EntregaNome = ReadNullableString(reader, "EntregaNome"),
                EntregaValor = ReadDecimal(reader, "EntregaValor"),
                EntregaPrazoMinimoDias = ReadNullableInt(reader, "EntregaPrazoMinimoDias"),
                EntregaPrazoMaximoDias = ReadNullableInt(reader, "EntregaPrazoMaximoDias"),
                CodigoRastreio = ReadNullableString(reader, "CodigoRastreio"),
                ObservacaoEntrega = ReadNullableString(reader, "ObservacaoEntrega"),
                RastreamentoAtualizadoEm = ReadNullableDateTime(reader, "RastreamentoAtualizadoEm"),
                ReferenciaPagamento = ReadNullableString(reader, "ReferenciaPagamento"),
                ObservacaoPagamento = ReadNullableString(reader, "ObservacaoPagamento"),
                GatewayPagamentoProvedor = ReadNullableString(reader, "GatewayPagamentoProvedor"),
                GatewayPagamentoId = ReadNullableString(reader, "GatewayPagamentoId"),
                GatewayPagamentoStatus = ReadNullableString(reader, "GatewayPagamentoStatus"),
                PixCopiaECola = ReadNullableString(reader, "PixCopiaECola"),
                PixQrCodeBase64 = ReadNullableString(reader, "PixQrCodeBase64"),
                PixExpiraEm = ReadNullableDateTime(reader, "PixExpiraEm"),
                UrlPagamento = ReadNullableString(reader, "UrlPagamento"),
                PagamentoAtualizadoEm = ReadNullableDateTime(reader, "PagamentoAtualizadoEm"),
                PagamentoConfirmadoEm = ReadNullableDateTime(reader, "PagamentoConfirmadoEm"),
                FormaPagamento = ReadEnum(reader, "FormaPagamento", FormaPagamento.Pix),
                Status = ReadEnum(reader, "Status", StatusPedidoOnline.Pago),
                Itens = DeserializeJson(ReadString(reader, "ItensJson"), new List<ItemPedidoOnline>()),
                CriadoEm = ReadDateTime(reader, "CriadoEm")
            });
        }
    }

    private void CarregarMovimentacoes(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM EstoqueMovimentacoes;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            _movimentacoes.Add(new EstoqueMovimentacao
            {
                Id = ReadGuid(reader, "Id"),
                ProdutoId = ReadGuid(reader, "ProdutoId"),
                ProdutoNome = ReadString(reader, "ProdutoNome"),
                Tipo = ReadEnum(reader, "Tipo", TipoMovimentacaoEstoque.Ajuste),
                Quantidade = ReadInt(reader, "Quantidade"),
                EstoqueAposMovimento = ReadInt(reader, "EstoqueAposMovimento"),
                Origem = ReadString(reader, "Origem"),
                VendaLojaId = ReadNullableGuid(reader, "VendaLojaId"),
                PedidoOnlineId = ReadNullableGuid(reader, "PedidoOnlineId"),
                FornecedorId = ReadNullableGuid(reader, "FornecedorId"),
                FornecedorNome = ReadNullableString(reader, "FornecedorNome"),
                CustoUnitario = ReadNullableDecimal(reader, "CustoUnitario"),
                Documento = ReadNullableString(reader, "Documento"),
                CriadaEm = ReadDateTime(reader, "CriadaEm")
            });
        }
    }

    private void CarregarAtividadesPainel(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM AtividadesPainel;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            _atividadesPainel.Add(new AtividadePainel
            {
                Id = ReadGuid(reader, "Id"),
                Usuario = ReadString(reader, "Usuario"),
                Acao = ReadString(reader, "Acao"),
                Detalhe = ReadNullableString(reader, "Detalhe"),
                CriadaEm = ReadDateTime(reader, "CriadaEm")
            });
        }
    }

    private void CarregarConfiguracaoLoja(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM LojaConfiguracao WHERE Id = 1;");
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return;
        }

        _configuracaoLoja.NomeCriadorSite = ReadString(reader, "NomeCriadorSite");
        _configuracaoLoja.PoliticaPrivacidade = ReadString(reader, "PoliticaPrivacidade");
        _configuracaoLoja.FreteValorPadrao = ReadDecimal(reader, "FreteValorPadrao");
        _configuracaoLoja.FreteGratisAcimaDe = ReadDecimal(reader, "FreteGratisAcimaDe");
        _configuracaoLoja.PrazoMinimoDias = ReadInt(reader, "PrazoMinimoDias");
        _configuracaoLoja.PrazoMaximoDias = ReadInt(reader, "PrazoMaximoDias");
        _configuracaoLoja.MensagemFrete = ReadString(reader, "MensagemFrete");
        _configuracaoLoja.MensagemLoginCliente = ReadString(reader, "MensagemLoginCliente");
        _configuracaoLoja.BannerEyebrow = ReadString(reader, "BannerEyebrow");
        _configuracaoLoja.BannerTitulo = ReadString(reader, "BannerTitulo");
        _configuracaoLoja.BannerDescricao = ReadString(reader, "BannerDescricao");
        _configuracaoLoja.BannerBotaoPrimario = ReadString(reader, "BannerBotaoPrimario");
        _configuracaoLoja.BannerBotaoSecundario = ReadString(reader, "BannerBotaoSecundario");
        _configuracaoLoja.BannerImagemUrl = ReadString(reader, "BannerImagemUrl");
        _configuracaoLoja.PromocaoTopoTexto = ReadString(reader, "PromocaoTopoTexto");
        _configuracaoLoja.CampanhaTitulo = ReadString(reader, "CampanhaTitulo");
        _configuracaoLoja.CampanhaDescricao = ReadString(reader, "CampanhaDescricao");
        _configuracaoLoja.CampanhaBotaoTexto = ReadString(reader, "CampanhaBotaoTexto");
        _configuracaoLoja.CampanhaImagemUrl = ReadString(reader, "CampanhaImagemUrl");
        _configuracaoLoja.VitrineImagem1Url = ReadString(reader, "VitrineImagem1Url");
        _configuracaoLoja.VitrineImagem1Titulo = ReadString(reader, "VitrineImagem1Titulo");
        _configuracaoLoja.VitrineImagem2Url = ReadString(reader, "VitrineImagem2Url");
        _configuracaoLoja.VitrineImagem2Titulo = ReadString(reader, "VitrineImagem2Titulo");
        _configuracaoLoja.VitrineImagem3Url = ReadString(reader, "VitrineImagem3Url");
        _configuracaoLoja.VitrineImagem3Titulo = ReadString(reader, "VitrineImagem3Titulo");
        _configuracaoLoja.WhatsappLoja = ReadString(reader, "WhatsappLoja");
        _configuracaoLoja.InstagramLoja = ReadString(reader, "InstagramLoja");
        _configuracaoLoja.EnderecoLoja = ReadString(reader, "EnderecoLoja");
        _configuracaoLoja.PixChave = ReadString(reader, "PixChave");
        _configuracaoLoja.PixNomeRecebedor = ReadString(reader, "PixNomeRecebedor");
        _configuracaoLoja.PixCidade = ReadString(reader, "PixCidade");
        _configuracaoLoja.PixOnlineAtivo = ReadBool(reader, "PixOnlineAtivo");
        _configuracaoLoja.CartaoOnlineAtivo = ReadBool(reader, "CartaoOnlineAtivo");
        _configuracaoLoja.CheckoutCartaoNome = ReadString(reader, "CheckoutCartaoNome");
        _configuracaoLoja.CheckoutCartaoUrl = ReadString(reader, "CheckoutCartaoUrl");
        _configuracaoLoja.MensagemPagamento = ReadString(reader, "MensagemPagamento");
        _configuracaoLoja.MensagemPagamentoCartao = ReadString(reader, "MensagemPagamentoCartao");
        _configuracaoLoja.EmailNotificacoesAtivo = ReadBool(reader, "EmailNotificacoesAtivo");
        _configuracaoLoja.EmailProvedor = NormalizarProvedorEmail(ReadString(reader, "EmailProvedor"));
        _configuracaoLoja.EmailRemetente = ReadString(reader, "EmailRemetente");
        _configuracaoLoja.EmailPedidosDestino = ReadString(reader, "EmailPedidosDestino");
        _configuracaoLoja.BrevoApiKey = ReadString(reader, "BrevoApiKey");
        _configuracaoLoja.SmtpHost = ReadString(reader, "SmtpHost");
        _configuracaoLoja.SmtpPorta = ReadInt(reader, "SmtpPorta");
        _configuracaoLoja.SmtpUsuario = ReadString(reader, "SmtpUsuario");
        _configuracaoLoja.SmtpSenha = ReadString(reader, "SmtpSenha");
        _configuracaoLoja.SmtpSsl = ReadBool(reader, "SmtpSsl");
        _configuracaoLoja.BackupAutomaticoAtivo = ReadBool(reader, "BackupAutomaticoAtivo");
        _configuracaoLoja.BackupIntervaloHoras = ReadInt(reader, "BackupIntervaloHoras");
        _configuracaoLoja.BackupUltimoEm = ReadNullableDateTime(reader, "BackupUltimoEm");
        _configuracaoLoja.GatewayPagamentoProvedor = ReadString(reader, "GatewayPagamentoProvedor");
        _configuracaoLoja.GatewayPagamentoAtivo = ReadBool(reader, "GatewayPagamentoAtivo");
        _configuracaoLoja.GatewayPagamentoProducao = ReadBool(reader, "GatewayPagamentoProducao");
        _configuracaoLoja.GatewayPagamentoPublicKey = ReadString(reader, "GatewayPagamentoPublicKey");
        _configuracaoLoja.GatewayPagamentoAccessToken = ReadString(reader, "GatewayPagamentoAccessToken");
        _configuracaoLoja.GatewayPagamentoWebhookSecret = ReadString(reader, "GatewayPagamentoWebhookSecret");
        _configuracaoLoja.GatewayPagamentoWebhookUrl = ReadString(reader, "GatewayPagamentoWebhookUrl");
        _configuracaoLoja.RazaoSocial = ReadString(reader, "RazaoSocial");
        _configuracaoLoja.Cnpj = ReadString(reader, "Cnpj");
        _configuracaoLoja.SiteUrlCanonica = ReadString(reader, "SiteUrlCanonica");
        _configuracaoLoja.PoliticaTrocaDevolucao = ReadString(reader, "PoliticaTrocaDevolucao");
        _configuracaoLoja.GoogleAnalyticsId = ReadString(reader, "GoogleAnalyticsId");
        _configuracaoLoja.MetaPixelId = ReadString(reader, "MetaPixelId");
        _configuracaoLoja.BackupEmailAtivo = ReadBool(reader, "BackupEmailAtivo");
        _configuracaoLoja.BackupEmailDestino = ReadString(reader, "BackupEmailDestino");
        _configuracaoLoja.AtualizadoEm = ReadDateTime(reader, "AtualizadoEm");
    }

    private void CarregarCupons(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM CuponsDesconto;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var cupom = new CupomDesconto
            {
                Id = ReadGuid(reader, "Id"),
                Codigo = ReadString(reader, "Codigo"),
                Descricao = ReadNullableString(reader, "Descricao"),
                PercentualDesconto = ReadDecimal(reader, "PercentualDesconto"),
                ValorMinimoPedido = ReadDecimal(reader, "ValorMinimoPedido"),
                Ativo = ReadBool(reader, "Ativo"),
                ValidoAte = ReadNullableDateTime(reader, "ValidoAte"),
                CriadoEm = ReadDateTime(reader, "CriadoEm"),
                AtualizadoEm = ReadDateTime(reader, "AtualizadoEm")
            };

            _cupons[cupom.Id] = cupom;
        }
    }

    private void CarregarOpcoesEntrega(SqliteConnection connection)
    {
        using var command = CreateCommand(connection, null, "SELECT * FROM OpcoesEntrega;");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var opcao = new OpcaoEntrega
            {
                Id = ReadGuid(reader, "Id"),
                Nome = ReadString(reader, "Nome"),
                Tipo = ReadString(reader, "Tipo"),
                Descricao = ReadNullableString(reader, "Descricao"),
                Valor = ReadDecimal(reader, "Valor"),
                FreteGratisAcimaDe = ReadDecimal(reader, "FreteGratisAcimaDe"),
                PrazoMinimoDias = ReadInt(reader, "PrazoMinimoDias"),
                PrazoMaximoDias = ReadInt(reader, "PrazoMaximoDias"),
                CepInicial = ReadNullableString(reader, "CepInicial"),
                CepFinal = ReadNullableString(reader, "CepFinal"),
                Cidades = DeserializeJson(ReadString(reader, "CidadesJson"), new List<string>()),
                Bairros = DeserializeJson(ReadString(reader, "BairrosJson"), new List<string>()),
                Estados = DeserializeJson(ReadString(reader, "EstadosJson"), new List<string>()),
                Ativo = ReadBool(reader, "Ativo"),
                Ordem = ReadInt(reader, "Ordem"),
                CriadoEm = ReadDateTime(reader, "CriadoEm"),
                AtualizadoEm = ReadDateTime(reader, "AtualizadoEm")
            };

            _opcoesEntrega[opcao.Id] = opcao;
        }
    }

    private void SalvarCategoria(SqliteConnection connection, SqliteTransaction transaction, Categoria categoria)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO Categorias (Id, Nome, CategoriaPaiId, Ativa, CriadaEm)
            VALUES ($Id, $Nome, $CategoriaPaiId, $Ativa, $CriadaEm);
            """);
        Add(command, "$Id", categoria.Id);
        Add(command, "$Nome", categoria.Nome);
        Add(command, "$CategoriaPaiId", categoria.CategoriaPaiId);
        Add(command, "$Ativa", categoria.Ativa);
        Add(command, "$CriadaEm", categoria.CriadaEm);
        command.ExecuteNonQuery();
    }

    private void SalvarCliente(SqliteConnection connection, SqliteTransaction transaction, Cliente cliente)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO Clientes (Id, Nome, Email, Telefone, SenhaHash, CodigoRecuperacaoHash, CodigoRecuperacaoExpiraEm, CriadoEm, AtualizadoEm)
            VALUES ($Id, $Nome, $Email, $Telefone, $SenhaHash, $CodigoRecuperacaoHash, $CodigoRecuperacaoExpiraEm, $CriadoEm, $AtualizadoEm);
            """);
        Add(command, "$Id", cliente.Id);
        Add(command, "$Nome", cliente.Nome);
        Add(command, "$Email", cliente.Email);
        Add(command, "$Telefone", cliente.Telefone);
        Add(command, "$SenhaHash", cliente.SenhaHash);
        Add(command, "$CodigoRecuperacaoHash", cliente.CodigoRecuperacaoHash);
        Add(command, "$CodigoRecuperacaoExpiraEm", cliente.CodigoRecuperacaoExpiraEm);
        Add(command, "$CriadoEm", cliente.CriadoEm);
        Add(command, "$AtualizadoEm", cliente.AtualizadoEm);
        command.ExecuteNonQuery();
    }

    private void SalvarUsuarioPainel(SqliteConnection connection, SqliteTransaction transaction, UsuarioPainel usuario)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO UsuariosPainel (Id, Usuario, NomeExibicao, Perfil, SenhaHash, Ativo, CriadoEm, AtualizadoEm)
            VALUES ($Id, $Usuario, $NomeExibicao, $Perfil, $SenhaHash, $Ativo, $CriadoEm, $AtualizadoEm);
            """);
        Add(command, "$Id", usuario.Id);
        Add(command, "$Usuario", usuario.Usuario);
        Add(command, "$NomeExibicao", usuario.NomeExibicao);
        Add(command, "$Perfil", usuario.Perfil);
        Add(command, "$SenhaHash", usuario.SenhaHash);
        Add(command, "$Ativo", usuario.Ativo);
        Add(command, "$CriadoEm", usuario.CriadoEm);
        Add(command, "$AtualizadoEm", usuario.AtualizadoEm);
        command.ExecuteNonQuery();
    }

    private void SalvarFornecedor(SqliteConnection connection, SqliteTransaction transaction, Fornecedor fornecedor)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO Fornecedores (Id, Nome, Documento, Telefone, Email, Ativo, CriadoEm, AtualizadoEm)
            VALUES ($Id, $Nome, $Documento, $Telefone, $Email, $Ativo, $CriadoEm, $AtualizadoEm);
            """);
        Add(command, "$Id", fornecedor.Id);
        Add(command, "$Nome", fornecedor.Nome);
        Add(command, "$Documento", fornecedor.Documento);
        Add(command, "$Telefone", fornecedor.Telefone);
        Add(command, "$Email", fornecedor.Email);
        Add(command, "$Ativo", fornecedor.Ativo);
        Add(command, "$CriadoEm", fornecedor.CriadoEm);
        Add(command, "$AtualizadoEm", fornecedor.AtualizadoEm);
        command.ExecuteNonQuery();
    }

    private void SalvarProduto(SqliteConnection connection, SqliteTransaction transaction, Produto produto)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO Produtos (
                Id, Nome, CategoriaId, Sku, Descricao, ImagemUrl, ImagensExtrasJson,
                TamanhosJson, CoresJson, ModelosJson, VariacoesEstoqueJson, GuiaMedidas,
                PublicadoNaLoja, DestaqueLoja, OrdemLoja, NomeLoja, DescricaoLoja,
                PrecoLoja, ImagemLojaUrl, ImagensLojaExtrasJson, Preco, Custo,
                QuantidadeEmEstoque, Ativo, CriadoEm, AtualizadoEm)
            VALUES (
                $Id, $Nome, $CategoriaId, $Sku, $Descricao, $ImagemUrl, $ImagensExtrasJson,
                $TamanhosJson, $CoresJson, $ModelosJson, $VariacoesEstoqueJson, $GuiaMedidas,
                $PublicadoNaLoja, $DestaqueLoja, $OrdemLoja, $NomeLoja, $DescricaoLoja,
                $PrecoLoja, $ImagemLojaUrl, $ImagensLojaExtrasJson, $Preco, $Custo,
                $QuantidadeEmEstoque, $Ativo, $CriadoEm, $AtualizadoEm);
            """);
        Add(command, "$Id", produto.Id);
        Add(command, "$Nome", produto.Nome);
        Add(command, "$CategoriaId", produto.CategoriaId);
        Add(command, "$Sku", produto.Sku);
        Add(command, "$Descricao", produto.Descricao);
        Add(command, "$ImagemUrl", produto.ImagemUrl);
        Add(command, "$ImagensExtrasJson", SerializeJson(produto.ImagensExtras));
        Add(command, "$TamanhosJson", SerializeJson(produto.Tamanhos));
        Add(command, "$CoresJson", SerializeJson(produto.Cores));
        Add(command, "$ModelosJson", SerializeJson(produto.Modelos));
        Add(command, "$VariacoesEstoqueJson", SerializeJson(produto.VariacoesEstoque));
        Add(command, "$GuiaMedidas", produto.GuiaMedidas);
        Add(command, "$PublicadoNaLoja", produto.PublicadoNaLoja);
        Add(command, "$DestaqueLoja", produto.DestaqueLoja);
        Add(command, "$OrdemLoja", produto.OrdemLoja);
        Add(command, "$NomeLoja", produto.NomeLoja);
        Add(command, "$DescricaoLoja", produto.DescricaoLoja);
        Add(command, "$PrecoLoja", produto.PrecoLoja);
        Add(command, "$ImagemLojaUrl", produto.ImagemLojaUrl);
        Add(command, "$ImagensLojaExtrasJson", SerializeJson(produto.ImagensLojaExtras));
        Add(command, "$Preco", produto.Preco);
        Add(command, "$Custo", produto.Custo);
        Add(command, "$QuantidadeEmEstoque", produto.QuantidadeEmEstoque);
        Add(command, "$Ativo", produto.Ativo);
        Add(command, "$CriadoEm", produto.CriadoEm);
        Add(command, "$AtualizadoEm", produto.AtualizadoEm);
        command.ExecuteNonQuery();
    }

    private void SalvarVendaLoja(SqliteConnection connection, SqliteTransaction transaction, VendaLoja venda)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO VendasLoja (
                Id, FormaPagamento, ItensJson, Desconto, ValorRecebido,
                Observacao, Devolvida, DevolvidaEm, MotivoDevolucao, CriadaEm)
            VALUES (
                $Id, $FormaPagamento, $ItensJson, $Desconto, $ValorRecebido,
                $Observacao, $Devolvida, $DevolvidaEm, $MotivoDevolucao, $CriadaEm);
            """);
        Add(command, "$Id", venda.Id);
        Add(command, "$FormaPagamento", venda.FormaPagamento);
        Add(command, "$ItensJson", SerializeJson(venda.Itens));
        Add(command, "$Desconto", venda.Desconto);
        Add(command, "$ValorRecebido", venda.ValorRecebido);
        Add(command, "$Observacao", venda.Observacao);
        Add(command, "$Devolvida", venda.Devolvida);
        Add(command, "$DevolvidaEm", venda.DevolvidaEm);
        Add(command, "$MotivoDevolucao", venda.MotivoDevolucao);
        Add(command, "$CriadaEm", venda.CriadaEm);
        command.ExecuteNonQuery();
    }

    private void SalvarPedidoOnline(SqliteConnection connection, SqliteTransaction transaction, PedidoOnline pedido)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO PedidosOnline (
                Id, NomeCliente, EmailCliente, TelefoneCliente, DocumentoCliente, ClienteId, EnderecoEntrega,
                CepEntrega, RuaEntrega, NumeroEntrega, ComplementoEntrega, BairroEntrega,
                CidadeEntrega, EstadoEntrega, Observacao, CupomCodigo, Desconto, OpcaoEntregaId,
                EntregaNome, EntregaValor, EntregaPrazoMinimoDias, EntregaPrazoMaximoDias,
                CodigoRastreio, ObservacaoEntrega, RastreamentoAtualizadoEm,
                ReferenciaPagamento, ObservacaoPagamento, GatewayPagamentoProvedor, GatewayPagamentoId,
                GatewayPagamentoStatus, PixCopiaECola, PixQrCodeBase64, PixExpiraEm, UrlPagamento,
                PagamentoAtualizadoEm, PagamentoConfirmadoEm,
                FormaPagamento, Status, ItensJson, CriadoEm)
            VALUES (
                $Id, $NomeCliente, $EmailCliente, $TelefoneCliente, $DocumentoCliente, $ClienteId, $EnderecoEntrega,
                $CepEntrega, $RuaEntrega, $NumeroEntrega, $ComplementoEntrega, $BairroEntrega,
                $CidadeEntrega, $EstadoEntrega, $Observacao, $CupomCodigo, $Desconto, $OpcaoEntregaId,
                $EntregaNome, $EntregaValor, $EntregaPrazoMinimoDias, $EntregaPrazoMaximoDias,
                $CodigoRastreio, $ObservacaoEntrega, $RastreamentoAtualizadoEm,
                $ReferenciaPagamento, $ObservacaoPagamento, $GatewayPagamentoProvedor, $GatewayPagamentoId,
                $GatewayPagamentoStatus, $PixCopiaECola, $PixQrCodeBase64, $PixExpiraEm, $UrlPagamento,
                $PagamentoAtualizadoEm, $PagamentoConfirmadoEm,
                $FormaPagamento, $Status, $ItensJson, $CriadoEm);
            """);
        Add(command, "$Id", pedido.Id);
        Add(command, "$NomeCliente", pedido.NomeCliente);
        Add(command, "$EmailCliente", pedido.EmailCliente);
        Add(command, "$TelefoneCliente", pedido.TelefoneCliente);
        Add(command, "$DocumentoCliente", pedido.DocumentoCliente);
        Add(command, "$ClienteId", pedido.ClienteId);
        Add(command, "$EnderecoEntrega", pedido.EnderecoEntrega);
        Add(command, "$CepEntrega", pedido.CepEntrega);
        Add(command, "$RuaEntrega", pedido.RuaEntrega);
        Add(command, "$NumeroEntrega", pedido.NumeroEntrega);
        Add(command, "$ComplementoEntrega", pedido.ComplementoEntrega);
        Add(command, "$BairroEntrega", pedido.BairroEntrega);
        Add(command, "$CidadeEntrega", pedido.CidadeEntrega);
        Add(command, "$EstadoEntrega", pedido.EstadoEntrega);
        Add(command, "$Observacao", pedido.Observacao);
        Add(command, "$CupomCodigo", pedido.CupomCodigo);
        Add(command, "$Desconto", pedido.Desconto);
        Add(command, "$OpcaoEntregaId", pedido.OpcaoEntregaId);
        Add(command, "$EntregaNome", pedido.EntregaNome);
        Add(command, "$EntregaValor", pedido.EntregaValor);
        Add(command, "$EntregaPrazoMinimoDias", pedido.EntregaPrazoMinimoDias);
        Add(command, "$EntregaPrazoMaximoDias", pedido.EntregaPrazoMaximoDias);
        Add(command, "$CodigoRastreio", pedido.CodigoRastreio);
        Add(command, "$ObservacaoEntrega", pedido.ObservacaoEntrega);
        Add(command, "$RastreamentoAtualizadoEm", pedido.RastreamentoAtualizadoEm);
        Add(command, "$ReferenciaPagamento", pedido.ReferenciaPagamento);
        Add(command, "$ObservacaoPagamento", pedido.ObservacaoPagamento);
        Add(command, "$GatewayPagamentoProvedor", pedido.GatewayPagamentoProvedor);
        Add(command, "$GatewayPagamentoId", pedido.GatewayPagamentoId);
        Add(command, "$GatewayPagamentoStatus", pedido.GatewayPagamentoStatus);
        Add(command, "$PixCopiaECola", pedido.PixCopiaECola);
        Add(command, "$PixQrCodeBase64", pedido.PixQrCodeBase64);
        Add(command, "$PixExpiraEm", pedido.PixExpiraEm);
        Add(command, "$UrlPagamento", pedido.UrlPagamento);
        Add(command, "$PagamentoAtualizadoEm", pedido.PagamentoAtualizadoEm);
        Add(command, "$PagamentoConfirmadoEm", pedido.PagamentoConfirmadoEm);
        Add(command, "$FormaPagamento", pedido.FormaPagamento);
        Add(command, "$Status", pedido.Status);
        Add(command, "$ItensJson", SerializeJson(pedido.Itens));
        Add(command, "$CriadoEm", pedido.CriadoEm);
        command.ExecuteNonQuery();
    }

    private void SalvarMovimentacao(SqliteConnection connection, SqliteTransaction transaction, EstoqueMovimentacao movimentacao)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO EstoqueMovimentacoes (
                Id, ProdutoId, ProdutoNome, Tipo, Quantidade, EstoqueAposMovimento,
                Origem, VendaLojaId, PedidoOnlineId, FornecedorId, FornecedorNome,
                CustoUnitario, Documento, CriadaEm)
            VALUES (
                $Id, $ProdutoId, $ProdutoNome, $Tipo, $Quantidade, $EstoqueAposMovimento,
                $Origem, $VendaLojaId, $PedidoOnlineId, $FornecedorId, $FornecedorNome,
                $CustoUnitario, $Documento, $CriadaEm);
            """);
        Add(command, "$Id", movimentacao.Id);
        Add(command, "$ProdutoId", movimentacao.ProdutoId);
        Add(command, "$ProdutoNome", movimentacao.ProdutoNome);
        Add(command, "$Tipo", movimentacao.Tipo);
        Add(command, "$Quantidade", movimentacao.Quantidade);
        Add(command, "$EstoqueAposMovimento", movimentacao.EstoqueAposMovimento);
        Add(command, "$Origem", movimentacao.Origem);
        Add(command, "$VendaLojaId", movimentacao.VendaLojaId);
        Add(command, "$PedidoOnlineId", movimentacao.PedidoOnlineId);
        Add(command, "$FornecedorId", movimentacao.FornecedorId);
        Add(command, "$FornecedorNome", movimentacao.FornecedorNome);
        Add(command, "$CustoUnitario", movimentacao.CustoUnitario);
        Add(command, "$Documento", movimentacao.Documento);
        Add(command, "$CriadaEm", movimentacao.CriadaEm);
        command.ExecuteNonQuery();
    }

    private void SalvarAtividadePainel(SqliteConnection connection, SqliteTransaction transaction, AtividadePainel atividade)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO AtividadesPainel (Id, Usuario, Acao, Detalhe, CriadaEm)
            VALUES ($Id, $Usuario, $Acao, $Detalhe, $CriadaEm);
            """);
        Add(command, "$Id", atividade.Id);
        Add(command, "$Usuario", atividade.Usuario);
        Add(command, "$Acao", atividade.Acao);
        Add(command, "$Detalhe", atividade.Detalhe);
        Add(command, "$CriadaEm", atividade.CriadaEm);
        command.ExecuteNonQuery();
    }

    private void SalvarConfiguracaoLoja(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO LojaConfiguracao (
                Id, NomeCriadorSite, PoliticaPrivacidade, FreteValorPadrao,
                FreteGratisAcimaDe, PrazoMinimoDias, PrazoMaximoDias,
                MensagemFrete, MensagemLoginCliente, BannerEyebrow, BannerTitulo,
                BannerDescricao, BannerBotaoPrimario, BannerBotaoSecundario,
                BannerImagemUrl, PromocaoTopoTexto, CampanhaTitulo, CampanhaDescricao,
                CampanhaBotaoTexto, CampanhaImagemUrl, VitrineImagem1Url, VitrineImagem1Titulo,
                VitrineImagem2Url, VitrineImagem2Titulo, VitrineImagem3Url, VitrineImagem3Titulo,
                WhatsappLoja, InstagramLoja, EnderecoLoja, PixChave, PixNomeRecebedor,
                PixCidade, PixOnlineAtivo, CartaoOnlineAtivo, CheckoutCartaoNome,
                CheckoutCartaoUrl, MensagemPagamento, MensagemPagamentoCartao,
                EmailNotificacoesAtivo, EmailProvedor, EmailRemetente, EmailPedidosDestino,
                BrevoApiKey, SmtpHost, SmtpPorta, SmtpUsuario, SmtpSenha, SmtpSsl,
                BackupAutomaticoAtivo, BackupIntervaloHoras, BackupUltimoEm,
                GatewayPagamentoProvedor, GatewayPagamentoAtivo, GatewayPagamentoProducao,
                GatewayPagamentoPublicKey, GatewayPagamentoAccessToken,
                GatewayPagamentoWebhookSecret, GatewayPagamentoWebhookUrl,
                RazaoSocial, Cnpj, SiteUrlCanonica, PoliticaTrocaDevolucao,
                GoogleAnalyticsId, MetaPixelId, BackupEmailAtivo, BackupEmailDestino, AtualizadoEm)
            VALUES (
                1, $NomeCriadorSite, $PoliticaPrivacidade, $FreteValorPadrao,
                $FreteGratisAcimaDe, $PrazoMinimoDias, $PrazoMaximoDias,
                $MensagemFrete, $MensagemLoginCliente, $BannerEyebrow, $BannerTitulo,
                $BannerDescricao, $BannerBotaoPrimario, $BannerBotaoSecundario,
                $BannerImagemUrl, $PromocaoTopoTexto, $CampanhaTitulo, $CampanhaDescricao,
                $CampanhaBotaoTexto, $CampanhaImagemUrl, $VitrineImagem1Url, $VitrineImagem1Titulo,
                $VitrineImagem2Url, $VitrineImagem2Titulo, $VitrineImagem3Url, $VitrineImagem3Titulo,
                $WhatsappLoja, $InstagramLoja, $EnderecoLoja, $PixChave, $PixNomeRecebedor,
                $PixCidade, $PixOnlineAtivo, $CartaoOnlineAtivo, $CheckoutCartaoNome,
                $CheckoutCartaoUrl, $MensagemPagamento, $MensagemPagamentoCartao,
                $EmailNotificacoesAtivo, $EmailProvedor, $EmailRemetente, $EmailPedidosDestino,
                $BrevoApiKey, $SmtpHost, $SmtpPorta, $SmtpUsuario, $SmtpSenha, $SmtpSsl,
                $BackupAutomaticoAtivo, $BackupIntervaloHoras, $BackupUltimoEm,
                $GatewayPagamentoProvedor, $GatewayPagamentoAtivo, $GatewayPagamentoProducao,
                $GatewayPagamentoPublicKey, $GatewayPagamentoAccessToken,
                $GatewayPagamentoWebhookSecret, $GatewayPagamentoWebhookUrl,
                $RazaoSocial, $Cnpj, $SiteUrlCanonica, $PoliticaTrocaDevolucao,
                $GoogleAnalyticsId, $MetaPixelId, $BackupEmailAtivo, $BackupEmailDestino, $AtualizadoEm);
            """);
        Add(command, "$NomeCriadorSite", _configuracaoLoja.NomeCriadorSite);
        Add(command, "$PoliticaPrivacidade", _configuracaoLoja.PoliticaPrivacidade);
        Add(command, "$FreteValorPadrao", _configuracaoLoja.FreteValorPadrao);
        Add(command, "$FreteGratisAcimaDe", _configuracaoLoja.FreteGratisAcimaDe);
        Add(command, "$PrazoMinimoDias", _configuracaoLoja.PrazoMinimoDias);
        Add(command, "$PrazoMaximoDias", _configuracaoLoja.PrazoMaximoDias);
        Add(command, "$MensagemFrete", _configuracaoLoja.MensagemFrete);
        Add(command, "$MensagemLoginCliente", _configuracaoLoja.MensagemLoginCliente);
        Add(command, "$BannerEyebrow", _configuracaoLoja.BannerEyebrow);
        Add(command, "$BannerTitulo", _configuracaoLoja.BannerTitulo);
        Add(command, "$BannerDescricao", _configuracaoLoja.BannerDescricao);
        Add(command, "$BannerBotaoPrimario", _configuracaoLoja.BannerBotaoPrimario);
        Add(command, "$BannerBotaoSecundario", _configuracaoLoja.BannerBotaoSecundario);
        Add(command, "$BannerImagemUrl", _configuracaoLoja.BannerImagemUrl);
        Add(command, "$PromocaoTopoTexto", _configuracaoLoja.PromocaoTopoTexto);
        Add(command, "$CampanhaTitulo", _configuracaoLoja.CampanhaTitulo);
        Add(command, "$CampanhaDescricao", _configuracaoLoja.CampanhaDescricao);
        Add(command, "$CampanhaBotaoTexto", _configuracaoLoja.CampanhaBotaoTexto);
        Add(command, "$CampanhaImagemUrl", _configuracaoLoja.CampanhaImagemUrl);
        Add(command, "$VitrineImagem1Url", _configuracaoLoja.VitrineImagem1Url);
        Add(command, "$VitrineImagem1Titulo", _configuracaoLoja.VitrineImagem1Titulo);
        Add(command, "$VitrineImagem2Url", _configuracaoLoja.VitrineImagem2Url);
        Add(command, "$VitrineImagem2Titulo", _configuracaoLoja.VitrineImagem2Titulo);
        Add(command, "$VitrineImagem3Url", _configuracaoLoja.VitrineImagem3Url);
        Add(command, "$VitrineImagem3Titulo", _configuracaoLoja.VitrineImagem3Titulo);
        Add(command, "$WhatsappLoja", _configuracaoLoja.WhatsappLoja);
        Add(command, "$InstagramLoja", _configuracaoLoja.InstagramLoja);
        Add(command, "$EnderecoLoja", _configuracaoLoja.EnderecoLoja);
        Add(command, "$PixChave", _configuracaoLoja.PixChave);
        Add(command, "$PixNomeRecebedor", _configuracaoLoja.PixNomeRecebedor);
        Add(command, "$PixCidade", _configuracaoLoja.PixCidade);
        Add(command, "$PixOnlineAtivo", _configuracaoLoja.PixOnlineAtivo);
        Add(command, "$CartaoOnlineAtivo", _configuracaoLoja.CartaoOnlineAtivo);
        Add(command, "$CheckoutCartaoNome", _configuracaoLoja.CheckoutCartaoNome);
        Add(command, "$CheckoutCartaoUrl", _configuracaoLoja.CheckoutCartaoUrl);
        Add(command, "$MensagemPagamento", _configuracaoLoja.MensagemPagamento);
        Add(command, "$MensagemPagamentoCartao", _configuracaoLoja.MensagemPagamentoCartao);
        Add(command, "$EmailNotificacoesAtivo", _configuracaoLoja.EmailNotificacoesAtivo);
        Add(command, "$EmailProvedor", _configuracaoLoja.EmailProvedor);
        Add(command, "$EmailRemetente", _configuracaoLoja.EmailRemetente);
        Add(command, "$EmailPedidosDestino", _configuracaoLoja.EmailPedidosDestino);
        Add(command, "$BrevoApiKey", _configuracaoLoja.BrevoApiKey);
        Add(command, "$SmtpHost", _configuracaoLoja.SmtpHost);
        Add(command, "$SmtpPorta", _configuracaoLoja.SmtpPorta);
        Add(command, "$SmtpUsuario", _configuracaoLoja.SmtpUsuario);
        Add(command, "$SmtpSenha", _configuracaoLoja.SmtpSenha);
        Add(command, "$SmtpSsl", _configuracaoLoja.SmtpSsl);
        Add(command, "$BackupAutomaticoAtivo", _configuracaoLoja.BackupAutomaticoAtivo);
        Add(command, "$BackupIntervaloHoras", _configuracaoLoja.BackupIntervaloHoras);
        Add(command, "$BackupUltimoEm", _configuracaoLoja.BackupUltimoEm);
        Add(command, "$GatewayPagamentoProvedor", _configuracaoLoja.GatewayPagamentoProvedor);
        Add(command, "$GatewayPagamentoAtivo", _configuracaoLoja.GatewayPagamentoAtivo);
        Add(command, "$GatewayPagamentoProducao", _configuracaoLoja.GatewayPagamentoProducao);
        Add(command, "$GatewayPagamentoPublicKey", _configuracaoLoja.GatewayPagamentoPublicKey);
        Add(command, "$GatewayPagamentoAccessToken", _configuracaoLoja.GatewayPagamentoAccessToken);
        Add(command, "$GatewayPagamentoWebhookSecret", _configuracaoLoja.GatewayPagamentoWebhookSecret);
        Add(command, "$GatewayPagamentoWebhookUrl", _configuracaoLoja.GatewayPagamentoWebhookUrl);
        Add(command, "$RazaoSocial", _configuracaoLoja.RazaoSocial);
        Add(command, "$Cnpj", _configuracaoLoja.Cnpj);
        Add(command, "$SiteUrlCanonica", _configuracaoLoja.SiteUrlCanonica);
        Add(command, "$PoliticaTrocaDevolucao", _configuracaoLoja.PoliticaTrocaDevolucao);
        Add(command, "$GoogleAnalyticsId", _configuracaoLoja.GoogleAnalyticsId);
        Add(command, "$MetaPixelId", _configuracaoLoja.MetaPixelId);
        Add(command, "$BackupEmailAtivo", _configuracaoLoja.BackupEmailAtivo);
        Add(command, "$BackupEmailDestino", _configuracaoLoja.BackupEmailDestino);
        Add(command, "$AtualizadoEm", _configuracaoLoja.AtualizadoEm);
        command.ExecuteNonQuery();
    }

    private void SalvarCupom(SqliteConnection connection, SqliteTransaction transaction, CupomDesconto cupom)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO CuponsDesconto (
                Id, Codigo, Descricao, PercentualDesconto, ValorMinimoPedido,
                Ativo, ValidoAte, CriadoEm, AtualizadoEm)
            VALUES (
                $Id, $Codigo, $Descricao, $PercentualDesconto, $ValorMinimoPedido,
                $Ativo, $ValidoAte, $CriadoEm, $AtualizadoEm);
            """);
        Add(command, "$Id", cupom.Id);
        Add(command, "$Codigo", cupom.Codigo);
        Add(command, "$Descricao", cupom.Descricao);
        Add(command, "$PercentualDesconto", cupom.PercentualDesconto);
        Add(command, "$ValorMinimoPedido", cupom.ValorMinimoPedido);
        Add(command, "$Ativo", cupom.Ativo);
        Add(command, "$ValidoAte", cupom.ValidoAte);
        Add(command, "$CriadoEm", cupom.CriadoEm);
        Add(command, "$AtualizadoEm", cupom.AtualizadoEm);
        command.ExecuteNonQuery();
    }

    private void SalvarOpcaoEntrega(SqliteConnection connection, SqliteTransaction transaction, OpcaoEntrega opcao)
    {
        using var command = CreateCommand(connection, transaction, """
            INSERT INTO OpcoesEntrega (
                Id, Nome, Tipo, Descricao, Valor, FreteGratisAcimaDe,
                PrazoMinimoDias, PrazoMaximoDias, CepInicial, CepFinal,
                CidadesJson, BairrosJson, EstadosJson, Ativo, Ordem, CriadoEm, AtualizadoEm)
            VALUES (
                $Id, $Nome, $Tipo, $Descricao, $Valor, $FreteGratisAcimaDe,
                $PrazoMinimoDias, $PrazoMaximoDias, $CepInicial, $CepFinal,
                $CidadesJson, $BairrosJson, $EstadosJson, $Ativo, $Ordem, $CriadoEm, $AtualizadoEm);
            """);
        Add(command, "$Id", opcao.Id);
        Add(command, "$Nome", opcao.Nome);
        Add(command, "$Tipo", opcao.Tipo);
        Add(command, "$Descricao", opcao.Descricao);
        Add(command, "$Valor", opcao.Valor);
        Add(command, "$FreteGratisAcimaDe", opcao.FreteGratisAcimaDe);
        Add(command, "$PrazoMinimoDias", opcao.PrazoMinimoDias);
        Add(command, "$PrazoMaximoDias", opcao.PrazoMaximoDias);
        Add(command, "$CepInicial", opcao.CepInicial);
        Add(command, "$CepFinal", opcao.CepFinal);
        Add(command, "$CidadesJson", SerializeJson(opcao.Cidades));
        Add(command, "$BairrosJson", SerializeJson(opcao.Bairros));
        Add(command, "$EstadosJson", SerializeJson(opcao.Estados));
        Add(command, "$Ativo", opcao.Ativo);
        Add(command, "$Ordem", opcao.Ordem);
        Add(command, "$CriadoEm", opcao.CriadoEm);
        Add(command, "$AtualizadoEm", opcao.AtualizadoEm);
        command.ExecuteNonQuery();
    }

    private static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = CreateCommand(connection, transaction, sql);
        command.ExecuteNonQuery();
    }

    private static void GarantirColuna(SqliteConnection connection, string tabela, string coluna, string definicao)
    {
        using var command = CreateCommand(connection, null, $"PRAGMA table_info({tabela});");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), coluna, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        ExecuteNonQuery(connection, null, $"ALTER TABLE {tabela} ADD COLUMN {coluna} {definicao};");
    }

    private static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }

    private static void Add(SqliteCommand command, string name, object? value)
    {
        var dbValue = value switch
        {
            null => DBNull.Value,
            Guid guid => guid.ToString(),
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
            Enum enumValue => enumValue.ToString(),
            bool boolValue => boolValue ? 1 : 0,
            _ => value
        };

        command.Parameters.AddWithValue(name, dbValue);
    }

    private string SerializeJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, _jsonOptions);
    }

    private T DeserializeJson<T>(string json, T fallback)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static string ReadString(SqliteDataReader reader, string column)
    {
        return reader.GetString(reader.GetOrdinal(column));
    }

    private static string? ReadNullableString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static Guid ReadGuid(SqliteDataReader reader, string column)
    {
        return Guid.Parse(ReadString(reader, column));
    }

    private static Guid? ReadNullableGuid(SqliteDataReader reader, string column)
    {
        var value = ReadNullableString(reader, column);
        return string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);
    }

    private static int ReadInt(SqliteDataReader reader, string column)
    {
        return Convert.ToInt32(reader.GetInt64(reader.GetOrdinal(column)), CultureInfo.InvariantCulture);
    }

    private static int? ReadNullableInt(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToInt32(reader.GetInt64(ordinal), CultureInfo.InvariantCulture);
    }

    private static bool ReadBool(SqliteDataReader reader, string column)
    {
        return ReadInt(reader, column) == 1;
    }

    private static decimal ReadDecimal(SqliteDataReader reader, string column)
    {
        return decimal.Parse(ReadString(reader, column), CultureInfo.InvariantCulture);
    }

    private static decimal? ReadNullableDecimal(SqliteDataReader reader, string column)
    {
        var value = ReadNullableString(reader, column);
        return string.IsNullOrWhiteSpace(value) ? null : decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    private static DateTime ReadDateTime(SqliteDataReader reader, string column)
    {
        return DateTime.Parse(ReadString(reader, column), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static DateTime? ReadNullableDateTime(SqliteDataReader reader, string column)
    {
        var value = ReadNullableString(reader, column);
        return string.IsNullOrWhiteSpace(value)
            ? null
            : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static TEnum ReadEnum<TEnum>(SqliteDataReader reader, string column, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse(ReadString(reader, column), out TEnum value) ? value : fallback;
    }

    private static string CriarHashSenha(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SenhaSaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            SenhaIteracoes,
            HashAlgorithmName.SHA256,
            SenhaHashBytes);

        return $"PBKDF2-SHA256${SenhaIteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerificarSenha(string senha, string senhaHash)
    {
        var partes = senhaHash.Split('$');
        if (partes.Length != 4 ||
            !string.Equals(partes[0], "PBKDF2-SHA256", StringComparison.Ordinal) ||
            !int.TryParse(partes[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iteracoes))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var hashSalvo = Convert.FromBase64String(partes[3]);
            var hashInformado = Rfc2898DeriveBytes.Pbkdf2(
                senha,
                salt,
                iteracoes,
                HashAlgorithmName.SHA256,
                hashSalvo.Length);

            return CryptographicOperations.FixedTimeEquals(hashSalvo, hashInformado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string? ValidarProduto(string nome, Guid categoriaId, decimal preco, decimal custo, int quantidadeInicial)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return "Informe o nome do produto.";
        }

        if (preco <= 0)
        {
            return "O preco deve ser maior que zero.";
        }

        if (custo < 0)
        {
            return "O custo nao pode ser negativo.";
        }

        if (quantidadeInicial < 0)
        {
            return "A quantidade inicial nao pode ser negativa.";
        }

        lock (_sync)
        {
            return _categorias.ContainsKey(categoriaId) ? null : "Categoria nao encontrada.";
        }
    }

    private static string? ValidarClientePedido(RegistrarPedidoOnlineRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NomeCliente))
        {
            return "Informe o nome do cliente.";
        }

        if (string.IsNullOrWhiteSpace(request.EmailCliente) || !request.EmailCliente.Contains('@', StringComparison.Ordinal))
        {
            return "Informe um e-mail valido.";
        }

        return null;
    }

    private static string? ValidarCupom(CupomDescontoRequest request, out string codigo)
    {
        codigo = NormalizarCodigoCupom(request.Codigo);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return "Informe o codigo do cupom.";
        }

        if (codigo.Length < 3 || codigo.Length > 24)
        {
            return "O codigo do cupom deve ter entre 3 e 24 caracteres.";
        }

        if (codigo.Any(caractere => !char.IsLetterOrDigit(caractere) && caractere is not '-' and not '_'))
        {
            return "Use apenas letras, numeros, hifen ou underline no codigo.";
        }

        if (request.PercentualDesconto <= 0 || request.PercentualDesconto > 80)
        {
            return "O percentual do cupom deve ficar entre 0,01% e 80%.";
        }

        if (request.ValorMinimoPedido < 0)
        {
            return "O valor minimo do pedido nao pode ser negativo.";
        }

        if (request.Ativo && request.ValidoAte.HasValue && request.ValidoAte.Value.Date < DateTime.UtcNow.Date)
        {
            return "A validade de um cupom ativo precisa ser hoje ou uma data futura.";
        }

        return null;
    }

    private static string NormalizarCodigoCupom(string? codigo)
    {
        return string.IsNullOrWhiteSpace(codigo) ? "" : codigo.Trim().ToUpperInvariant();
    }

    private static bool CupomDisponivelNaLoja(CupomDesconto cupom)
    {
        return cupom.Ativo &&
            (!cupom.ValidoAte.HasValue || cupom.ValidoAte.Value.Date >= DateTime.UtcNow.Date);
    }

    private static string? ValidarOpcaoEntrega(OpcaoEntregaRequest request, out string tipo, out string? cepInicial, out string? cepFinal)
    {
        tipo = NormalizarTextoOpcional(request.Tipo) ?? "";
        cepInicial = NormalizarCep(request.CepInicial);
        cepFinal = NormalizarCep(request.CepFinal);

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return "Informe o nome da opcao de entrega.";
        }

        var tiposPermitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Retirada",
            "EntregaLocal",
            "Correios",
            "Transportadora",
            "Personalizada"
        };

        var tipoInformado = tipo;
        var tipoCanonico = tiposPermitidos.FirstOrDefault(item => string.Equals(item, tipoInformado, StringComparison.OrdinalIgnoreCase));
        if (tipoCanonico is null)
        {
            return "Escolha um tipo de entrega valido.";
        }

        tipo = tipoCanonico;

        if (request.Valor < 0 || request.FreteGratisAcimaDe < 0)
        {
            return "Os valores de entrega nao podem ser negativos.";
        }

        if (request.PrazoMinimoDias < 0 || request.PrazoMaximoDias < request.PrazoMinimoDias)
        {
            return "Informe um prazo de entrega valido.";
        }

        if (request.Ordem < 0)
        {
            return "A ordem nao pode ser negativa.";
        }

        if (TemCepInvalido(request.CepInicial) || TemCepInvalido(request.CepFinal))
        {
            return "Informe CEP inicial e final com 8 numeros.";
        }

        if (cepInicial is not null && cepFinal is not null &&
            string.CompareOrdinal(cepInicial, cepFinal) > 0)
        {
            return "O CEP inicial nao pode ser maior que o CEP final.";
        }

        return null;
    }

    private Resultado<(Guid? OpcaoEntregaId, string Nome, decimal Valor, int? PrazoMinimoDias, int? PrazoMaximoDias, string? Tipo)> CalcularEntregaPedido(RegistrarPedidoOnlineRequest request, decimal totalProdutos)
    {
        if (!request.OpcaoEntregaId.HasValue)
        {
            return Resultado<(Guid? OpcaoEntregaId, string Nome, decimal Valor, int? PrazoMinimoDias, int? PrazoMaximoDias, string? Tipo)>.Ok(
                (null, "Entrega a combinar", 0, null, null, null));
        }

        if (!_opcoesEntrega.TryGetValue(request.OpcaoEntregaId.Value, out var opcao) || !opcao.Ativo)
        {
            return Resultado<(Guid? OpcaoEntregaId, string Nome, decimal Valor, int? PrazoMinimoDias, int? PrazoMaximoDias, string? Tipo)>.Falha("Opcao de entrega indisponivel.");
        }

        if (!OpcaoEntregaAtendeEndereco(opcao, request))
        {
            return Resultado<(Guid? OpcaoEntregaId, string Nome, decimal Valor, int? PrazoMinimoDias, int? PrazoMaximoDias, string? Tipo)>.Falha("Essa opcao de entrega nao atende o endereco informado.");
        }

        var valor = CalcularValorEntrega(opcao, totalProdutos);
        return Resultado<(Guid? OpcaoEntregaId, string Nome, decimal Valor, int? PrazoMinimoDias, int? PrazoMaximoDias, string? Tipo)>.Ok(
            (opcao.Id, opcao.Nome, valor, opcao.PrazoMinimoDias, opcao.PrazoMaximoDias, opcao.Tipo));
    }

    private static decimal CalcularValorEntrega(OpcaoEntrega opcao, decimal totalProdutos)
    {
        if (string.Equals(opcao.Tipo, "Retirada", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return opcao.FreteGratisAcimaDe > 0 && totalProdutos >= opcao.FreteGratisAcimaDe
            ? 0
            : opcao.Valor;
    }

    private static bool OpcaoEntregaAtendeEndereco(OpcaoEntrega opcao, RegistrarPedidoOnlineRequest request)
    {
        var cep = NormalizarCep(request.CepEntrega);
        if ((opcao.CepInicial is not null || opcao.CepFinal is not null) &&
            (cep is null ||
                (opcao.CepInicial is not null && string.CompareOrdinal(cep, opcao.CepInicial) < 0) ||
                (opcao.CepFinal is not null && string.CompareOrdinal(cep, opcao.CepFinal) > 0)))
        {
            return false;
        }

        return ListaAtendeTexto(opcao.Cidades, request.CidadeEntrega) &&
            ListaAtendeTexto(opcao.Bairros, request.BairroEntrega) &&
            ListaAtendeTexto(opcao.Estados, request.EstadoEntrega);
    }

    private static bool ListaAtendeTexto(IReadOnlyCollection<string> permitidos, string? valor)
    {
        if (permitidos.Count == 0)
        {
            return true;
        }

        var normalizado = NormalizarComparacao(valor);
        return !string.IsNullOrWhiteSpace(normalizado) &&
            permitidos.Any(item => string.Equals(NormalizarComparacao(item), normalizado, StringComparison.OrdinalIgnoreCase));
    }

    private Resultado<(string? CupomCodigo, decimal Desconto)> CalcularCupomPedido(string? cupomInformado, decimal subtotal)
    {
        var cupomCodigo = NormalizarCodigoCupom(cupomInformado);
        if (string.IsNullOrWhiteSpace(cupomCodigo))
        {
            return Resultado<(string? CupomCodigo, decimal Desconto)>.Ok((null, 0));
        }

        var cupom = _cupons.Values.FirstOrDefault(item => string.Equals(item.Codigo, cupomCodigo, StringComparison.OrdinalIgnoreCase));
        if (cupom is null)
        {
            return Resultado<(string? CupomCodigo, decimal Desconto)>.Falha("Cupom invalido.");
        }

        if (!CupomDisponivelNaLoja(cupom))
        {
            return Resultado<(string? CupomCodigo, decimal Desconto)>.Falha("Esse cupom nao esta disponivel.");
        }

        if (subtotal < cupom.ValorMinimoPedido)
        {
            return Resultado<(string? CupomCodigo, decimal Desconto)>.Falha($"O cupom {cupom.Codigo} vale para compras acima de {cupom.ValorMinimoPedido.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}.");
        }

        var desconto = Math.Round(subtotal * (cupom.PercentualDesconto / 100), 2, MidpointRounding.AwayFromZero);
        return Resultado<(string? CupomCodigo, decimal Desconto)>.Ok((cupom.Codigo, desconto));
    }

    private static bool EnderecoPedidoInformado(RegistrarPedidoOnlineRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.EnderecoEntrega))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(request.RuaEntrega) &&
            !string.IsNullOrWhiteSpace(request.NumeroEntrega) &&
            !string.IsNullOrWhiteSpace(request.BairroEntrega) &&
            !string.IsNullOrWhiteSpace(request.CidadeEntrega) &&
            !string.IsNullOrWhiteSpace(request.EstadoEntrega);
    }

    private static string CriarEnderecoEntrega(RegistrarPedidoOnlineRequest request)
    {
        var enderecoUnico = NormalizarTextoOpcional(request.EnderecoEntrega);
        if (enderecoUnico is not null)
        {
            return enderecoUnico;
        }

        var partes = new[]
        {
            NormalizarTextoOpcional(request.RuaEntrega),
            NormalizarTextoOpcional(request.NumeroEntrega),
            NormalizarTextoOpcional(request.ComplementoEntrega),
            NormalizarTextoOpcional(request.BairroEntrega),
            NormalizarTextoOpcional(request.CidadeEntrega),
            NormalizarTextoOpcional(request.EstadoEntrega)?.ToUpperInvariant(),
            NormalizarTextoOpcional(request.CepEntrega)
        };

        return string.Join(", ", partes.Where(parte => parte is not null));
    }

    private static string? ValidarVariacaoPedido(Produto produto, ItemPedidoOnlineRequest item)
    {
        var tamanho = ValidarOpcaoProduto(produto, produto.Tamanhos, item.Tamanho, "tamanho");
        if (tamanho is not null)
        {
            return tamanho;
        }

        var cor = ValidarOpcaoProduto(produto, produto.Cores, item.Cor, "cor");
        if (cor is not null)
        {
            return cor;
        }

        return ValidarOpcaoProduto(produto, produto.Modelos, item.Modelo, "modelo");
    }

    private static string? ValidarVariacaoVenda(Produto produto, ItemVendaLojaRequest item)
    {
        var tamanho = ValidarOpcaoProduto(produto, produto.Tamanhos, item.Tamanho, "tamanho");
        if (tamanho is not null)
        {
            return tamanho;
        }

        var cor = ValidarOpcaoProduto(produto, produto.Cores, item.Cor, "cor");
        if (cor is not null)
        {
            return cor;
        }

        return ValidarOpcaoProduto(produto, produto.Modelos, item.Modelo, "modelo");
    }

    private static string? ValidarVariacaoEntrada(Produto produto, string? tamanho, string? cor, string? modelo)
    {
        var tamanhoErro = ValidarOpcaoEntrada(produto.Tamanhos, tamanho, "tamanho");
        if (tamanhoErro is not null)
        {
            return tamanhoErro;
        }

        var corErro = ValidarOpcaoEntrada(produto.Cores, cor, "cor");
        if (corErro is not null)
        {
            return corErro;
        }

        return ValidarOpcaoEntrada(produto.Modelos, modelo, "modelo");
    }

    private static string? ValidarOpcaoEntrada(IReadOnlyCollection<string> opcoes, string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor) || opcoes.Count == 0)
        {
            return null;
        }

        return opcoes.Any(opcao => string.Equals(opcao, valor, StringComparison.OrdinalIgnoreCase))
            ? null
            : $"{campo} invalido para esse produto.";
    }

    private static string? ValidarOpcaoProduto(Produto produto, IReadOnlyCollection<string> opcoes, string? valor, string campo)
    {
        if (opcoes.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(valor))
        {
            return $"Escolha {campo} para {ObterNomeProdutoLoja(produto)}.";
        }

        return opcoes.Any(opcao => string.Equals(opcao, valor, StringComparison.OrdinalIgnoreCase))
            ? null
            : $"{campo} invalido para {ObterNomeProdutoLoja(produto)}.";
    }

    private static int ObterEstoqueDisponivel(Produto produto, string? tamanho, string? cor, string? modelo)
    {
        if (produto.VariacoesEstoque.Count == 0)
        {
            return produto.QuantidadeEmEstoque;
        }

        return EncontrarVariacaoEstoque(produto, tamanho, cor, modelo)?.Quantidade ?? 0;
    }

    private static ProdutoVariacaoEstoque? EncontrarVariacaoEstoque(Produto produto, string? tamanho, string? cor, string? modelo)
    {
        return produto.VariacoesEstoque.FirstOrDefault(variacao =>
            string.Equals(NormalizarComparacao(variacao.Tamanho), NormalizarComparacao(tamanho), StringComparison.Ordinal) &&
            string.Equals(NormalizarComparacao(variacao.Cor), NormalizarComparacao(cor), StringComparison.Ordinal) &&
            string.Equals(NormalizarComparacao(variacao.Modelo), NormalizarComparacao(modelo), StringComparison.Ordinal));
    }

    private static bool VariacaoIgual(string? esquerda, string? direita)
    {
        return string.Equals(NormalizarComparacao(esquerda), NormalizarComparacao(direita), StringComparison.Ordinal);
    }

    private static string CriarChaveItemVenda(Guid produtoId, string? tamanho, string? cor, string? modelo)
    {
        return string.Join("|",
            produtoId,
            NormalizarComparacao(tamanho),
            NormalizarComparacao(cor),
            NormalizarComparacao(modelo));
    }

    private static void BaixarEstoqueProduto(Produto produto, string? tamanho, string? cor, string? modelo, int quantidade)
    {
        if (produto.VariacoesEstoque.Count > 0)
        {
            var variacao = EncontrarVariacaoEstoque(produto, tamanho, cor, modelo);
            if (variacao is not null)
            {
                variacao.Quantidade = Math.Max(0, variacao.Quantidade - quantidade);
            }
        }

        produto.QuantidadeEmEstoque = Math.Max(0, produto.QuantidadeEmEstoque - quantidade);
    }

    private static void AdicionarEstoqueProduto(Produto produto, string? tamanho, string? cor, string? modelo, int quantidade)
    {
        if (!string.IsNullOrWhiteSpace(tamanho) || !string.IsNullOrWhiteSpace(cor) || !string.IsNullOrWhiteSpace(modelo))
        {
            var variacao = EncontrarVariacaoEstoque(produto, tamanho, cor, modelo);
            if (variacao is null)
            {
                variacao = new ProdutoVariacaoEstoque
                {
                    Tamanho = tamanho,
                    Cor = cor,
                    Modelo = modelo
                };
                produto.VariacoesEstoque.Add(variacao);
            }

            variacao.Quantidade += quantidade;
        }

        produto.QuantidadeEmEstoque += quantidade;
    }

    private static void DevolverEstoqueProduto(Produto produto, string? tamanho, string? cor, string? modelo, int quantidade)
    {
        if (produto.VariacoesEstoque.Count > 0)
        {
            var variacao = EncontrarVariacaoEstoque(produto, tamanho, cor, modelo);
            if (variacao is not null)
            {
                variacao.Quantidade += quantidade;
            }
        }

        produto.QuantidadeEmEstoque += quantidade;
    }

    private static string? CriarTextoVariacao(string? tamanho, string? cor, string? modelo)
    {
        var partes = new[]
        {
            string.IsNullOrWhiteSpace(tamanho) ? null : $"Tam. {tamanho}",
            string.IsNullOrWhiteSpace(cor) ? null : $"Cor {cor}",
            string.IsNullOrWhiteSpace(modelo) ? null : $"Modelo {modelo}"
        }.Where(parte => parte is not null);

        var texto = string.Join(" / ", partes);
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }

    private string? ValidarProdutosDoPedido(PedidoOnline pedido)
    {
        foreach (var item in pedido.Itens)
        {
            if (!_produtos.ContainsKey(item.ProdutoId))
            {
                return $"Produto {item.ProdutoNome} nao encontrado para atualizar o estoque.";
            }
        }

        return null;
    }

    private string? ValidarEstoqueParaReativarPedido(PedidoOnline pedido)
    {
        foreach (var item in pedido.Itens)
        {
            if (!_produtos.TryGetValue(item.ProdutoId, out var produto))
            {
                return $"Produto {item.ProdutoNome} nao encontrado para atualizar o estoque.";
            }

            var disponivel = ObterEstoqueDisponivel(produto, item.Tamanho, item.Cor, item.Modelo);
            if (disponivel < item.Quantidade)
            {
                return $"Estoque insuficiente para reativar {item.ProdutoNome}. Disponivel: {disponivel}.";
            }
        }

        return null;
    }

    private static UsuarioPainelResponse CriarUsuarioPainelResponse(UsuarioPainel usuario)
    {
        return new UsuarioPainelResponse(
            usuario.Id,
            usuario.Usuario,
            usuario.NomeExibicao,
            usuario.Perfil,
            usuario.Ativo,
            usuario.CriadoEm,
            usuario.AtualizadoEm);
    }

    private ProdutoResponse CriarProdutoResponse(Produto produto)
    {
        var categoria = _categorias.TryGetValue(produto.CategoriaId, out var encontrada)
            ? encontrada.Nome
            : "Sem categoria";

        return new ProdutoResponse(
            produto.Id,
            produto.Nome,
            produto.CategoriaId,
            categoria,
            produto.Sku,
            produto.Preco,
            produto.Custo,
            produto.QuantidadeEmEstoque,
            produto.Ativo,
            produto.Descricao,
            produto.ImagemUrl,
            produto.ImagensExtras,
            produto.Tamanhos,
            produto.Cores,
            produto.Modelos,
            produto.VariacoesEstoque.Select(CriarVariacaoEstoqueResponse).ToList(),
            produto.GuiaMedidas,
            produto.PublicadoNaLoja,
            produto.DestaqueLoja,
            produto.OrdemLoja,
            produto.NomeLoja,
            produto.DescricaoLoja,
            produto.PrecoLoja,
            produto.ImagemLojaUrl,
            produto.ImagensLojaExtras,
            produto.CriadoEm,
            produto.AtualizadoEm);
    }

    private ProdutoLojaResponse CriarProdutoLojaResponse(Produto produto)
    {
        var categoria = _categorias.TryGetValue(produto.CategoriaId, out var encontrada)
            ? encontrada.Nome
            : "Sem categoria";

        return new ProdutoLojaResponse(
            produto.Id,
            ObterNomeProdutoLoja(produto),
            produto.CategoriaId,
            categoria,
            produto.Sku,
            ObterPrecoProdutoLoja(produto),
            produto.QuantidadeEmEstoque,
            produto.Ativo,
            produto.DescricaoLoja ?? produto.Descricao,
            produto.ImagemLojaUrl ?? produto.ImagemUrl,
            produto.ImagensLojaExtras.Count > 0 ? produto.ImagensLojaExtras : produto.ImagensExtras,
            produto.Tamanhos,
            produto.Cores,
            produto.Modelos,
            produto.VariacoesEstoque.Select(CriarVariacaoEstoqueResponse).ToList(),
            produto.GuiaMedidas,
            produto.DestaqueLoja,
            produto.OrdemLoja);
    }

    private LojaConfiguracaoResponse CriarLojaConfiguracaoResponse()
    {
        return new LojaConfiguracaoResponse(
            _configuracaoLoja.NomeCriadorSite,
            _configuracaoLoja.PoliticaPrivacidade,
            _configuracaoLoja.FreteValorPadrao,
            _configuracaoLoja.FreteGratisAcimaDe,
            _configuracaoLoja.PrazoMinimoDias,
            _configuracaoLoja.PrazoMaximoDias,
            _configuracaoLoja.MensagemFrete,
            _configuracaoLoja.MensagemLoginCliente,
            _configuracaoLoja.BannerEyebrow,
            _configuracaoLoja.BannerTitulo,
            _configuracaoLoja.BannerDescricao,
            _configuracaoLoja.BannerBotaoPrimario,
            _configuracaoLoja.BannerBotaoSecundario,
            _configuracaoLoja.BannerImagemUrl,
            _configuracaoLoja.PromocaoTopoTexto,
            _configuracaoLoja.CampanhaTitulo,
            _configuracaoLoja.CampanhaDescricao,
            _configuracaoLoja.CampanhaBotaoTexto,
            _configuracaoLoja.CampanhaImagemUrl,
            _configuracaoLoja.VitrineImagem1Url,
            _configuracaoLoja.VitrineImagem1Titulo,
            _configuracaoLoja.VitrineImagem2Url,
            _configuracaoLoja.VitrineImagem2Titulo,
            _configuracaoLoja.VitrineImagem3Url,
            _configuracaoLoja.VitrineImagem3Titulo,
            _configuracaoLoja.WhatsappLoja,
            _configuracaoLoja.InstagramLoja,
            _configuracaoLoja.EnderecoLoja,
            _configuracaoLoja.PixChave,
            _configuracaoLoja.PixNomeRecebedor,
            _configuracaoLoja.PixCidade,
            _configuracaoLoja.PixOnlineAtivo,
            _configuracaoLoja.CartaoOnlineAtivo,
            _configuracaoLoja.CheckoutCartaoNome,
            _configuracaoLoja.CheckoutCartaoUrl,
            _configuracaoLoja.MensagemPagamento,
            _configuracaoLoja.MensagemPagamentoCartao,
            _configuracaoLoja.EmailNotificacoesAtivo,
            _configuracaoLoja.EmailProvedor,
            _configuracaoLoja.EmailRemetente,
            _configuracaoLoja.EmailPedidosDestino,
            !string.IsNullOrWhiteSpace(_configuracaoLoja.BrevoApiKey),
            _configuracaoLoja.SmtpHost,
            _configuracaoLoja.SmtpPorta,
            _configuracaoLoja.SmtpUsuario,
            _configuracaoLoja.SmtpSsl,
            _configuracaoLoja.BackupAutomaticoAtivo,
            _configuracaoLoja.BackupIntervaloHoras,
            _configuracaoLoja.BackupUltimoEm,
            _configuracaoLoja.GatewayPagamentoProvedor,
            _configuracaoLoja.GatewayPagamentoAtivo,
            _configuracaoLoja.GatewayPagamentoProducao,
            _configuracaoLoja.GatewayPagamentoPublicKey,
            !string.IsNullOrWhiteSpace(_configuracaoLoja.GatewayPagamentoAccessToken),
            !string.IsNullOrWhiteSpace(_configuracaoLoja.GatewayPagamentoWebhookSecret),
            _configuracaoLoja.GatewayPagamentoWebhookUrl,
            _configuracaoLoja.RazaoSocial,
            _configuracaoLoja.Cnpj,
            _configuracaoLoja.SiteUrlCanonica,
            _configuracaoLoja.PoliticaTrocaDevolucao,
            _configuracaoLoja.GoogleAnalyticsId,
            _configuracaoLoja.MetaPixelId,
            _configuracaoLoja.BackupEmailAtivo,
            _configuracaoLoja.BackupEmailDestino,
            _configuracaoLoja.AtualizadoEm);
    }

    private static ProdutoVariacaoEstoqueResponse CriarVariacaoEstoqueResponse(ProdutoVariacaoEstoque variacao)
    {
        return new ProdutoVariacaoEstoqueResponse(
            variacao.Tamanho,
            variacao.Cor,
            variacao.Modelo,
            variacao.Quantidade);
    }

    private static ClienteResponse CriarClienteResponse(Cliente cliente)
    {
        return new ClienteResponse(
            cliente.Id,
            cliente.Nome,
            cliente.Email,
            cliente.Telefone,
            cliente.CriadoEm,
            cliente.AtualizadoEm);
    }

    private static CupomDescontoResponse CriarCupomDescontoResponse(CupomDesconto cupom)
    {
        return new CupomDescontoResponse(
            cupom.Id,
            cupom.Codigo,
            cupom.Descricao,
            cupom.PercentualDesconto,
            cupom.ValorMinimoPedido,
            cupom.Ativo,
            CupomDisponivelNaLoja(cupom),
            cupom.ValidoAte,
            cupom.CriadoEm,
            cupom.AtualizadoEm);
    }

    private static OpcaoEntregaResponse CriarOpcaoEntregaResponse(OpcaoEntrega opcao)
    {
        return new OpcaoEntregaResponse(
            opcao.Id,
            opcao.Nome,
            opcao.Tipo,
            opcao.Descricao,
            opcao.Valor,
            opcao.FreteGratisAcimaDe,
            opcao.PrazoMinimoDias,
            opcao.PrazoMaximoDias,
            opcao.CepInicial,
            opcao.CepFinal,
            opcao.Cidades,
            opcao.Bairros,
            opcao.Estados,
            opcao.Ativo,
            opcao.Ativo,
            opcao.Ordem,
            opcao.CriadoEm,
            opcao.AtualizadoEm);
    }

    private static PedidoClienteResponse CriarPedidoClienteResponse(PedidoOnline pedido)
    {
        return new PedidoClienteResponse(
            pedido.Id,
            pedido.NomeCliente,
            pedido.EmailCliente,
            pedido.TelefoneCliente,
            pedido.DocumentoCliente,
            pedido.FormaPagamento,
            pedido.Status,
            pedido.EnderecoEntrega,
            pedido.CepEntrega,
            pedido.RuaEntrega,
            pedido.NumeroEntrega,
            pedido.ComplementoEntrega,
            pedido.BairroEntrega,
            pedido.CidadeEntrega,
            pedido.EstadoEntrega,
            pedido.Observacao,
            pedido.CupomCodigo,
            pedido.TotalBruto,
            pedido.Desconto,
            pedido.OpcaoEntregaId,
            pedido.EntregaNome,
            pedido.EntregaValor,
            pedido.EntregaPrazoMinimoDias,
            pedido.EntregaPrazoMaximoDias,
            pedido.CodigoRastreio,
            pedido.ObservacaoEntrega,
            pedido.RastreamentoAtualizadoEm,
            pedido.ReferenciaPagamento,
            pedido.ObservacaoPagamento,
            pedido.GatewayPagamentoProvedor,
            pedido.GatewayPagamentoId,
            pedido.GatewayPagamentoStatus,
            pedido.PixCopiaECola,
            pedido.PixQrCodeBase64,
            pedido.PixExpiraEm,
            pedido.UrlPagamento,
            pedido.PagamentoAtualizadoEm,
            pedido.PagamentoConfirmadoEm,
            pedido.Total,
            pedido.CriadoEm,
            pedido.Itens
                .Select(item => new ItemPedidoClienteResponse(
                    item.ProdutoId,
                    item.ProdutoNome,
                    item.Tamanho,
                    item.Cor,
                    item.Modelo,
                    item.Quantidade,
                    item.PrecoUnitario,
                    item.Subtotal))
                .ToList());
    }

    private void TentarEnviarEmailPedido(PedidoOnline pedido, string assunto, string corpo)
    {
        if (!EmailConfigurado())
        {
            return;
        }

        try
        {
            var destinos = _configuracaoLoja.EmailPedidosDestino.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            EnviarEmail(destinos, assunto, corpo, pedido.EmailCliente);
        }
        catch
        {
            // O e-mail nao pode impedir a venda ou a atualizacao do pedido.
        }
    }

    private string? ValidarConfiguracaoEmail()
    {
        if (string.IsNullOrWhiteSpace(_configuracaoLoja.EmailRemetente) ||
            string.IsNullOrWhiteSpace(_configuracaoLoja.EmailPedidosDestino))
        {
            return "Configure remetente e e-mail dos pedidos antes de testar.";
        }

        if (_configuracaoLoja.EmailProvedor == "Brevo")
        {
            return string.IsNullOrWhiteSpace(_configuracaoLoja.BrevoApiKey)
                ? "Informe a API key da Brevo antes de testar."
                : null;
        }

        if (string.IsNullOrWhiteSpace(_configuracaoLoja.SmtpHost))
        {
            return "Configure o servidor SMTP antes de testar.";
        }

        if (!string.IsNullOrWhiteSpace(_configuracaoLoja.SmtpUsuario) &&
            string.IsNullOrWhiteSpace(_configuracaoLoja.SmtpSenha))
        {
            return "Informe a senha SMTP. No Gmail use a senha de app, nao a senha normal da conta.";
        }

        return null;
    }

    private bool EmailConfigurado()
    {
        return _configuracaoLoja.EmailNotificacoesAtivo &&
            ValidarConfiguracaoEmail() is null;
    }

    private void TentarEnviarEmailSimples(string destino, string assunto, string corpo)
    {
        if (!EmailConfigurado() || !EmailValido(destino))
        {
            return;
        }

        try
        {
            EnviarEmail([destino], assunto, corpo);
        }
        catch
        {
            // Recuperacao de senha nao deve expor falha SMTP para o cliente.
        }
    }

    private static string CriarResumoPedidoEmail(PedidoOnline pedido)
    {
        var itens = pedido.Itens
            .Select(item =>
            {
                var variacao = CriarTextoVariacao(item.Tamanho, item.Cor, item.Modelo);
                return $"- {item.Quantidade}x {item.ProdutoNome}{(variacao is null ? "" : $" ({variacao})")}: {item.Subtotal.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}";
            });

        return string.Join(Environment.NewLine, new[]
        {
            $"Pedido: {pedido.Id.ToString()[..8].ToUpperInvariant()}",
            $"Status: {pedido.Status}",
            $"Cliente: {pedido.NomeCliente}",
            $"E-mail: {pedido.EmailCliente}",
            string.IsNullOrWhiteSpace(pedido.TelefoneCliente) ? null : $"Telefone: {pedido.TelefoneCliente}",
            $"Pagamento: {pedido.FormaPagamento}",
            $"Total: {pedido.Total.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}",
            "",
            "Itens:",
            string.Join(Environment.NewLine, itens),
            "",
            $"Entrega: {pedido.EnderecoEntrega}",
            string.IsNullOrWhiteSpace(pedido.Observacao) ? null : $"Observacao: {pedido.Observacao}"
        }.Where(linha => linha is not null));
    }

    private static string ObterNomeProdutoLoja(Produto produto)
    {
        return string.IsNullOrWhiteSpace(produto.NomeLoja) ? produto.Nome : produto.NomeLoja;
    }

    private static decimal ObterPrecoProdutoLoja(Produto produto)
    {
        return produto.PrecoLoja ?? produto.Preco;
    }

    private decimal ObterCustoTotalItem(Guid produtoId, decimal custoUnitario, int quantidade)
    {
        var custo = custoUnitario > 0
            ? custoUnitario
            : _produtos.TryGetValue(produtoId, out var produto) ? produto.Custo : 0;

        return custo * quantidade;
    }

    private decimal ObterCustoProduto(Guid produtoId)
    {
        return _produtos.TryGetValue(produtoId, out var produto) ? produto.Custo : 0;
    }

    private void RegistrarMovimentacao(
        Produto produto,
        TipoMovimentacaoEstoque tipo,
        int quantidade,
        string origem,
        Guid? vendaLojaId,
        Guid? pedidoOnlineId = null,
        Guid? fornecedorId = null,
        string? fornecedorNome = null,
        decimal? custoUnitario = null,
        string? documento = null)
    {
        _movimentacoes.Add(new EstoqueMovimentacao
        {
            ProdutoId = produto.Id,
            ProdutoNome = produto.Nome,
            Tipo = tipo,
            Quantidade = quantidade,
            EstoqueAposMovimento = produto.QuantidadeEmEstoque,
            Origem = origem,
            VendaLojaId = vendaLojaId,
            PedidoOnlineId = pedidoOnlineId,
            FornecedorId = fornecedorId,
            FornecedorNome = fornecedorNome,
            CustoUnitario = custoUnitario,
            Documento = documento
        });
    }

    private void DevolverEstoquePedidoCancelado(PedidoOnline pedido)
    {
        foreach (var item in pedido.Itens)
        {
            var produto = _produtos[item.ProdutoId];
            DevolverEstoqueProduto(produto, item.Tamanho, item.Cor, item.Modelo, item.Quantidade);
            produto.AtualizadoEm = DateTime.UtcNow;
            RegistrarMovimentacao(
                produto,
                TipoMovimentacaoEstoque.Entrada,
                item.Quantidade,
                "Cancelamento pedido online",
                null,
                pedido.Id);
        }
    }

    private void BaixarEstoquePedidoReativado(PedidoOnline pedido)
    {
        foreach (var item in pedido.Itens)
        {
            var produto = _produtos[item.ProdutoId];
            BaixarEstoqueProduto(produto, item.Tamanho, item.Cor, item.Modelo, item.Quantidade);
            produto.AtualizadoEm = DateTime.UtcNow;
            RegistrarMovimentacao(
                produto,
                TipoMovimentacaoEstoque.SaidaPedidoOnline,
                item.Quantidade,
                "Reativacao pedido online",
                null,
                pedido.Id);
        }
    }

    private void CriarDadosIniciais()
    {
        var camisetas = new Categoria { Nome = "Camisetas" };
        var acessorios = new Categoria { Nome = "Acessorios" };

        _categorias[camisetas.Id] = camisetas;
        _categorias[acessorios.Id] = acessorios;

        var produto1 = new Produto
        {
            Nome = "Camiseta basica preta",
            CategoriaId = camisetas.Id,
            Sku = "NM-CAM-PRETA",
            Preco = 59.90m,
            Custo = 28.00m,
            QuantidadeEmEstoque = 10,
            Descricao = "Camiseta de algodao para pronta entrega",
            Tamanhos = ["P", "M", "G"],
            Cores = ["Preto"],
            Modelos = ["Basica"],
            GuiaMedidas = "P: busto 88 cm, comprimento 58 cm\nM: busto 94 cm, comprimento 60 cm\nG: busto 100 cm, comprimento 62 cm",
            PublicadoNaLoja = true
        };

        var produto2 = new Produto
        {
            Nome = "Bone bordado",
            CategoriaId = acessorios.Id,
            Sku = "NM-BONE-BORD",
            Preco = 49.90m,
            Custo = 22.00m,
            QuantidadeEmEstoque = 5,
            Descricao = "Bone com aba curva",
            Cores = ["Preto", "Caramelo"],
            Modelos = ["Bordado"],
            PublicadoNaLoja = true
        };

        _produtos[produto1.Id] = produto1;
        _produtos[produto2.Id] = produto2;

        RegistrarMovimentacao(produto1, TipoMovimentacaoEstoque.Entrada, produto1.QuantidadeEmEstoque, "Carga inicial", null);
        RegistrarMovimentacao(produto2, TipoMovimentacaoEstoque.Entrada, produto2.QuantidadeEmEstoque, "Carga inicial", null);
    }

    private void CriarCupomPadrao()
    {
        var cupom = new CupomDesconto
        {
            Codigo = "NANA5",
            Descricao = "Primeira compra Nana Modas",
            PercentualDesconto = 5,
            ValorMinimoPedido = 200,
            Ativo = true
        };

        _cupons[cupom.Id] = cupom;
    }

    private void CriarOpcoesEntregaPadrao()
    {
        var retirada = new OpcaoEntrega
        {
            Nome = "Retirada na loja",
            Tipo = "Retirada",
            Descricao = "Cliente retira o pedido presencialmente após confirmação.",
            Valor = 0,
            FreteGratisAcimaDe = 0,
            PrazoMinimoDias = 0,
            PrazoMaximoDias = 1,
            Ativo = true,
            Ordem = 0
        };

        var entregaLocal = new OpcaoEntrega
        {
            Nome = "Entrega local",
            Tipo = "EntregaLocal",
            Descricao = "Entrega combinada pela Nana Modas para regiões próximas.",
            Valor = _configuracaoLoja.FreteValorPadrao,
            FreteGratisAcimaDe = _configuracaoLoja.FreteGratisAcimaDe,
            PrazoMinimoDias = _configuracaoLoja.PrazoMinimoDias,
            PrazoMaximoDias = _configuracaoLoja.PrazoMaximoDias,
            Ativo = true,
            Ordem = 1
        };

        _opcoesEntrega[retirada.Id] = retirada;
        _opcoesEntrega[entregaLocal.Id] = entregaLocal;
    }

    // Senhas padrão só valem pra uma instalação nova (banco vazio). Como o repositório é
    // público, qualquer senha fixa aqui deve ser tratada como já vazada — troque todas as
    // três pelo painel assim que fizer login pela primeira vez em cada ambiente.
    private void CriarUsuariosPainelPadrao()
    {
        AdicionarUsuarioPainelPadrao("admin", "lajTsbNJKY", "Admin", "Administrador");
        AdicionarUsuarioPainelPadrao("caixa", "FpEoMnyfsg", "Caixa", "Caixa");
        AdicionarUsuarioPainelPadrao("estoque", "oeYeWfodUl", "Estoque", "Estoque");
    }

    private void AdicionarUsuarioPainelPadrao(string usuario, string senha, string perfil, string nome)
    {
        var usuarioPainel = new UsuarioPainel
        {
            Usuario = usuario,
            NomeExibicao = nome,
            Perfil = perfil,
            SenhaHash = CriarHashSenha(senha),
            Ativo = true
        };

        _usuariosPainel[usuarioPainel.Id] = usuarioPainel;
    }

    private static string NormalizarTexto(string texto)
    {
        return texto.Trim();
    }

    private static string NormalizarUsuarioPainel(string? usuario)
    {
        return (usuario ?? "").Trim().ToLowerInvariant();
    }

    private static string? ValidarUsuarioPainel(
        string? usuarioEntrada,
        string? nomeEntrada,
        string? perfilEntrada,
        string? senha,
        bool senhaObrigatoria,
        out string usuario,
        out string nome,
        out string perfil)
    {
        usuario = NormalizarUsuarioPainel(usuarioEntrada);
        nome = NormalizarTexto(nomeEntrada ?? "");
        perfil = NormalizarPerfilPainel(perfilEntrada);

        if (string.IsNullOrWhiteSpace(usuario))
        {
            return "Informe o usuario de login.";
        }

        if (usuario.Length < 3)
        {
            return "O usuario precisa ter pelo menos 3 caracteres.";
        }

        if (usuario.Any(caractere => !char.IsLetterOrDigit(caractere) && caractere != '.' && caractere != '_' && caractere != '-'))
        {
            return "Use apenas letras, numeros, ponto, hifen ou underline no usuario.";
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            return "Informe o nome de exibicao.";
        }

        if (!PerfilPermitido(perfil))
        {
            return "Escolha um perfil valido: Admin, Caixa ou Estoque.";
        }

        if (senhaObrigatoria && string.IsNullOrWhiteSpace(senha))
        {
            return "Informe uma senha para o usuario.";
        }

        if (!string.IsNullOrWhiteSpace(senha) && senha.Length < 6)
        {
            return "A senha precisa ter pelo menos 6 caracteres.";
        }

        return null;
    }

    private static string NormalizarPerfilPainel(string? perfil)
    {
        var texto = (perfil ?? "").Trim();
        return texto.ToLowerInvariant() switch
        {
            "admin" or "administrador" => "Admin",
            "caixa" => "Caixa",
            "estoque" => "Estoque",
            _ => texto
        };
    }

    private static bool PerfilPermitido(string perfil)
    {
        return perfil is "Admin" or "Caixa" or "Estoque";
    }

    private static int PerfilOrdem(string perfil)
    {
        return perfil switch
        {
            "Admin" => 0,
            "Caixa" => 1,
            "Estoque" => 2,
            _ => 9
        };
    }

    private static string NormalizarEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizarProvedorEmail(string? provedor)
    {
        var texto = NormalizarTextoOpcional(provedor);
        return texto?.ToLowerInvariant() switch
        {
            "brevo" => "Brevo",
            _ => "Smtp"
        };
    }

    private static string? NormalizarEmailOpcional(string? email)
    {
        var normalizado = NormalizarTextoOpcional(email)?.ToLowerInvariant();
        return normalizado is not null && EmailValido(normalizado) ? normalizado : null;
    }

    private static bool EmailValido(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
            email.Contains('@', StringComparison.Ordinal) &&
            email.Contains('.', StringComparison.Ordinal);
    }

    private static bool UrlValida(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string? NormalizarTextoOpcional(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }

    private static string? NormalizarDocumento(string? documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return null;
        }

        var digitos = new string(documento.Where(char.IsDigit).ToArray());
        return digitos.Length is 11 or 14 ? digitos : documento.Trim();
    }

    private static string? NormalizarCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
        {
            return null;
        }

        var digitos = new string(cep.Where(char.IsDigit).ToArray());
        return digitos.Length == 8 ? digitos : null;
    }

    private static bool TemCepInvalido(string? cep)
    {
        return !string.IsNullOrWhiteSpace(cep) && NormalizarCep(cep) is null;
    }

    private static bool WebhookPagamentoConfirmado(string? evento, string? status)
    {
        var eventoNormalizado = (evento ?? "").Trim().ToUpperInvariant();
        var statusNormalizado = (status ?? "").Trim().ToUpperInvariant();

        return eventoNormalizado is "PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED" or "PAYMENT_RECEIVED_IN_CASH" ||
            statusNormalizado is "RECEIVED" or "CONFIRMED";
    }

    private static string NormalizarComparacao(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? ""
            : string.Concat(texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD)
                .Where(caractere => CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark));
    }

    private static List<string> NormalizarListaDeImagens(IEnumerable<string>? imagens)
    {
        if (imagens is null)
        {
            return [];
        }

        return imagens
            .Select(NormalizarTextoOpcional)
            .Where(imagem => imagem is not null)
            .Select(imagem => imagem!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizarListaEstados(IEnumerable<string>? estados)
    {
        return NormalizarListaTexto(estados)
            .Select(estado => estado.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizarListaTexto(IEnumerable<string>? itens)
    {
        if (itens is null)
        {
            return [];
        }

        return itens
            .SelectMany(item => item.Split([',', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(NormalizarTextoOpcional)
            .Where(item => item is not null)
            .Select(item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ProdutoVariacaoEstoque> NormalizarVariacoesEstoque(
        IEnumerable<ProdutoVariacaoEstoqueRequest>? variacoes,
        IReadOnlyCollection<string> tamanhos,
        IReadOnlyCollection<string> cores,
        IReadOnlyCollection<string> modelos,
        out string? erro)
    {
        erro = null;
        if (variacoes is null)
        {
            return [];
        }

        var normalizadas = new List<ProdutoVariacaoEstoque>();
        foreach (var variacao in variacoes)
        {
            var tamanho = NormalizarTextoOpcional(variacao.Tamanho);
            var cor = NormalizarTextoOpcional(variacao.Cor);
            var modelo = NormalizarTextoOpcional(variacao.Modelo);

            if (variacao.Quantidade < 0)
            {
                erro = "O estoque da variacao nao pode ser negativo.";
                return [];
            }

            if (!OpcaoExiste(tamanhos, tamanho) || !OpcaoExiste(cores, cor) || !OpcaoExiste(modelos, modelo))
            {
                erro = "Uma variacao de estoque usa tamanho, cor ou modelo que nao existe no produto.";
                return [];
            }

            if (string.IsNullOrWhiteSpace(tamanho) && string.IsNullOrWhiteSpace(cor) && string.IsNullOrWhiteSpace(modelo))
            {
                continue;
            }

            normalizadas.Add(new ProdutoVariacaoEstoque
            {
                Tamanho = tamanho,
                Cor = cor,
                Modelo = modelo,
                Quantidade = variacao.Quantidade
            });
        }

        return normalizadas
            .GroupBy(CriarChaveVariacao)
            .Select(grupo => new ProdutoVariacaoEstoque
            {
                Tamanho = grupo.First().Tamanho,
                Cor = grupo.First().Cor,
                Modelo = grupo.First().Modelo,
                Quantidade = grupo.Sum(item => item.Quantidade)
            })
            .Where(variacao => variacao.Quantidade > 0)
            .ToList();
    }

    private static bool OpcaoExiste(IReadOnlyCollection<string> opcoes, string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ||
            opcoes.Any(opcao => string.Equals(opcao, valor, StringComparison.OrdinalIgnoreCase));
    }

    private static string CriarChaveVariacao(ProdutoVariacaoEstoque variacao)
    {
        return string.Join("|",
            NormalizarComparacao(variacao.Tamanho),
            NormalizarComparacao(variacao.Cor),
            NormalizarComparacao(variacao.Modelo));
    }

}
