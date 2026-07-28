const storeState = {
    categories: [],
    products: [],
    cart: [],
    categoryId: "all",
    search: "",
    minPrice: "",
    maxPrice: "",
    sizeFilter: "all",
    colorFilter: "all",
    availabilityFilter: "available",
    collection: "all",
    catalogTitle: "Escolha suas peças",
    catalogSubtitle: "Use os filtros para encontrar o que combina com seu momento.",
    view: "home",
    selectedProductId: null,
    selectedImageUrl: null,
    config: null,
    customer: null,
    orders: [],
    coupons: [],
    deliveryOptions: [],
    shippingQuote: null,
    couponCode: "",
    lastOrder: null
};

const defaultStoreConfig = {
    nomeCriadorSite: "Davi Silva Dias",
    politicaPrivacidade: "A Nana Modas utiliza os dados informados no checkout apenas para identificar o cliente, confirmar o pedido, combinar a entrega e prestar atendimento.",
    freteValorPadrao: 19.90,
    freteGratisAcimaDe: 399,
    prazoMinimoDias: 3,
    prazoMaximoDias: 7,
    mensagemFrete: "O frete é calculado como estimativa. A loja confirma o valor final pelo atendimento.",
    mensagemLoginCliente: "Entre para salvar seus dados neste dispositivo e deixar o checkout mais rápido.",
    bannerEyebrow: "Coleção pronta entrega",
    bannerTitulo: "Nana Modas",
    bannerDescricao: "Peças selecionadas, estética premium e compra online integrada ao estoque da loja física.",
    bannerBotaoPrimario: "Ver coleção",
    bannerBotaoSecundario: "Minha sacola",
    bannerImagemUrl: "",
    promocaoTopoTexto: "Compra segura Nana Modas: estoque real, atendimento direto e pagamento por Pix ou cartão.",
    campanhaTitulo: "Coleção premium pronta entrega",
    campanhaDescricao: "Peças selecionadas, atendimento direto e checkout conectado ao estoque real da loja.",
    campanhaBotaoTexto: "Ver novidades",
    campanhaImagemUrl: "",
    vitrineImagem1Url: "",
    vitrineImagem1Titulo: "Novidades",
    vitrineImagem2Url: "",
    vitrineImagem2Titulo: "Promoções",
    vitrineImagem3Url: "",
    vitrineImagem3Titulo: "Mais desejados",
    whatsappLoja: "",
    instagramLoja: "",
    enderecoLoja: "",
    pixChave: "Configure a chave Pix no painel",
    pixNomeRecebedor: "NANA MODAS",
    pixCidade: "SAO PAULO",
    pixOnlineAtivo: true,
    cartaoOnlineAtivo: true,
    checkoutCartaoNome: "Link de pagamento",
    checkoutCartaoUrl: "",
    mensagemPagamento: "Após finalizar o pedido, envie o comprovante pelo WhatsApp da loja para confirmação.",
    mensagemPagamentoCartao: "Finalize o pedido e use o link de pagamento, ou aguarde a loja enviar a cobrança.",
    gatewayPagamentoProvedor: "",
    gatewayPagamentoAtivo: false,
    gatewayPagamentoProducao: false,
    gatewayPagamentoPublicKey: "",
    razaoSocial: "",
    cnpj: "",
    siteUrlCanonica: "",
    politicaTrocaDevolucao: "Você pode desistir da compra em até 7 dias corridos após o recebimento, sem precisar justificar o motivo, conforme o art. 49 do Código de Defesa do Consumidor.",
    googleAnalyticsId: "",
    metaPixelId: ""
};

const storeCurrency = new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL"
});

const storeDate = new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short"
});

const storeEls = {};

document.addEventListener("DOMContentLoaded", () => {
    cacheStoreElements();
    bindStoreEvents();
    const initialRoute = getStoreRouteFromHash();
    storeState.view = initialRoute.view;
    storeState.selectedProductId = initialRoute.productId;
    loadStore();
});

function cacheStoreElements() {
    storeEls.status = document.querySelector("#storeStatus");
    storeEls.hero = document.querySelector("#storeHero");
    storeEls.heroEyebrow = document.querySelector("#heroEyebrow");
    storeEls.heroTitle = document.querySelector("#storeTitle");
    storeEls.heroDescription = document.querySelector("#heroDescription");
    storeEls.heroPrimaryButton = document.querySelector("#heroPrimaryButton");
    storeEls.heroSecondaryButton = document.querySelector("#heroSecondaryButton");
    storeEls.promoText = document.querySelector("#storePromoText");
    storeEls.campaign = document.querySelector("#siteCampaign");
    storeEls.campaignTitle = document.querySelector("#siteCampaignTitle");
    storeEls.campaignDescription = document.querySelector("#siteCampaignDescription");
    storeEls.campaignButton = document.querySelector("#siteCampaignButton");
    storeEls.campaignMedia = document.querySelector("#siteCampaignMedia");
    storeEls.visualShowcase = document.querySelector("#visualShowcase");
    storeEls.homeFeaturedProducts = document.querySelector("#homeFeaturedProducts");
    storeEls.homeLaunchProducts = document.querySelector("#homeLaunchProducts");
    storeEls.homeCategoryGrid = document.querySelector("#homeCategoryGrid");
    storeEls.contactStrip = document.querySelector("#storeContactStrip");
    storeEls.contactActions = document.querySelector("#storeContactActions");
    storeEls.footerContact = document.querySelector("#footerContact");
    storeEls.productCount = document.querySelector("#storeProductCount");
    storeEls.cartCount = document.querySelector("#storeCartCount");
    storeEls.headerCartCount = document.querySelector("#storeHeaderCartCount");
    storeEls.tabCartCount = document.querySelector("#storeTabCartCount");
    storeEls.tabButtons = document.querySelectorAll("[data-store-tab]");
    storeEls.homeButtons = document.querySelectorAll("[data-store-home]");
    storeEls.views = document.querySelectorAll("[data-store-view]");
    storeEls.catalogTitle = document.querySelector("#catalogTitle");
    storeEls.catalogSubtitle = document.querySelector("#catalogSubtitle");
    storeEls.categories = document.querySelector("#storeCategories");
    storeEls.products = document.querySelector("#storeProducts");
    storeEls.productDetail = document.querySelector("#storeProductDetail");
    storeEls.search = document.querySelector("#storeSearch");
    storeEls.minPrice = document.querySelector("#storeMinPrice");
    storeEls.maxPrice = document.querySelector("#storeMaxPrice");
    storeEls.sizeFilter = document.querySelector("#storeSizeFilter");
    storeEls.colorFilter = document.querySelector("#storeColorFilter");
    storeEls.availabilityFilter = document.querySelector("#storeAvailabilityFilter");
    storeEls.clearFilters = document.querySelector("#clearStoreFilters");
    storeEls.cartList = document.querySelector("#storeCartList");
    storeEls.cartSubtotal = document.querySelector("#storeCartSubtotal");
    storeEls.cartDiscountRow = document.querySelector("#storeCartDiscountRow");
    storeEls.cartDiscount = document.querySelector("#storeCartDiscount");
    storeEls.cartShippingRow = document.querySelector("#storeCartShippingRow");
    storeEls.cartShipping = document.querySelector("#storeCartShipping");
    storeEls.cartTotal = document.querySelector("#storeCartTotal");
    storeEls.couponCode = document.querySelector("#storeCouponCode");
    storeEls.applyCoupon = document.querySelector("#applyStoreCoupon");
    storeEls.couponMessage = document.querySelector("#storeCouponMessage");
    storeEls.clearCart = document.querySelector("#clearStoreCart");
    storeEls.continueShopping = document.querySelector("#continueShoppingButton");
    storeEls.checkoutForm = document.querySelector("#storeCheckoutForm");
    storeEls.checkoutButton = document.querySelector("#storeCheckoutButton");
    storeEls.checkoutShipping = document.querySelector("#storeCheckoutShipping");
    storeEls.customerName = document.querySelector("#customerName");
    storeEls.customerEmail = document.querySelector("#customerEmail");
    storeEls.customerPhone = document.querySelector("#customerPhone");
    storeEls.customerDocument = document.querySelector("#customerDocument");
    storeEls.customerCep = document.querySelector("#customerCep");
    storeEls.customerStreet = document.querySelector("#customerStreet");
    storeEls.customerNumber = document.querySelector("#customerNumber");
    storeEls.customerComplement = document.querySelector("#customerComplement");
    storeEls.customerDistrict = document.querySelector("#customerDistrict");
    storeEls.customerCity = document.querySelector("#customerCity");
    storeEls.customerState = document.querySelector("#customerState");
    storeEls.customerOrderNote = document.querySelector("#customerOrderNote");
    storeEls.paymentHelp = document.querySelector("#storePaymentHelp");
    storeEls.pixPaymentOption = document.querySelector("#storePixPaymentOption");
    storeEls.cardPaymentOption = document.querySelector("#storeCardPaymentOption");
    storeEls.pixPaymentInput = document.querySelector("#storePaymentPix");
    storeEls.cardPaymentInput = document.querySelector("#storePaymentCard");
    storeEls.shippingForm = document.querySelector("#shippingForm");
    storeEls.shippingCep = document.querySelector("#shippingCep");
    storeEls.shippingDistrict = document.querySelector("#shippingDistrict");
    storeEls.shippingCity = document.querySelector("#shippingCity");
    storeEls.shippingState = document.querySelector("#shippingState");
    storeEls.shippingMessage = document.querySelector("#shippingMessage");
    storeEls.shippingResult = document.querySelector("#shippingResult");
    storeEls.customerLoginForm = document.querySelector("#customerLoginForm");
    storeEls.customerPasswordResetForm = document.querySelector("#customerPasswordResetForm");
    storeEls.accountName = document.querySelector("#accountName");
    storeEls.accountEmail = document.querySelector("#accountEmail");
    storeEls.accountPhone = document.querySelector("#accountPhone");
    storeEls.accountPassword = document.querySelector("#accountPassword");
    storeEls.customerLoginButton = document.querySelector("#customerLoginButton");
    storeEls.customerLogoutButton = document.querySelector("#customerLogoutButton");
    storeEls.resetEmail = document.querySelector("#resetEmail");
    storeEls.resetCode = document.querySelector("#resetCode");
    storeEls.resetNewPassword = document.querySelector("#resetNewPassword");
    storeEls.requestResetButton = document.querySelector("#requestResetButton");
    storeEls.confirmResetButton = document.querySelector("#confirmResetButton");
    storeEls.resetPasswordMessage = document.querySelector("#resetPasswordMessage");
    storeEls.customerLoginMessage = document.querySelector("#customerLoginMessage");
    storeEls.customerAccountSummary = document.querySelector("#customerAccountSummary");
    storeEls.customerOrders = document.querySelector("#customerOrders");
    storeEls.customerOrdersSummary = document.querySelector("#customerOrdersSummary");
    storeEls.privacyPolicyText = document.querySelector("#privacyPolicyText");
    storeEls.exchangePolicyText = document.querySelector("#exchangePolicyText");
    storeEls.footerLegal = document.querySelector("#footerLegal");
    storeEls.siteCreatorName = document.querySelector("#siteCreatorName");
    storeEls.toast = document.querySelector("#storeToast");
    storeEls.orderModal = document.querySelector("#orderModal");
    storeEls.orderMessage = document.querySelector("#orderMessage");
    storeEls.orderSummary = document.querySelector("#orderSummary");
    storeEls.orderPaymentBox = document.querySelector("#orderPaymentBox");
    storeEls.orderPixQrCode = document.querySelector("#orderPixQrCode");
    storeEls.orderPixKey = document.querySelector("#orderPixKey");
    storeEls.orderPixInstructions = document.querySelector("#orderPixInstructions");
    storeEls.copyPixKeyButton = document.querySelector("#copyPixKeyButton");
    storeEls.orderCardPaymentBox = document.querySelector("#orderCardPaymentBox");
    storeEls.orderCardPaymentTitle = document.querySelector("#orderCardPaymentTitle");
    storeEls.orderCardPaymentName = document.querySelector("#orderCardPaymentName");
    storeEls.orderCardPaymentInstructions = document.querySelector("#orderCardPaymentInstructions");
    storeEls.openCardPaymentButton = document.querySelector("#openCardPaymentButton");
    storeEls.copyOrderSummaryButton = document.querySelector("#copyOrderSummaryButton");
    storeEls.printOrderReceiptButton = document.querySelector("#printOrderReceiptButton");
    storeEls.whatsappOrderButton = document.querySelector("#whatsappOrderButton");
    storeEls.emailOrderButton = document.querySelector("#emailOrderButton");
    storeEls.goToOrdersButton = document.querySelector("#goToOrdersButton");
    storeEls.closeOrderModal = document.querySelector("#closeOrderModal");
}

function bindStoreEvents() {
    storeEls.tabButtons.forEach((button) => {
        button.addEventListener("click", () => navigateStoreButton(button));
    });

    storeEls.homeButtons.forEach((button) => {
        button.addEventListener("click", (event) => {
            event.preventDefault();
            showStoreView("home");
            window.scrollTo({ top: 0, behavior: "smooth" });
        });
    });

    storeEls.search.addEventListener("input", (event) => {
        storeState.search = event.target.value;
        renderStoreProducts();
    });

    storeEls.minPrice.addEventListener("input", (event) => {
        storeState.minPrice = event.target.value;
        renderStoreProducts();
    });

    storeEls.maxPrice.addEventListener("input", (event) => {
        storeState.maxPrice = event.target.value;
        renderStoreProducts();
    });

    storeEls.sizeFilter.addEventListener("change", (event) => {
        storeState.sizeFilter = event.target.value;
        renderStoreProducts();
    });

    storeEls.colorFilter.addEventListener("change", (event) => {
        storeState.colorFilter = event.target.value;
        renderStoreProducts();
    });

    storeEls.availabilityFilter.addEventListener("change", (event) => {
        storeState.availabilityFilter = event.target.value;
        renderStoreProducts();
    });

    if (storeEls.clearFilters) {
        storeEls.clearFilters.addEventListener("click", () => {
            storeState.search = "";
            storeState.categoryId = "all";
            storeState.minPrice = "";
            storeState.maxPrice = "";
            storeState.sizeFilter = "all";
            storeState.colorFilter = "all";
            storeState.availabilityFilter = "available";
            storeState.collection = "all";
            storeState.catalogTitle = "Todos os produtos";
            storeState.catalogSubtitle = "Use os filtros para encontrar o que combina com seu momento.";
            storeEls.search.value = "";
            storeEls.minPrice.value = "";
            storeEls.maxPrice.value = "";
            storeEls.availabilityFilter.value = "available";
            renderStoreVariationFilters();
            renderStoreCategories();
            renderStoreProducts();
            renderStoreHome();
        });
    }

    storeEls.categories.addEventListener("click", (event) => {
        const button = event.target.closest("[data-category]");
        if (!button) {
            return;
        }

        storeState.categoryId = button.dataset.category;
        storeState.collection = "all";
        const category = storeState.categories.find((item) => item.id === storeState.categoryId);
        storeState.catalogTitle = category ? category.nome : "Todos os produtos";
        storeState.catalogSubtitle = category
            ? `Peças da categoria ${category.nome}, com filtros por preço, tamanho e cor.`
            : "Use os filtros para encontrar o que combina com seu momento.";
        renderStoreCategories();
        renderStoreProducts();
    });

    storeEls.products.addEventListener("click", handleStoreProductListClick);
    storeEls.homeFeaturedProducts.addEventListener("click", handleStoreProductListClick);
    storeEls.homeLaunchProducts.addEventListener("click", handleStoreProductListClick);
    storeEls.homeCategoryGrid.addEventListener("click", (event) => {
        const button = event.target.closest("[data-home-category]");
        if (!button) {
            return;
        }

        openStoreCategory(button.dataset.homeCategory, button.dataset.homeCategoryName);
    });

    storeEls.productDetail.addEventListener("click", (event) => {
        const addButton = event.target.closest("[data-add]");
        if (addButton) {
            const product = getSelectedStoreProduct();
            addStoreProduct(addButton.dataset.add, product ? getSelectedDetailVariation(product) : null);
            return;
        }

        const detailAction = event.target.closest("[data-detail-action]");
        if (detailAction?.dataset.detailAction === "back") {
            showStoreView("catalog");
            return;
        }

        const productButton = event.target.closest("[data-open-product]");
        if (productButton) {
            openStoreProduct(productButton.dataset.openProduct);
            return;
        }

        const thumbButton = event.target.closest("[data-detail-image-index]");
        if (thumbButton) {
            const product = getSelectedStoreProduct();
            const images = getStoreProductImages(product);
            storeState.selectedImageUrl = images[Number(thumbButton.dataset.detailImageIndex)] || null;
            renderStoreProductDetail();
        }
    });

    storeEls.cartList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-cart-action]");
        if (!button) {
            return;
        }

        changeStoreCart(button.dataset.id, button.dataset.cartAction);
    });

    storeEls.clearCart.addEventListener("click", () => {
        storeState.cart = [];
        storeState.couponCode = "";
        storeEls.couponCode.value = "";
        renderStoreCart();
        renderStoreShipping();
        renderStoreProducts();
        renderStoreHome();
    });

    storeEls.continueShopping.addEventListener("click", () => {
        prepareStoreCatalog();
        showStoreView("catalog");
    });
    storeEls.applyCoupon.addEventListener("click", applyStoreCoupon);
    storeEls.couponCode.addEventListener("input", () => {
        if (!storeEls.couponCode.value.trim()) {
            storeState.couponCode = "";
            renderStoreCart();
            renderStoreShipping();
        }
    });

    storeEls.checkoutForm.addEventListener("submit", submitStoreOrder);
    document.querySelectorAll("input[name='storePayment']").forEach((input) => {
        input.addEventListener("change", renderStorePaymentHelp);
    });
    storeEls.shippingForm.addEventListener("submit", calculateStoreShipping);
    storeEls.shippingResult.addEventListener("click", (event) => {
        const button = event.target.closest("[data-select-delivery]");
        if (!button) {
            return;
        }

        selectStoreDeliveryOption(button.dataset.selectDelivery);
    });
    storeEls.customerLoginForm.addEventListener("submit", saveStoreCustomer);
    storeEls.customerLogoutButton.addEventListener("click", logoutStoreCustomer);
    storeEls.requestResetButton.addEventListener("click", requestStorePasswordReset);
    storeEls.customerPasswordResetForm.addEventListener("submit", confirmStorePasswordReset);
    storeEls.customerOrders.addEventListener("click", (event) => {
        const action = event.target.closest("[data-orders-action]")?.dataset.ordersAction;
        if (action === "login") {
            showStoreView("account");
        }

        if (action === "catalog") {
            prepareStoreCatalog();
            showStoreView("catalog");
        }

        if (action === "copy") {
            const order = storeState.orders.find((item) => item.id === event.target.closest("[data-order-id]")?.dataset.orderId);
            if (order) {
                copyStoreOrderSummary(order);
            }
        }

        if (action === "whatsapp") {
            const order = storeState.orders.find((item) => item.id === event.target.closest("[data-order-id]")?.dataset.orderId);
            if (order) {
                shareStoreOrderWhatsApp(order);
            }
        }

        if (action === "email") {
            const order = storeState.orders.find((item) => item.id === event.target.closest("[data-order-id]")?.dataset.orderId);
            if (order) {
                shareStoreOrderEmail(order);
            }
        }

        if (action === "receipt") {
            const order = storeState.orders.find((item) => item.id === event.target.closest("[data-order-id]")?.dataset.orderId);
            if (order) {
                printStoreOrderReceipt(order);
            }
        }
    });
    storeEls.closeOrderModal.addEventListener("click", () => {
        storeEls.orderModal.classList.add("hidden");
        prepareStoreCatalog();
        showStoreView("catalog");
    });
    storeEls.goToOrdersButton.addEventListener("click", () => {
        storeEls.orderModal.classList.add("hidden");
        showStoreView("orders");
    });
    storeEls.copyPixKeyButton.addEventListener("click", copyStorePixKey);
    storeEls.openCardPaymentButton.addEventListener("click", openStoreCardPayment);
    storeEls.copyOrderSummaryButton.addEventListener("click", () => {
        if (storeState.lastOrder) {
            copyStoreOrderSummary(storeState.lastOrder);
        }
    });
    storeEls.printOrderReceiptButton.addEventListener("click", () => {
        if (storeState.lastOrder) {
            printStoreOrderReceipt(storeState.lastOrder);
        }
    });
    storeEls.whatsappOrderButton.addEventListener("click", () => {
        if (storeState.lastOrder) {
            shareStoreOrderWhatsApp(storeState.lastOrder);
        }
    });

    storeEls.emailOrderButton.addEventListener("click", () => {
        if (storeState.lastOrder) {
            shareStoreOrderEmail(storeState.lastOrder);
        }
    });
    window.addEventListener("hashchange", applyStoreRouteFromHash);
}

async function loadStore() {
    try {
        storeEls.status.textContent = "Online";
        const [categories, products, config, coupons, deliveryOptions, customerStatus] = await Promise.all([
            storeApi("/loja-api/categorias"),
            storeApi("/loja-api/produtos"),
            storeApi("/loja-api/configuracao"),
            storeApi("/loja-api/cupons"),
            storeApi("/loja-api/entregas"),
            storeApi("/clientes/status")
        ]);

        storeState.categories = categories;
        storeState.products = products.filter((product) => product.ativo);
        storeState.config = { ...defaultStoreConfig, ...config };
        storeState.coupons = coupons;
        storeState.deliveryOptions = deliveryOptions;
        storeState.customer = customerStatus?.autenticado ? customerStatus.cliente : null;
        storeState.orders = storeState.customer ? await storeApi("/clientes/pedidos") : [];

        if (storeState.view === "product" && !getSelectedStoreProduct()) {
            storeState.view = "catalog";
            storeState.selectedProductId = null;
        }

        syncStoreCart();
        renderStore();
    } catch (error) {
        storeEls.status.textContent = "Offline";
        showStoreToast(error.message || "Não foi possível carregar a loja.");
    }
}

async function storeApi(path, options = {}) {
    const response = await fetch(path, {
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        },
        ...options
    });

    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;

    if (!response.ok) {
        throw new Error(payload?.erro || "Erro na operação.");
    }

    return payload;
}

function renderStore() {
    showStoreView(storeState.view, false);
    renderStoreConfig();
    renderStoreCustomer();
    renderStoreShipping();
    renderStorePaymentOptions();
    renderStorePaymentHelp();
    renderStoreCategories();
    renderStoreVariationFilters();
    renderStoreProducts();
    renderStoreHome();
    renderStoreProductDetail();
    renderStoreCart();
    renderStoreOrders();
}

function showStoreView(viewName, updateHash = true) {
    storeState.view = viewName;
    const activeTab = viewName === "product" ? "catalog" : viewName;

    storeEls.tabButtons.forEach((button) => {
        button.classList.toggle("is-active", isStoreNavigationButtonActive(button, activeTab));
    });

    storeEls.views.forEach((view) => {
        view.classList.toggle("is-active", view.dataset.storeView === viewName);
    });

    if (!updateHash) {
        return;
    }

    let nextHash = "#inicio";
    if (viewName === "catalog") {
        nextHash = "#produtos";
    }

    if (viewName === "bag") {
        nextHash = "#sacola";
    }

    if (viewName === "shipping") {
        nextHash = "#frete";
    }

    if (viewName === "account") {
        nextHash = "#cliente";
    }

    if (viewName === "orders") {
        nextHash = "#pedidos";
    }

    if (viewName === "privacy") {
        nextHash = "#privacidade";
    }

    if (viewName === "exchanges") {
        nextHash = "#trocas";
    }

    if (viewName === "product" && storeState.selectedProductId) {
        nextHash = `#produto-${storeState.selectedProductId}`;
    }

    if (window.location.hash !== nextHash) {
        window.location.hash = nextHash;
    }

    window.scrollTo({ top: 0, behavior: "smooth" });
}

function isStoreNavigationButtonActive(button, activeTab) {
    if (button.dataset.storeTab !== activeTab) {
        return false;
    }

    if (activeTab !== "catalog") {
        return true;
    }

    if (storeState.collection === "launches") {
        return button.dataset.storeCollection === "launches";
    }

    const categoryName = button.dataset.storeCategoryName;
    if (storeState.categoryId !== "all") {
        const category = categoryName ? findStoreCategoryByName(categoryName) : null;
        return category?.id === storeState.categoryId;
    }

    return !categoryName && !button.dataset.storeCollection;
}

function navigateStoreButton(button) {
    const viewName = button.dataset.storeTab;
    if (!viewName) {
        return;
    }

    if (viewName === "catalog") {
        prepareStoreCatalog(button);
    }

    showStoreView(viewName);

    if (button.dataset.storeFocusSearch === "true") {
        setTimeout(() => storeEls.search?.focus(), 80);
    }
}

function prepareStoreCatalog(trigger = null) {
    const categoryName = trigger?.dataset.storeCategoryName || "";
    const collection = trigger?.dataset.storeCollection || "all";

    storeState.collection = collection;
    storeState.search = "";
    storeState.minPrice = "";
    storeState.maxPrice = "";
    storeState.sizeFilter = "all";
    storeState.colorFilter = "all";
    storeState.availabilityFilter = "available";
    storeState.categoryId = "all";

    if (storeEls.search) {
        storeEls.search.value = "";
        storeEls.minPrice.value = "";
        storeEls.maxPrice.value = "";
        storeEls.availabilityFilter.value = "available";
    }

    if (categoryName) {
        const category = findStoreCategoryByName(categoryName);
        storeState.categoryId = category?.id || "all";
        storeState.catalogTitle = category?.nome || categoryName;
        storeState.catalogSubtitle = `Peças de ${category?.nome || categoryName} com estoque integrado à loja física.`;
    } else if (collection === "launches") {
        storeState.catalogTitle = "Lançamentos";
        storeState.catalogSubtitle = "Novidades e peças em destaque para comprar online.";
    } else {
        storeState.catalogTitle = "Todos os produtos";
        storeState.catalogSubtitle = "Use os filtros para encontrar o que combina com seu momento.";
    }

    renderStoreVariationFilters();
    renderStoreCategories();
    renderStoreProducts();
}

function openStoreCategory(categoryId, categoryName = "") {
    storeState.collection = "all";
    storeState.categoryId = categoryId || "all";
    storeState.search = "";
    storeState.minPrice = "";
    storeState.maxPrice = "";
    storeState.sizeFilter = "all";
    storeState.colorFilter = "all";
    storeState.availabilityFilter = "available";
    const category = storeState.categories.find((item) => item.id === categoryId);
    storeState.catalogTitle = category?.nome || categoryName || "Produtos";
    storeState.catalogSubtitle = category
        ? `Peças da categoria ${category.nome}, com estoque disponível para compra online.`
        : "Use os filtros para encontrar o que combina com seu momento.";

    if (storeEls.search) {
        storeEls.search.value = "";
        storeEls.minPrice.value = "";
        storeEls.maxPrice.value = "";
        storeEls.availabilityFilter.value = "available";
    }

    renderStoreVariationFilters();
    renderStoreCategories();
    renderStoreProducts();
    showStoreView("catalog");
}

function renderStoreConfig() {
    const config = getStoreConfig();
    const bestCoupon = getBestStoreCoupon();
    storeEls.promoText.textContent = config.promocaoTopoTexto || (bestCoupon
        ? `Primeira compra na Nana Modas? Use o cupom ${bestCoupon.codigo} para ${formatStorePercent(bestCoupon.percentualDesconto)} OFF acima de ${storeCurrency.format(bestCoupon.valorMinimoPedido)}.`
        : "Nana Modas online: peças selecionadas com estoque integrado à loja física.");
    storeEls.couponCode.placeholder = bestCoupon?.codigo || "Cupom";
    storeEls.heroEyebrow.textContent = config.bannerEyebrow || defaultStoreConfig.bannerEyebrow;
    storeEls.heroTitle.textContent = config.bannerTitulo || defaultStoreConfig.bannerTitulo;
    storeEls.heroDescription.textContent = config.bannerDescricao || defaultStoreConfig.bannerDescricao;
    storeEls.heroPrimaryButton.textContent = config.bannerBotaoPrimario || defaultStoreConfig.bannerBotaoPrimario;
    storeEls.heroSecondaryButton.textContent = config.bannerBotaoSecundario || defaultStoreConfig.bannerBotaoSecundario;
    if (config.bannerImagemUrl) {
        storeEls.hero.style.setProperty("--hero-image", `url("${escapeStoreCssUrl(config.bannerImagemUrl)}")`);
    } else {
        storeEls.hero.style.removeProperty("--hero-image");
    }
    renderStoreCampaign(config);
    renderStoreVisualShowcase(config);
    renderStoreContact(config);
    storeEls.shippingMessage.textContent = config.mensagemFrete || defaultStoreConfig.mensagemFrete;
    storeEls.customerLoginMessage.textContent = config.mensagemLoginCliente || defaultStoreConfig.mensagemLoginCliente;
    storeEls.siteCreatorName.textContent = config.nomeCriadorSite || defaultStoreConfig.nomeCriadorSite;
    storeEls.privacyPolicyText.innerHTML = formatStoreText(config.politicaPrivacidade || defaultStoreConfig.politicaPrivacidade);
    storeEls.exchangePolicyText.innerHTML = formatStoreText(config.politicaTrocaDevolucao || defaultStoreConfig.politicaTrocaDevolucao);

    const razaoSocial = (config.razaoSocial || "").trim();
    const cnpj = (config.cnpj || "").trim();
    if (razaoSocial || cnpj) {
        storeEls.footerLegal.hidden = false;
        storeEls.footerLegal.textContent = [razaoSocial, cnpj ? `CNPJ ${cnpj}` : ""].filter(Boolean).join(" · ");
    } else {
        storeEls.footerLegal.hidden = true;
        storeEls.footerLegal.textContent = "";
    }

    aplicarAnalyticsStoreConfig(config);
}

function renderStoreCampaign(config) {
    storeEls.campaignTitle.textContent = config.campanhaTitulo || defaultStoreConfig.campanhaTitulo;
    storeEls.campaignDescription.textContent = config.campanhaDescricao || defaultStoreConfig.campanhaDescricao;
    storeEls.campaignButton.textContent = config.campanhaBotaoTexto || defaultStoreConfig.campanhaBotaoTexto;

    const image = String(config.campanhaImagemUrl || "").trim();
    storeEls.campaignMedia.innerHTML = image
        ? `<img src="${escapeStoreHtml(image)}" alt="${escapeStoreHtml(config.campanhaTitulo || "Campanha Nana Modas")}">`
        : `<div class="campaign-placeholder"><span>Nana Modas</span><strong>Premium</strong></div>`;
}

function renderStoreVisualShowcase(config) {
    const slots = [
        { title: config.vitrineImagem1Titulo || defaultStoreConfig.vitrineImagem1Titulo, image: config.vitrineImagem1Url },
        { title: config.vitrineImagem2Titulo || defaultStoreConfig.vitrineImagem2Titulo, image: config.vitrineImagem2Url },
        { title: config.vitrineImagem3Titulo || defaultStoreConfig.vitrineImagem3Titulo, image: config.vitrineImagem3Url }
    ];

    storeEls.visualShowcase.innerHTML = slots.map((slot) => {
        const image = String(slot.image || "").trim();
        return `
            <article class="visual-tile">
                ${image ? `<img src="${escapeStoreHtml(image)}" alt="${escapeStoreHtml(slot.title)}">` : `<div class="visual-placeholder"><span>${escapeStoreHtml(slot.title)}</span></div>`}
                <strong>${escapeStoreHtml(slot.title)}</strong>
            </article>
        `;
    }).join("");
}

function renderStoreContact(config) {
    const whatsapp = String(config.whatsappLoja || "").trim();
    const instagram = String(config.instagramLoja || "").trim();
    const address = String(config.enderecoLoja || "").trim();
    const whatsappUrl = buildStoreWhatsappUrl(whatsapp);
    const instagramUrl = buildStoreInstagramUrl(instagram);
    const actions = [
        whatsappUrl ? `<a class="checkout-button whatsapp-link" href="${escapeStoreHtml(whatsappUrl)}" target="_blank" rel="noopener">${renderWhatsappLabel("WhatsApp")}</a>` : "",
        instagramUrl ? `<a class="ghost-button" href="${escapeStoreHtml(instagramUrl)}" target="_blank" rel="noopener">Instagram</a>` : ""
    ].filter(Boolean);

    if (storeEls.contactActions) {
        storeEls.contactActions.innerHTML = actions.length
            ? actions.join("")
            : `<span class="contact-muted">Configure WhatsApp e Instagram no painel.</span>`;
    }

    if (storeEls.contactStrip) {
        storeEls.contactStrip.classList.toggle("is-empty", !actions.length && !address);
    }

    const footerItems = [
        whatsappUrl ? `<a class="whatsapp-footer-link" href="${escapeStoreHtml(whatsappUrl)}" target="_blank" rel="noopener">${renderWhatsappLabel("WhatsApp")}</a>` : "",
        instagramUrl ? `<a href="${escapeStoreHtml(instagramUrl)}" target="_blank" rel="noopener">Instagram</a>` : "",
        address ? `<span>${escapeStoreHtml(address)}</span>` : ""
    ].filter(Boolean);

    storeEls.footerContact.innerHTML = footerItems.length
        ? footerItems.join("")
        : `<span>Contato da loja em configuração.</span>`;
}

function buildStoreWhatsappUrl(value) {
    const digits = String(value || "").replace(/\D/g, "");
    return digits.length >= 10 ? `https://wa.me/${digits}` : "";
}

function buildStoreInstagramUrl(value) {
    const normalized = String(value || "").trim();
    if (!normalized) {
        return "";
    }

    if (/^https?:\/\//i.test(normalized)) {
        return normalized;
    }

    const handle = normalized.replace(/^@/, "").replace(/^instagram\.com\//i, "").replace(/^www\.instagram\.com\//i, "").split(/[/?#]/)[0];
    return handle ? `https://instagram.com/${encodeURIComponent(handle)}` : "";
}

function renderWhatsappLabel(text) {
    return `
        <span class="whatsapp-mini-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" focusable="false">
                <path d="M7.4 18.4 4 19.2l.9-3.2A7.7 7.7 0 1 1 7.4 18.4Z"></path>
                <path d="M9.3 8.2c.2-.3.4-.4.7-.4h.5c.2 0 .4.1.5.4l.6 1.4c.1.2.1.5-.1.7l-.4.5c.5.9 1.2 1.6 2.1 2.1l.5-.4c.2-.2.5-.2.7-.1l1.4.6c.3.1.4.3.4.6v.5c0 .3-.1.5-.4.7-.4.2-.9.4-1.4.4-2.9-.1-5.4-2.5-5.5-5.5 0-.5.1-1 .4-1.4Z"></path>
            </svg>
        </span>
        <span>${escapeStoreHtml(text)}</span>
    `;
}

function escapeStoreCssUrl(value) {
    return String(value || "").replace(/["\\\n\r]/g, "");
}

function renderStoreCustomer() {
    const customer = storeState.customer;
    const isLogged = Boolean(customer?.email);

    storeEls.customerLogoutButton.classList.toggle("hidden", !isLogged);
    storeEls.customerLoginButton.textContent = isLogged ? "Atualizar dados" : "Entrar ou criar conta";

    if (isLogged) {
        storeEls.accountName.value = customer.nome || "";
        storeEls.accountEmail.value = customer.email || "";
        storeEls.accountPhone.value = customer.telefone || "";
        storeEls.customerName.value = customer.nome || storeEls.customerName.value;
        storeEls.customerEmail.value = customer.email || storeEls.customerEmail.value;
        storeEls.customerPhone.value = customer.telefone || storeEls.customerPhone.value;
        storeEls.customerAccountSummary.innerHTML = `
            <p class="store-eyebrow">Status</p>
            <h2>${escapeStoreHtml(customer.nome || "Cliente")}</h2>
            <div class="account-summary-lines">
                <span>${escapeStoreHtml(customer.email)}</span>
                <span>${escapeStoreHtml(customer.telefone || "Telefone não informado")}</span>
            </div>
        `;
        return;
    }

    storeEls.customerAccountSummary.innerHTML = `
        <p class="store-eyebrow">Status</p>
        <h2>Visitante</h2>
        <span>Entre com e-mail e senha para preencher o checkout automaticamente.</span>
    `;
}

function renderStoreShipping() {
    const config = getStoreConfig();
    const quote = storeState.shippingQuote;
    const deliveryOptions = storeState.deliveryOptions || [];
    const totalProducts = getStoreCartTotal();

    if (!quote) {
        const cards = deliveryOptions.length
            ? deliveryOptions.slice(0, 3).map((option) => renderStoreDeliveryCard(option, null, totalProducts, false)).join("")
            : renderStoreFallbackShipping(config);
        storeEls.shippingResult.innerHTML = cards;
        storeEls.checkoutShipping.textContent = "Calcule pelo CEP";
        return;
    }

    if (!quote.options.length) {
        storeEls.shippingResult.innerHTML = `
            <div class="shipping-line">
                <span>CEP</span>
                <strong>${escapeStoreHtml(quote.cep)}</strong>
            </div>
            <p>Nenhuma opção ativa atende esse endereço. A loja pode combinar a entrega pelo atendimento.</p>
        `;
        storeEls.checkoutShipping.textContent = "Entrega a combinar";
        return;
    }

    storeEls.shippingResult.innerHTML = `
        <div class="shipping-line">
            <span>CEP</span>
            <strong>${escapeStoreHtml(quote.cep)}</strong>
        </div>
        ${quote.options.map((option) => renderStoreDeliveryCard(option, quote.selectedId, totalProducts, true)).join("")}
    `;

    const selected = getSelectedStoreDelivery();
    storeEls.checkoutShipping.textContent = selected
        ? `${formatStoreShippingValue(selected.valor)} · ${selected.nome}`
        : "Escolha uma entrega";
    updateStoreAddressRequirement(selected);
}

// Retirada na loja não precisa de endereço — sem isso, o navegador bloqueia o
// envio do formulário (campos "required") mesmo quando o backend já aceita o
// pedido sem endereço para esse tipo de entrega.
function updateStoreAddressRequirement(selected) {
    const isPickup = selected?.tipo === "Retirada";
    [
        storeEls.customerStreet,
        storeEls.customerNumber,
        storeEls.customerDistrict,
        storeEls.customerCity,
        storeEls.customerState
    ].forEach((field) => {
        if (field) {
            field.required = !isPickup;
        }
    });
}

function renderStoreDeliveryCard(option, selectedId, totalProducts, selectable) {
    const calculated = {
        ...option,
        valor: calculateStoreDeliveryValue(option, totalProducts)
    };
    const isSelected = selectedId === option.id;
    const shippingText = formatStoreShippingValue(calculated.valor);
    const freeText = option.freteGratisAcimaDe > 0
        ? `Grátis acima de ${storeCurrency.format(option.freteGratisAcimaDe)}`
        : "Valor fixo";
    const button = selectable
        ? `<button class="ghost-button" type="button" data-select-delivery="${option.id}">${isSelected ? "Selecionada" : "Escolher"}</button>`
        : "";

    return `
        <article class="delivery-option ${isSelected ? "is-selected" : ""}">
            <div>
                <strong>${escapeStoreHtml(option.nome)}</strong>
                <span>${escapeStoreHtml(formatStoreDeliveryType(option.tipo))}${option.descricao ? ` · ${escapeStoreHtml(option.descricao)}` : ""}</span>
                <span>${option.prazoMinimoDias} a ${option.prazoMaximoDias} dias · ${freeText}</span>
            </div>
            <div>
                <strong>${shippingText}</strong>
                ${button}
            </div>
        </article>
    `;
}

function renderStoreFallbackShipping(config) {
    const baseText = storeCurrency.format(Number(config.freteValorPadrao || 0));
    const freeText = Number(config.freteGratisAcimaDe || 0) > 0
        ? `Grátis acima de ${storeCurrency.format(Number(config.freteGratisAcimaDe || 0))}`
        : "Valor combinado com a loja";

    return `
        <div class="shipping-line">
            <span>Valor base</span>
            <strong>${baseText}</strong>
        </div>
        <div class="shipping-line">
            <span>Frete grátis</span>
            <strong>${freeText}</strong>
        </div>
        <div class="shipping-line">
            <span>Prazo estimado</span>
            <strong>${config.prazoMinimoDias} a ${config.prazoMaximoDias} dias</strong>
        </div>
    `;
}

function renderStorePaymentOptions() {
    const config = getStoreConfig();
    const pixActive = config.pixOnlineAtivo !== false;
    const cardActive = config.cartaoOnlineAtivo !== false;

    storeEls.pixPaymentOption.classList.toggle("hidden", !pixActive);
    storeEls.cardPaymentOption.classList.toggle("hidden", !cardActive);
    storeEls.pixPaymentInput.disabled = !pixActive;
    storeEls.cardPaymentInput.disabled = !cardActive;

    const selected = document.querySelector("input[name='storePayment']:checked");
    if (!selected || selected.disabled) {
        if (pixActive) {
            storeEls.pixPaymentInput.checked = true;
            return;
        }

        if (cardActive) {
            storeEls.cardPaymentInput.checked = true;
        }
    }
}

function renderStorePaymentHelp() {
    const selectedPayment = document.querySelector("input[name='storePayment']:checked")?.value || "Pix";
    const config = getStoreConfig();

    if (selectedPayment === "Pix") {
        const pixKey = config.pixChave || "Chave Pix ainda não configurada";
        const gatewayText = config.gatewayPagamentoAtivo
            ? `QR Code Pix automático por ${config.gatewayPagamentoProvedor || "gateway configurado"}.`
            : "Confirmação manual pela loja.";
        storeEls.paymentHelp.innerHTML = `
            <strong>Pagamento via Pix</strong>
            <span>Finalize o pedido para gerar o QR Code e o Pix copia e cola com valor fechado.</span>
            ${config.gatewayPagamentoAtivo ? "" : `<code>${escapeStoreHtml(pixKey)}</code>`}
            <span>${escapeStoreHtml(gatewayText)}</span>
            <span>${escapeStoreHtml(config.mensagemPagamento || defaultStoreConfig.mensagemPagamento)}</span>
        `;
        return;
    }

    const cardGatewayText = config.gatewayPagamentoAtivo
        ? `Gateway ${config.gatewayPagamentoProvedor || "configurado"} em ${config.gatewayPagamentoProducao ? "produção" : "teste"}.`
        : "";
    storeEls.paymentHelp.innerHTML = `
        <strong>Pagamento no cartão</strong>
        <span>${escapeStoreHtml(config.mensagemPagamentoCartao || defaultStoreConfig.mensagemPagamentoCartao)}</span>
        ${cardGatewayText ? `<span>${escapeStoreHtml(cardGatewayText)}</span>` : ""}
        ${config.checkoutCartaoUrl ? `<code>${escapeStoreHtml(config.checkoutCartaoNome || defaultStoreConfig.checkoutCartaoNome)}</code>` : ""}
    `;
}

function renderStoreOrders() {
    const isLogged = Boolean(storeState.customer?.email);
    const orders = storeState.orders || [];

    if (!isLogged) {
        storeEls.customerOrdersSummary.textContent = "Acesso do cliente";
        storeEls.customerOrders.innerHTML = `
            <div class="orders-empty">
                <p class="store-eyebrow">Conta necessária</p>
                <h3>Entre para ver seus pedidos</h3>
                <span>O histórico aparece aqui depois que você acessa sua conta Nana Modas.</span>
                <button class="checkout-button" type="button" data-orders-action="login">Entrar na conta</button>
            </div>
        `;
        return;
    }

    storeEls.customerOrdersSummary.textContent = `${orders.length} ${orders.length === 1 ? "pedido" : "pedidos"}`;

    if (!orders.length) {
        storeEls.customerOrders.innerHTML = `
            <div class="orders-empty">
                <p class="store-eyebrow">Sem compras ainda</p>
                <h3>Nenhum pedido encontrado</h3>
                <span>Quando você finalizar uma compra online, ela aparecerá aqui com status e detalhes.</span>
                <button class="checkout-button" type="button" data-orders-action="catalog">Ver coleção</button>
            </div>
        `;
        return;
    }

    storeEls.customerOrders.innerHTML = orders.map((order) => {
        const shortId = order.id.slice(0, 8).toUpperCase();
        const timeline = renderStoreOrderTimeline(order);
        const items = order.itens.map((item) => {
            const variation = formatStoreVariation(item);
            return `
                <li>
                    <span>${item.quantidade}x ${escapeStoreHtml(item.produtoNome)}${variation ? ` · ${escapeStoreHtml(variation)}` : ""}</span>
                    <strong>${storeCurrency.format(item.subtotal)}</strong>
                </li>
            `;
        }).join("");

        return `
            <article class="customer-order-card">
                <div class="order-card-head">
                    <div>
                        <span>Pedido ${shortId}</span>
                        <strong>${formatStoreDate(order.criadoEm)}</strong>
                    </div>
                    <span class="order-status">${formatStoreOrderStatus(order.status)}</span>
                </div>

                <ul class="order-items">${items}</ul>

                <div class="order-card-foot">
                    <div>
                        <span>Pagamento</span>
                        <strong>${formatStorePayment(order.formaPagamento)}</strong>
                    </div>
                    ${order.desconto ? `
                        <div>
                            <span>Cupom</span>
                            <strong>${escapeStoreHtml(order.cupomCodigo || "Aplicado")} (- ${storeCurrency.format(order.desconto)})</strong>
                        </div>
                    ` : ""}
                    ${order.entregaNome ? `
                        <div>
                            <span>Entrega</span>
                            <strong>${escapeStoreHtml(order.entregaNome)} · ${formatStoreShippingValue(order.entregaValor)}</strong>
                        </div>
                    ` : ""}
                    <div>
                        <span>Total</span>
                        <strong>${storeCurrency.format(order.total)}</strong>
                    </div>
                </div>

                <p class="order-address">${escapeStoreHtml(order.enderecoEntrega)}</p>
                ${timeline}
                ${order.codigoRastreio || order.observacaoEntrega ? `
                    <div class="order-tracking">
                        <span>Acompanhamento</span>
                        ${order.codigoRastreio ? `<strong>${escapeStoreHtml(order.codigoRastreio)}</strong>` : ""}
                        ${order.observacaoEntrega ? `<p>${escapeStoreHtml(order.observacaoEntrega)}</p>` : ""}
                        ${order.rastreamentoAtualizadoEm ? `<small>Atualizado em ${formatStoreDate(order.rastreamentoAtualizadoEm)}</small>` : ""}
                    </div>
                ` : ""}
                ${order.pagamentoConfirmadoEm || order.referenciaPagamento || order.observacaoPagamento ? `
                    <div class="order-tracking">
                        <span>Pagamento</span>
                        ${order.pixQrCodeBase64 && !order.pagamentoConfirmadoEm ? `<img class="pix-qr-image order-card-qr" src="${normalizeStorePixQrCode(order.pixQrCodeBase64)}" alt="QR Code Pix do pedido">` : ""}
                        ${order.pagamentoConfirmadoEm ? `<strong>Confirmado em ${formatStoreDate(order.pagamentoConfirmadoEm)}</strong>` : ""}
                        ${order.pixCopiaECola && !order.pagamentoConfirmadoEm ? `<code>${escapeStoreHtml(order.pixCopiaECola)}</code>` : ""}
                        ${order.referenciaPagamento ? `<p>${escapeStoreHtml(order.referenciaPagamento)}</p>` : ""}
                        ${order.observacaoPagamento ? `<p>${escapeStoreHtml(order.observacaoPagamento)}</p>` : ""}
                    </div>
                ` : ""}
                <div class="order-card-actions">
                    <button class="ghost-button" type="button" data-orders-action="copy" data-order-id="${order.id}">Copiar pedido</button>
                    <button class="ghost-button" type="button" data-orders-action="receipt" data-order-id="${order.id}">Imprimir recibo</button>
                    <button class="ghost-button whatsapp-link" type="button" data-orders-action="whatsapp" data-order-id="${order.id}">${renderWhatsappLabel("WhatsApp")}</button>
                    <button class="ghost-button" type="button" data-orders-action="email" data-order-id="${order.id}">E-mail</button>
                </div>
            </article>
        `;
    }).join("");
}

function renderStoreOrderTimeline(order) {
    const steps = ["Recebido", "Pago", "Separando", "Enviado", "Entregue"];
    const status = order.status || "Recebido";
    const currentIndex = steps.indexOf(status);
    const isCanceled = status === "Cancelado";

    const markers = steps.map((step, index) => {
        const done = !isCanceled && currentIndex >= index;
        const active = !isCanceled && currentIndex === index;
        return `
            <li class="${done ? "is-done" : ""} ${active ? "is-active" : ""}">
                <span>${index + 1}</span>
                <strong>${formatStoreOrderStatus(step)}</strong>
            </li>
        `;
    }).join("");

    return `
        <ol class="order-timeline ${isCanceled ? "is-canceled" : ""}" aria-label="Acompanhamento do pedido">
            ${markers}
            ${isCanceled ? `
                <li class="is-canceled-step">
                    <span>!</span>
                    <strong>Cancelado</strong>
                </li>
            ` : ""}
        </ol>
    `;
}

function renderStoreCategories() {
    const buttons = [
        `<button class="category-tab ${storeState.categoryId === "all" ? "is-active" : ""}" type="button" data-category="all">Todos</button>`,
        ...storeState.categories.map((category) => `
            <button class="category-tab ${storeState.categoryId === category.id ? "is-active" : ""}" type="button" data-category="${category.id}">
                ${escapeStoreHtml(category.nome)}
            </button>
        `)
    ];

    storeEls.categories.innerHTML = buttons.join("");
}

function renderStoreVariationFilters() {
    const sizes = collectProductOptions("tamanhos");
    const colors = collectProductOptions("cores");

    storeEls.sizeFilter.innerHTML = [
        `<option value="all">Todos</option>`,
        ...sizes.map((size) => `<option value="${escapeStoreHtml(size)}">${escapeStoreHtml(size)}</option>`)
    ].join("");

    storeEls.colorFilter.innerHTML = [
        `<option value="all">Todas</option>`,
        ...colors.map((color) => `<option value="${escapeStoreHtml(color)}">${escapeStoreHtml(color)}</option>`)
    ].join("");

    storeEls.sizeFilter.value = sizes.includes(storeState.sizeFilter) ? storeState.sizeFilter : "all";
    storeEls.colorFilter.value = colors.includes(storeState.colorFilter) ? storeState.colorFilter : "all";
    storeState.sizeFilter = storeEls.sizeFilter.value;
    storeState.colorFilter = storeEls.colorFilter.value;
}

function renderStoreProducts() {
    const products = getFilteredStoreProducts();
    storeEls.productCount.textContent = storeState.products.length;
    storeEls.catalogTitle.textContent = storeState.catalogTitle;
    storeEls.catalogSubtitle.textContent = products.length
        ? `${storeState.catalogSubtitle} ${products.length} ${products.length === 1 ? "peça encontrada" : "peças encontradas"}.`
        : storeState.catalogSubtitle;
    storeEls.products.innerHTML = products.length
        ? products.map((product) => renderStoreProductCard(product)).join("")
        : `<div class="empty-state">Nenhuma peça disponível nessa busca.</div>`;
}

function renderStoreHome() {
    const availableProducts = storeState.products.filter((product) => product.quantidadeEmEstoque > 0);
    const featured = availableProducts.filter((product) => product.destaqueLoja);
    const featuredProducts = (featured.length ? featured : availableProducts).slice(0, 8);
    const launchProducts = [...availableProducts]
        .sort((first, second) => (first.ordemLoja || 0) - (second.ordemLoja || 0))
        .slice(0, 8);

    storeEls.homeFeaturedProducts.innerHTML = featuredProducts.length
        ? featuredProducts.map((product) => renderStoreProductCard(product, true)).join("")
        : `<div class="empty-state">Marque produtos como destaque no painel para preencher esta vitrine.</div>`;

    storeEls.homeLaunchProducts.innerHTML = launchProducts.length
        ? launchProducts.map((product) => renderStoreProductCard(product, true)).join("")
        : `<div class="empty-state">Publique produtos na loja para aparecerem aqui.</div>`;

    renderStoreHomeCategories();
}

function renderStoreHomeCategories() {
    const categories = storeState.categories.slice(0, 6);
    storeEls.homeCategoryGrid.innerHTML = categories.length
        ? categories.map((category) => {
            const count = storeState.products.filter((product) => product.categoriaId === category.id && product.quantidadeEmEstoque > 0).length;
            return `
                <button class="home-category-card" type="button" data-home-category="${category.id}" data-home-category-name="${escapeStoreHtml(category.nome)}">
                    <span>${escapeStoreHtml(category.nome)}</span>
                    <strong>${count} ${count === 1 ? "peça" : "peças"}</strong>
                </button>
            `;
        }).join("")
        : `<button class="home-category-card" type="button" data-home-category="all"><span>Produtos</span><strong>Ver coleção</strong></button>`;
}

function renderStoreProductCard(product, compact = false) {
    const inCart = getStoreCartQuantity(product.id);
    const available = Math.max(product.quantidadeEmEstoque - inCart, 0);
    const hasVariations = productHasVariations(product);
    const optionSummary = getStoreProductOptionSummary(product);
    const initials = getStoreProductInitials(product.nome);
    const visual = product.imagemUrl
        ? `<img src="${escapeStoreHtml(product.imagemUrl)}" alt="${escapeStoreHtml(product.nome)}">`
        : `<span>${escapeStoreHtml(initials)}</span>`;
    const stockClass = available <= 3 ? "stock-pill is-low" : "stock-pill";
    const badgeText = product.destaqueLoja ? "Destaque" : available <= 3 ? "Últimas peças" : "Pronta entrega";
    const trustNote = available > 0 ? "Disponível para compra online" : "Produto esgotado no momento";

    return `
        <article class="product-card ${compact ? "is-home-card" : ""}">
            <button class="product-open" type="button" data-open-product="${product.id}" aria-label="Ver detalhes de ${escapeStoreHtml(product.nome)}">
                <div class="product-visual">
                    ${visual}
                    <span class="product-badge">${badgeText}</span>
                </div>
                <div>
                    <h3>${escapeStoreHtml(product.nome)}</h3>
                    <p>${escapeStoreHtml(product.descricao || product.categoria)}</p>
                </div>
            </button>
            <div class="product-card-notes">
                <span>${escapeStoreHtml(optionSummary || trustNote)}</span>
                <span>Estoque integrado</span>
            </div>
            <div class="product-meta">
                <span class="price">${storeCurrency.format(product.preco)}</span>
                <span class="${stockClass}">${available} un.</span>
            </div>
            <div class="product-card-actions">
                <button class="ghost-button" type="button" data-open-product="${product.id}">Ver detalhes</button>
                <button class="add-button" type="button" ${hasVariations ? `data-open-product="${product.id}"` : `data-add="${product.id}"`} ${available === 0 ? "disabled" : ""}>${hasVariations ? "Escolher" : "Adicionar"}</button>
            </div>
        </article>
    `;
}

function getStoreProductOptionSummary(product) {
    const parts = [
        product.tamanhos?.length ? `${product.tamanhos.length} tam.` : "",
        product.cores?.length ? `${product.cores.length} cores` : "",
        product.modelos?.length ? `${product.modelos.length} modelos` : ""
    ].filter(Boolean);
    return parts.join(" · ");
}

function getFilteredStoreProducts() {
    const term = normalizeStore(storeState.search);
    const minPrice = Number(storeState.minPrice || 0);
    const maxPrice = Number(storeState.maxPrice || 0);

    const products = storeState.products
        .filter((product) => storeState.availabilityFilter === "all" || product.quantidadeEmEstoque > 0)
        .filter((product) => storeState.categoryId === "all" || product.categoriaId === storeState.categoryId)
        .filter((product) => storeState.availabilityFilter !== "low" || product.quantidadeEmEstoque <= 3)
        .filter((product) => !minPrice || product.preco >= minPrice)
        .filter((product) => !maxPrice || product.preco <= maxPrice)
        .filter((product) => storeState.sizeFilter === "all" || (product.tamanhos || []).includes(storeState.sizeFilter))
        .filter((product) => storeState.colorFilter === "all" || (product.cores || []).includes(storeState.colorFilter))
        .filter((product) => normalizeStore(`${product.nome} ${product.sku || ""} ${product.categoria} ${product.descricao || ""} ${(product.tamanhos || []).join(" ")} ${(product.cores || []).join(" ")} ${(product.modelos || []).join(" ")}`).includes(term));

    if (storeState.collection === "launches") {
        return [...products].sort((first, second) => (first.ordemLoja || 0) - (second.ordemLoja || 0));
    }

    return products;
}

function handleStoreProductListClick(event) {
    const button = event.target.closest("[data-add]");
    if (button) {
        addStoreProduct(button.dataset.add);
        return;
    }

    const productButton = event.target.closest("[data-open-product]");
    if (productButton) {
        openStoreProduct(productButton.dataset.openProduct);
    }
}

function findStoreCategoryByName(name) {
    const normalizedName = normalizeStore(name);
    return storeState.categories.find((category) => normalizeStore(category.nome) === normalizedName)
        || storeState.categories.find((category) => normalizeStore(category.nome).includes(normalizedName) || normalizedName.includes(normalizeStore(category.nome)));
}

function renderStoreProductDetail() {
    const product = getSelectedStoreProduct();
    if (!product) {
        storeEls.productDetail.innerHTML = `<div class="empty-state">Produto não encontrado.</div>`;
        return;
    }

    const images = getStoreProductImages(product);
    const selectedImage = images.includes(storeState.selectedImageUrl) ? storeState.selectedImageUrl : images[0] || null;
    storeState.selectedImageUrl = selectedImage;
    const initials = getStoreProductInitials(product.nome);
    const inCart = getStoreCartQuantity(product.id);
    const available = Math.max(product.quantidadeEmEstoque - inCart, 0);
    const stockClass = available <= 3 ? "stock-pill is-low" : "stock-pill";
    const mainVisual = selectedImage
        ? `<img src="${escapeStoreHtml(selectedImage)}" alt="${escapeStoreHtml(product.nome)}">`
        : `<span>${escapeStoreHtml(initials)}</span>`;
    const thumbs = images.length
        ? images.map((image, index) => `
            <button class="detail-thumb ${image === selectedImage ? "is-active" : ""}" type="button" data-detail-image-index="${index}" aria-label="Ver foto ${index + 1} de ${escapeStoreHtml(product.nome)}">
                <img src="${escapeStoreHtml(image)}" alt="">
            </button>
        `).join("")
        : `<div class="detail-thumb is-empty">${escapeStoreHtml(initials)}</div>`;
    const variationControls = renderDetailVariationControls(product);
    const sizeGuide = product.guiaMedidas
        ? `<div class="size-guide">
                <h3>Guia de medidas</h3>
                ${formatStoreText(product.guiaMedidas)}
            </div>`
        : "";
    const variantStock = renderStoreVariantStock(product);
    const commercialBadges = getStoreProductCommercialBadges(product, available);
    const relatedProducts = getRelatedStoreProducts(product);
    const related = relatedProducts.length
        ? `<div class="related-products">
                <h3>Você também pode gostar</h3>
                <div>
                    ${relatedProducts.map((relatedProduct) => `
                        <button type="button" data-open-product="${relatedProduct.id}">
                            <span>${escapeStoreHtml(relatedProduct.nome)}</span>
                            <strong>${storeCurrency.format(relatedProduct.preco)}</strong>
                        </button>
                    `).join("")}
                </div>
            </div>`
        : "";

    storeEls.productDetail.innerHTML = `
        <button class="detail-back" type="button" data-detail-action="back">Voltar à coleção</button>
        <div class="product-detail-layout">
            <section class="detail-gallery" aria-label="Fotos do produto">
                <div class="detail-main-image">${mainVisual}</div>
                <div class="detail-thumbs">${thumbs}</div>
            </section>

            <section class="detail-info">
                <p class="store-eyebrow">${escapeStoreHtml(product.categoria)}</p>
                <h2>${escapeStoreHtml(product.nome)}</h2>
                <div class="detail-commercial-badges">
                    ${commercialBadges.map((item) => `<span>${escapeStoreHtml(item)}</span>`).join("")}
                </div>
                <p class="detail-description">${escapeStoreHtml(product.descricao || "Peça selecionada da curadoria Nana Modas.")}</p>

                <div class="detail-price-row">
                    <strong>${storeCurrency.format(product.preco)}</strong>
                    <span class="${stockClass}">${available} disponíveis</span>
                </div>

                ${variationControls}
                ${variantStock}

                <dl class="detail-specs">
                    <div>
                        <dt>Código</dt>
                        <dd>${escapeStoreHtml(product.sku || product.id.slice(0, 8).toUpperCase())}</dd>
                    </div>
                    <div>
                        <dt>Categoria</dt>
                        <dd>${escapeStoreHtml(product.categoria)}</dd>
                    </div>
                    <div>
                        <dt>Pagamento</dt>
                        <dd>Pix ou cartão</dd>
                    </div>
                    <div>
                        <dt>Estoque</dt>
                        <dd>${product.quantidadeEmEstoque} unidades</dd>
                    </div>
                    <div>
                        <dt>Compra</dt>
                        <dd>Online com confirmação</dd>
                    </div>
                    <div>
                        <dt>Atendimento</dt>
                        <dd>Direto com a Nana Modas</dd>
                    </div>
                </dl>

                <div class="detail-actions">
                    <button class="add-button" type="button" data-add="${product.id}" ${available === 0 ? "disabled" : ""}>Adicionar à sacola</button>
                    <button class="ghost-button" type="button" data-detail-action="back">Continuar vendo peças</button>
                </div>

                ${sizeGuide}
                <div class="detail-assurance">
                    <div>
                        <strong>Estoque real</strong>
                        <span>O produto baixa automaticamente quando vender no site ou no PDV.</span>
                    </div>
                    <div>
                        <strong>Compra acompanhada</strong>
                        <span>O cliente pode consultar o status em Meus pedidos.</span>
                    </div>
                    <div>
                        <strong>Atendimento próximo</strong>
                        <span>Dúvidas, entrega e pagamento podem ser confirmados com a loja.</span>
                    </div>
                </div>
                ${related}
            </section>
        </div>
    `;
}

function getStoreProductCommercialBadges(product, available) {
    const badges = [
        product.destaqueLoja ? "Destaque da loja" : "Curadoria Nana Modas",
        available <= 3 ? "Últimas peças" : "Pronta entrega",
        productHasVariations(product) ? "Com variações" : "Compra rápida"
    ];
    return [...new Set(badges)];
}

function renderStoreCart() {
    const totalItems = storeState.cart.reduce((sum, item) => sum + item.quantidade, 0);
    const subtotal = getStoreCartSubtotal();
    const coupon = getStoreCoupon();
    const selectedDelivery = getSelectedStoreDelivery();
    const total = getStoreOrderTotal();

    storeEls.cartCount.textContent = totalItems;
    storeEls.headerCartCount.textContent = totalItems;
    storeEls.tabCartCount.textContent = totalItems;
    storeEls.cartSubtotal.textContent = storeCurrency.format(subtotal);
    storeEls.cartDiscountRow.classList.toggle("hidden", coupon.discount <= 0);
    storeEls.cartDiscount.textContent = `- ${storeCurrency.format(coupon.discount)}`;
    storeEls.cartShippingRow.classList.toggle("hidden", !selectedDelivery);
    storeEls.cartShipping.textContent = selectedDelivery ? formatStoreShippingValue(selectedDelivery.valor) : storeCurrency.format(0);
    storeEls.cartTotal.textContent = storeCurrency.format(total);
    storeEls.couponMessage.textContent = coupon.message;
    storeEls.couponMessage.classList.toggle("is-error", Boolean(coupon.invalid));
    storeEls.couponMessage.classList.toggle("is-success", coupon.discount > 0);
    storeEls.checkoutButton.disabled = storeState.cart.length === 0 || coupon.invalid;
    storeEls.clearCart.disabled = storeState.cart.length === 0;

    storeEls.cartList.innerHTML = storeState.cart.length
        ? storeState.cart.map((item) => {
            const product = storeState.products.find((candidate) => candidate.id === item.produtoId);
            if (!product) {
                return "";
            }

            const variation = formatStoreVariation(item);
            return `
                <div class="cart-item">
                    <div class="cart-line">
                        <div>
                            <strong>${escapeStoreHtml(product.nome)}</strong>
                            <span>${storeCurrency.format(product.preco)} cada</span>
                            ${variation ? `<span>${escapeStoreHtml(variation)}</span>` : ""}
                        </div>
                        <strong>${storeCurrency.format(product.preco * item.quantidade)}</strong>
                    </div>
                    <div class="cart-line">
                        <div class="cart-actions">
                            <button type="button" data-cart-action="decrease" data-id="${escapeStoreHtml(item.id)}" title="Diminuir">-</button>
                            <span>${item.quantidade}</span>
                            <button type="button" data-cart-action="increase" data-id="${escapeStoreHtml(item.id)}" title="Aumentar">+</button>
                        </div>
                        <button class="clear-cart" type="button" data-cart-action="remove" data-id="${escapeStoreHtml(item.id)}">Remover</button>
                    </div>
                </div>
            `;
        }).join("")
        : `<div class="empty-state">Sua sacola está vazia.</div>`;
}

function applyStoreCoupon() {
    storeState.couponCode = storeEls.couponCode.value.trim().toUpperCase();
    renderStoreCart();
    renderStoreShipping();
}

function addStoreProduct(productId, variation = null) {
    const product = storeState.products.find((item) => item.id === productId);
    if (!product) {
        return;
    }

    const selectedVariation = normalizeStoreVariation(variation);
    const validation = validateProductVariation(product, selectedVariation);
    if (validation) {
        showStoreToast(validation);
        openStoreProduct(product.id);
        return;
    }

    const currentQuantity = getStoreCartQuantity(productId, selectedVariation);
    const availableQuantity = getStoreAvailableQuantity(product, selectedVariation);

    if (currentQuantity >= availableQuantity) {
        showStoreToast("Quantidade máxima disponível no estoque.");
        return;
    }

    const itemKey = getStoreCartItemKey(product.id, selectedVariation);
    const item = storeState.cart.find((cartItem) => cartItem.id === itemKey);

    if (item) {
        item.quantidade += 1;
    } else {
        storeState.cart.push({
            id: itemKey,
            produtoId: productId,
            quantidade: 1,
            ...selectedVariation
        });
    }

    renderStoreCart();
    renderStoreShipping();
    renderStoreProducts();
    renderStoreHome();
    renderStoreProductDetail();
    showStoreToast("Peça adicionada à sacola.");
}

function changeStoreCart(itemKey, action) {
    const item = storeState.cart.find((cartItem) => cartItem.id === itemKey);
    const product = storeState.products.find((candidate) => candidate.id === item?.produtoId);

    if (!item || !product) {
        return;
    }

    if (action === "increase") {
        const variation = normalizeStoreVariation(item);
        if (getStoreCartQuantity(product.id, variation) >= getStoreAvailableQuantity(product, variation)) {
            showStoreToast("Quantidade máxima disponível no estoque.");
            return;
        }

        item.quantidade += 1;
    }

    if (action === "decrease") {
        item.quantidade -= 1;
    }

    if (action === "remove" || item.quantidade <= 0) {
        storeState.cart = storeState.cart.filter((cartItem) => cartItem.id !== itemKey);
    }

    renderStoreCart();
    renderStoreShipping();
    renderStoreProducts();
    renderStoreHome();
    renderStoreProductDetail();
}

function openStoreProduct(productId) {
    const product = storeState.products.find((item) => item.id === productId);
    if (!product) {
        showStoreToast("Produto não encontrado.");
        return;
    }

    storeState.selectedProductId = product.id;
    storeState.selectedImageUrl = getStoreProductImages(product)[0] || null;
    showStoreView("product");
    renderStoreProductDetail();
    window.scrollTo({ top: 0, behavior: "smooth" });
}

async function submitStoreOrder(event) {
    event.preventDefault();

    if (storeState.cart.length === 0) {
        showStoreToast("Adicione uma peça à sacola.");
        return;
    }

    const formaPagamento = document.querySelector("input[name='storePayment']:checked").value;
    const config = getStoreConfig();
    if ((formaPagamento === "Pix" && config.pixOnlineAtivo === false) ||
        (formaPagamento === "CartaoCredito" && config.cartaoOnlineAtivo === false)) {
        showStoreToast("Essa forma de pagamento não está disponível agora.");
        return;
    }

    if (formaPagamento === "Pix" && config.gatewayPagamentoAtivo && !isValidStoreDocument(storeEls.customerDocument.value)) {
        showStoreToast("Informe CPF ou CNPJ para gerar o QR Code Pix.");
        return;
    }

    const address = buildStoreAddress();
    const coupon = getStoreCoupon();
    const selectedDelivery = getSelectedStoreDelivery();
    if (coupon.invalid) {
        showStoreToast(coupon.message);
        return;
    }

    const payload = {
        nomeCliente: storeEls.customerName.value.trim(),
        emailCliente: storeEls.customerEmail.value.trim(),
        telefoneCliente: emptyStoreToNull(storeEls.customerPhone.value),
        documentoCliente: emptyStoreToNull(storeEls.customerDocument.value),
        enderecoEntrega: address,
        cepEntrega: emptyStoreToNull(storeEls.customerCep.value),
        ruaEntrega: emptyStoreToNull(storeEls.customerStreet.value),
        numeroEntrega: emptyStoreToNull(storeEls.customerNumber.value),
        complementoEntrega: emptyStoreToNull(storeEls.customerComplement.value),
        bairroEntrega: emptyStoreToNull(storeEls.customerDistrict.value),
        cidadeEntrega: emptyStoreToNull(storeEls.customerCity.value),
        estadoEntrega: emptyStoreToNull(storeEls.customerState.value)?.toUpperCase(),
        observacao: emptyStoreToNull(storeEls.customerOrderNote.value),
        cupomCodigo: coupon.code,
        opcaoEntregaId: selectedDelivery?.id || null,
        formaPagamento,
        itens: storeState.cart.map((item) => ({
            produtoId: item.produtoId,
            quantidade: item.quantidade,
            tamanho: item.tamanho || null,
            cor: item.cor || null,
            modelo: item.modelo || null
        }))
    };

    try {
        storeEls.checkoutButton.disabled = true;
        const order = await storeApi("/pedidos-online", {
            method: "POST",
            body: JSON.stringify(payload)
        });

        storeState.cart = [];
        storeState.shippingQuote = null;
        storeState.couponCode = "";
        storeState.lastOrder = order;
        storeEls.checkoutForm.reset();
        storeEls.couponCode.value = "";
        renderStoreOrderConfirmation(order);
        storeEls.orderModal.classList.remove("hidden");
        showStoreView("catalog");
        await loadStore();
    } catch (error) {
        showStoreToast(error.message);
    } finally {
        renderStoreCart();
    }
}

function renderStoreOrderConfirmation(order) {
    const config = getStoreConfig();
    const shortId = order.id.slice(0, 8).toUpperCase();
    const isPix = order.formaPagamento === "Pix";
    const isCard = order.formaPagamento === "CartaoCredito";
    const pixKey = config.pixChave || "";
    const pixPayload = isPix ? (order.pixCopiaECola || buildPixPayload(order, config)) : "";
    const pixQrCode = isPix ? normalizeStorePixQrCode(order.pixQrCodeBase64) : "";
    const cardPaymentUrl = isCard ? buildStoreCardPaymentUrl(order, config) : "";
    const statusText = formatStoreOrderStatus(order.status);

    storeEls.orderMessage.textContent = isPix
        ? (order.gatewayPagamentoId
            ? "Pedido recebido. Pague pelo QR Code Pix ou copie o código abaixo; a confirmação cai automaticamente."
            : "Pedido recebido. Copie o Pix com valor fechado e envie o comprovante para a loja confirmar o pagamento.")
        : isCard
            ? "Pedido recebido. Use o pagamento online configurado ou aguarde a loja enviar a cobrança."
            : "Pedido recebido. A loja vai confirmar a forma de pagamento e atualizar o status.";

    storeEls.orderSummary.innerHTML = `
        <div>
            <span>Pedido</span>
            <strong>${shortId}</strong>
        </div>
        <div>
            <span>Status</span>
            <strong>${statusText}</strong>
        </div>
        ${order.desconto ? `
            <div>
                <span>Cupom</span>
                <strong>${escapeStoreHtml(order.cupomCodigo || "Aplicado")}</strong>
            </div>
            <div>
                <span>Desconto</span>
                <strong>- ${storeCurrency.format(order.desconto)}</strong>
            </div>
        ` : ""}
        ${order.entregaNome ? `
            <div>
                <span>Entrega</span>
                <strong>${escapeStoreHtml(order.entregaNome)} · ${formatStoreShippingValue(order.entregaValor)}</strong>
            </div>
        ` : ""}
        <div>
            <span>Total</span>
            <strong>${storeCurrency.format(order.total)}</strong>
        </div>
        <div>
            <span>Pagamento</span>
            <strong>${formatStorePayment(order.formaPagamento)}</strong>
        </div>
    `;

    storeEls.orderPaymentBox.classList.toggle("hidden", !isPix);
    storeEls.orderPixQrCode.classList.toggle("hidden", !pixQrCode);
    storeEls.orderPixQrCode.src = pixQrCode || "";
    storeEls.copyPixKeyButton.disabled = !pixPayload;
    storeEls.orderPixKey.textContent = pixPayload || pixKey || "Chave Pix ainda não configurada";
    storeEls.orderPixInstructions.textContent = order.gatewayPagamentoId
        ? `Pix automático ${order.gatewayPagamentoStatus ? `(${order.gatewayPagamentoStatus})` : ""}${order.pixExpiraEm ? ` · válido até ${formatStoreDate(order.pixExpiraEm)}` : ""}.`
        : (config.mensagemPagamento || defaultStoreConfig.mensagemPagamento);

    storeEls.orderCardPaymentBox.classList.toggle("hidden", !isCard);
    storeEls.openCardPaymentButton.disabled = !cardPaymentUrl;
    storeEls.openCardPaymentButton.dataset.paymentUrl = cardPaymentUrl;
    storeEls.orderCardPaymentTitle.textContent = "Pagamento no cartão";
    storeEls.orderCardPaymentName.textContent = config.checkoutCartaoNome || defaultStoreConfig.checkoutCartaoNome;
    storeEls.orderCardPaymentInstructions.textContent = cardPaymentUrl
        ? (config.mensagemPagamentoCartao || defaultStoreConfig.mensagemPagamentoCartao)
        : "A loja ainda não configurou um link online. Aguarde o envio da cobrança pelo atendimento.";
}

async function copyStoreOrderSummary(order) {
    try {
        await navigator.clipboard.writeText(buildStoreOrderSummary(order));
        showStoreToast("Resumo do pedido copiado.");
    } catch {
        showStoreToast("Não foi possível copiar automaticamente.");
    }
}

function shareStoreOrderWhatsApp(order) {
    const text = buildStoreOrderSummary(order);
    window.open(`https://wa.me/?text=${encodeURIComponent(text)}`, "_blank", "noopener");
}

function shareStoreOrderEmail(order) {
    const shortId = order.id.slice(0, 8).toUpperCase();
    const recipient = String(order.emailCliente || "").trim();
    const subject = `Pedido Nana Modas ${shortId}`;
    const body = buildStoreOrderSummary(order);
    const mailto = `mailto:${encodeURIComponent(recipient)}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
    window.location.href = mailto;
}

function printStoreOrderReceipt(order) {
    const receiptWindow = window.open("", "_blank", "width=720,height=900");
    if (!receiptWindow) {
        showStoreToast("Permita pop-ups para imprimir o recibo.");
        return;
    }

    receiptWindow.opener = null;
    receiptWindow.document.open();
    receiptWindow.document.write(buildStoreReceiptHtml(order));
    receiptWindow.document.close();
    receiptWindow.focus();
    window.setTimeout(() => {
        receiptWindow.print();
    }, 350);
}

function buildStoreReceiptHtml(order) {
    const shortId = order.id.slice(0, 8).toUpperCase();
    const items = (order.itens || []).map((item) => {
        const variation = formatStoreVariation(item);
        return `
            <tr>
                <td>
                    <strong>${escapeStoreHtml(item.produtoNome)}</strong>
                    ${variation ? `<span>${escapeStoreHtml(variation)}</span>` : ""}
                </td>
                <td>${item.quantidade}</td>
                <td>${storeCurrency.format(item.precoUnitario || 0)}</td>
                <td>${storeCurrency.format(item.subtotal || 0)}</td>
            </tr>
        `;
    }).join("");

    const paymentDetail = [
        order.pagamentoConfirmadoEm ? `Confirmado em ${formatStoreDate(order.pagamentoConfirmadoEm)}` : null,
        order.referenciaPagamento,
        order.observacaoPagamento
    ].filter(Boolean).join(" · ");

    return `<!doctype html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8">
    <title>Recibo Nana Modas ${shortId}</title>
    <style>
        * { box-sizing: border-box; }
        body {
            margin: 0;
            background: #f4efe6;
            color: #171008;
            font-family: Inter, Arial, sans-serif;
            font-size: 14px;
            line-height: 1.45;
        }
        main {
            width: min(760px, calc(100vw - 28px));
            margin: 24px auto;
            border: 1px solid #d7b46a;
            background: #fffaf0;
            padding: 28px;
        }
        header {
            display: flex;
            justify-content: space-between;
            gap: 18px;
            border-bottom: 2px solid #d7b46a;
            padding-bottom: 18px;
        }
        h1, h2, p { margin: 0; }
        h1 {
            color: #6f4d18;
            font-family: Georgia, "Times New Roman", serif;
            font-size: 30px;
            text-transform: uppercase;
        }
        header span, .muted, th {
            color: #766a5a;
            font-size: 12px;
            font-weight: 800;
            text-transform: uppercase;
        }
        .badge {
            align-self: start;
            border: 1px solid #d7b46a;
            background: #f3e0ad;
            color: #5c3d0f;
            padding: 8px 10px;
            font-weight: 900;
        }
        section { margin-top: 22px; }
        .grid {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 12px;
        }
        .box {
            border: 1px solid #e5d3aa;
            background: #fffdf7;
            padding: 12px;
        }
        .box strong {
            display: block;
            margin-top: 4px;
            color: #171008;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }
        th, td {
            border-bottom: 1px solid #eadbb8;
            padding: 10px 8px;
            text-align: left;
            vertical-align: top;
        }
        td:nth-child(2), td:nth-child(3), td:nth-child(4),
        th:nth-child(2), th:nth-child(3), th:nth-child(4) {
            text-align: right;
        }
        td span {
            display: block;
            color: #766a5a;
            font-size: 12px;
        }
        .totals {
            display: grid;
            gap: 8px;
            margin-left: auto;
            width: min(320px, 100%);
        }
        .totals div {
            display: flex;
            justify-content: space-between;
            gap: 16px;
        }
        .totals .final {
            border-top: 2px solid #d7b46a;
            padding-top: 10px;
            color: #6f4d18;
            font-size: 18px;
            font-weight: 950;
        }
        footer {
            margin-top: 24px;
            border-top: 1px solid #e5d3aa;
            padding-top: 14px;
            color: #766a5a;
            font-size: 12px;
        }
        @media print {
            body { background: #fff; }
            main {
                width: 100%;
                margin: 0;
                border: 0;
                padding: 0;
            }
        }
    </style>
</head>
<body>
    <main>
        <header>
            <div>
                <span>Recibo de pedido online</span>
                <h1>Nana Modas</h1>
                <p class="muted">Pedido ${shortId} · ${formatStoreDate(order.criadoEm)}</p>
            </div>
            <div class="badge">${escapeStoreHtml(formatStoreOrderStatus(order.status))}</div>
        </header>

        <section class="grid">
            <div class="box">
                <span class="muted">Cliente</span>
                <strong>${escapeStoreHtml(order.nomeCliente || "Cliente Nana Modas")}</strong>
                <p>${escapeStoreHtml(order.emailCliente || "")}</p>
                ${order.telefoneCliente ? `<p>${escapeStoreHtml(order.telefoneCliente)}</p>` : ""}
            </div>
            <div class="box">
                <span class="muted">Entrega</span>
                <strong>${escapeStoreHtml(order.entregaNome || "Entrega a combinar")}</strong>
                <p>${escapeStoreHtml(order.enderecoEntrega || "Endereço não informado")}</p>
                ${order.codigoRastreio ? `<p>Rastreio: ${escapeStoreHtml(order.codigoRastreio)}</p>` : ""}
            </div>
            <div class="box">
                <span class="muted">Pagamento</span>
                <strong>${escapeStoreHtml(formatStorePayment(order.formaPagamento))}</strong>
                ${paymentDetail ? `<p>${escapeStoreHtml(paymentDetail)}</p>` : "<p>Aguardando confirmação da loja.</p>"}
            </div>
            <div class="box">
                <span class="muted">Observação</span>
                <strong>${escapeStoreHtml(order.observacao || "Sem observações")}</strong>
            </div>
        </section>

        <section>
            <h2>Itens do pedido</h2>
            <table>
                <thead>
                    <tr>
                        <th>Produto</th>
                        <th>Qtd.</th>
                        <th>Unit.</th>
                        <th>Total</th>
                    </tr>
                </thead>
                <tbody>${items}</tbody>
            </table>
        </section>

        <section class="totals">
            <div><span>Produtos</span><strong>${storeCurrency.format(order.totalBruto || 0)}</strong></div>
            ${order.desconto ? `<div><span>Desconto</span><strong>- ${storeCurrency.format(order.desconto)}</strong></div>` : ""}
            <div><span>Entrega</span><strong>${formatStoreShippingValue(order.entregaValor || 0)}</strong></div>
            <div class="final"><span>Total</span><strong>${storeCurrency.format(order.total || 0)}</strong></div>
        </section>

        <footer>
            Este recibo confirma o registro do pedido online. Pagamento, separação e entrega seguem a confirmação da Nana Modas.
        </footer>
    </main>
</body>
</html>`;
}

function buildStoreOrderSummary(order) {
    const shortId = order.id.slice(0, 8).toUpperCase();
    const items = (order.itens || [])
        .map((item) => {
            const variation = formatStoreVariation(item);
            return `- ${item.quantidade}x ${item.produtoNome}${variation ? ` (${variation})` : ""}: ${storeCurrency.format(item.subtotal)}`;
        })
        .join("\n");

    return [
        `Pedido Nana Modas ${shortId}`,
        `Status: ${formatStoreOrderStatus(order.status)}`,
        `Cliente: ${order.nomeCliente}`,
        `E-mail: ${order.emailCliente}`,
        order.telefoneCliente ? `Telefone: ${order.telefoneCliente}` : null,
        `Pagamento: ${formatStorePayment(order.formaPagamento)}`,
        order.cupomCodigo ? `Cupom: ${order.cupomCodigo}` : null,
        order.desconto ? `Desconto: ${storeCurrency.format(order.desconto)}` : null,
        order.entregaNome ? `Entrega: ${order.entregaNome} (${formatStoreShippingValue(order.entregaValor)})` : null,
        `Total: ${storeCurrency.format(order.total)}`,
        "",
        "Itens:",
        items,
        "",
        `Endereço: ${order.enderecoEntrega}`,
        order.observacao ? `Observação: ${order.observacao}` : null,
        order.codigoRastreio ? `Rastreio: ${order.codigoRastreio}` : null,
        order.observacaoEntrega ? `Acompanhamento: ${order.observacaoEntrega}` : null,
        order.pixCopiaECola && !order.pagamentoConfirmadoEm ? `Pix copia e cola: ${order.pixCopiaECola}` : null
    ].filter((line) => line !== null).join("\n");
}

function buildPixPayload(order, config) {
    const pixKey = String(config.pixChave || "").trim();
    if (!pixKey || pixKey === "Configure a chave Pix no painel") {
        return "";
    }

    const amount = Number(order.total || 0);
    if (amount <= 0) {
        return "";
    }

    const merchantName = normalizePixText(config.pixNomeRecebedor || "NANA MODAS", 25, "NANA MODAS");
    const merchantCity = normalizePixText(config.pixCidade || "SAO PAULO", 15, "SAO PAULO");
    const txid = normalizePixText(`NM${order.id.slice(0, 8)}`, 25, "NANAMODAS").replace(/\s/g, "");
    const merchantAccount = [
        pixField("00", "br.gov.bcb.pix"),
        pixField("01", pixKey)
    ].join("");
    const additionalData = pixField("05", txid);
    const payloadWithoutCrc = [
        pixField("00", "01"),
        pixField("26", merchantAccount),
        pixField("52", "0000"),
        pixField("53", "986"),
        pixField("54", amount.toFixed(2)),
        pixField("58", "BR"),
        pixField("59", merchantName),
        pixField("60", merchantCity),
        pixField("62", additionalData),
        "6304"
    ].join("");

    return `${payloadWithoutCrc}${pixCrc16(payloadWithoutCrc)}`;
}

function normalizeStorePixQrCode(value) {
    const image = String(value || "").trim();
    if (!image) {
        return "";
    }

    return image.startsWith("data:image")
        ? image
        : `data:image/png;base64,${image}`;
}

function pixField(id, value) {
    const text = String(value ?? "");
    return `${id}${String(text.length).padStart(2, "0")}${text}`;
}

function pixCrc16(payload) {
    let crc = 0xFFFF;
    for (let index = 0; index < payload.length; index += 1) {
        crc ^= payload.charCodeAt(index) << 8;
        for (let bit = 0; bit < 8; bit += 1) {
            crc = (crc & 0x8000) ? ((crc << 1) ^ 0x1021) : (crc << 1);
            crc &= 0xFFFF;
        }
    }

    return crc.toString(16).toUpperCase().padStart(4, "0");
}

function normalizePixText(value, maxLength, fallback = "NANA MODAS") {
    return String(value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/[^A-Za-z0-9 $%*+\-./:]/g, "")
        .trim()
        .toUpperCase()
        .slice(0, maxLength) || fallback.slice(0, maxLength);
}

async function copyStorePixKey() {
    const pixCode = storeEls.orderPixKey.textContent.trim();
    if (!pixCode || pixCode === "Chave Pix ainda não configurada") {
        showStoreToast("Configure a chave Pix no painel primeiro.");
        return;
    }

    try {
        await navigator.clipboard.writeText(pixCode);
        showStoreToast("Pix copiado.");
    } catch {
        showStoreToast("Não foi possível copiar automaticamente. Selecione o Pix na tela.");
    }
}

function openStoreCardPayment() {
    const url = storeEls.openCardPaymentButton.dataset.paymentUrl;
    if (!url) {
        showStoreToast("Configure o link de pagamento do cartão no painel.");
        return;
    }

    window.open(url, "_blank", "noopener");
}

function buildStoreCardPaymentUrl(order, config) {
    const rawUrl = String(config.checkoutCartaoUrl || "").trim();
    if (!rawUrl) {
        return "";
    }

    const shortId = order.id.slice(0, 8).toUpperCase();
    const amount = Number(order.total || 0).toFixed(2);
    const replacements = {
        pedido: shortId,
        valor: amount,
        cliente: order.nomeCliente || ""
    };

    let url = rawUrl;
    Object.entries(replacements).forEach(([key, value]) => {
        url = url.replaceAll(`{${key}}`, encodeURIComponent(value));
    });

    try {
        const parsed = new URL(url, window.location.origin);
        if (!rawUrl.includes("{pedido}")) {
            parsed.searchParams.set("pedido", shortId);
        }
        if (!rawUrl.includes("{valor}")) {
            parsed.searchParams.set("valor", amount);
        }
        return parsed.href;
    } catch {
        return url;
    }
}

function calculateStoreShipping(event) {
    event.preventDefault();

    const cepDigits = storeEls.shippingCep.value.replace(/\D/g, "");
    if (cepDigits.length !== 8) {
        showStoreToast("Informe um CEP com 8 números.");
        return;
    }

    const formattedCep = `${cepDigits.slice(0, 5)}-${cepDigits.slice(5)}`;
    const address = {
        cep: cepDigits,
        bairro: storeEls.shippingDistrict.value || storeEls.customerDistrict.value,
        cidade: storeEls.shippingCity.value || storeEls.customerCity.value,
        estado: storeEls.shippingState.value || storeEls.customerState.value
    };
    const options = (storeState.deliveryOptions || [])
        .filter((option) => storeDeliveryMatchesAddress(option, address));

    storeState.shippingQuote = {
        cep: formattedCep,
        bairro: address.bairro || null,
        cidade: address.cidade || null,
        estado: address.estado ? address.estado.toUpperCase() : null,
        options,
        selectedId: options[0]?.id || null
    };

    storeEls.shippingCep.value = formattedCep;
    renderStoreShipping();
    showStoreToast(options.length ? "Frete calculado." : "Nenhuma entrega ativa encontrada para esse endereço.");
}

function selectStoreDeliveryOption(optionId) {
    if (!storeState.shippingQuote?.options.some((option) => option.id === optionId)) {
        return;
    }

    storeState.shippingQuote.selectedId = optionId;
    renderStoreShipping();
    renderStoreCart();
}

function getSelectedStoreDelivery() {
    if (!storeState.shippingQuote?.selectedId) {
        return null;
    }

    const option = storeState.shippingQuote.options.find((item) => item.id === storeState.shippingQuote.selectedId);
    return option
        ? { ...option, valor: calculateStoreDeliveryValue(option, getStoreCartTotal()) }
        : null;
}

function storeDeliveryMatchesAddress(option, address) {
    const cep = address.cep;
    if ((option.cepInicial || option.cepFinal) &&
        (!cep ||
            (option.cepInicial && cep < option.cepInicial) ||
            (option.cepFinal && cep > option.cepFinal))) {
        return false;
    }

    return storeListMatches(option.cidades, address.cidade) &&
        storeListMatches(option.bairros, address.bairro) &&
        storeListMatches(option.estados, address.estado);
}

function storeListMatches(list, value) {
    if (!list?.length) {
        return true;
    }

    const normalized = normalizeStore(value);
    return Boolean(normalized) && list.some((item) => normalizeStore(item) === normalized);
}

function calculateStoreDeliveryValue(option, totalProducts) {
    if (option.tipo === "Retirada") {
        return 0;
    }

    return option.freteGratisAcimaDe > 0 && totalProducts >= option.freteGratisAcimaDe
        ? 0
        : Number(option.valor || 0);
}

function formatStoreShippingValue(value) {
    return Number(value || 0) === 0 ? "Grátis" : storeCurrency.format(Number(value || 0));
}

function formatStoreDeliveryType(type) {
    const names = {
        Retirada: "Retirada na loja",
        EntregaLocal: "Entrega local",
        Correios: "Correios",
        Transportadora: "Transportadora",
        Personalizada: "Personalizada"
    };
    return names[type] || type;
}

async function saveStoreCustomer(event) {
    event.preventDefault();

    const payload = {
        nome: storeEls.accountName.value.trim(),
        email: storeEls.accountEmail.value.trim(),
        telefone: emptyStoreToNull(storeEls.accountPhone.value),
        senha: storeEls.accountPassword.value
    };

    if (!payload.nome || !payload.email.includes("@")) {
        showStoreToast("Informe nome e e-mail válidos.");
        return;
    }

    if (!payload.senha || payload.senha.length < 6) {
        showStoreToast("A senha precisa ter pelo menos 6 caracteres.");
        return;
    }

    try {
        storeEls.customerLoginButton.disabled = true;
        const acesso = await storeApi("/clientes/acessar", {
            method: "POST",
            body: JSON.stringify(payload)
        });

        storeState.customer = acesso.cliente;
        storeState.orders = await storeApi("/clientes/pedidos");
        storeEls.accountPassword.value = "";
        renderStoreCustomer();
        renderStoreOrders();
        showStoreToast(acesso.criado ? "Conta criada com sucesso." : "Login realizado.");
    } catch (error) {
        showStoreToast(error.message);
    } finally {
        storeEls.customerLoginButton.disabled = false;
    }
}

async function requestStorePasswordReset() {
    const email = storeEls.resetEmail.value.trim() || storeEls.accountEmail.value.trim();
    if (!email.includes("@")) {
        showStoreToast("Informe o e-mail cadastrado.");
        return;
    }

    try {
        storeEls.requestResetButton.disabled = true;
        const response = await storeApi("/clientes/recuperar-senha", {
            method: "POST",
            body: JSON.stringify({ email })
        });

        storeEls.resetEmail.value = email;
        storeEls.resetPasswordMessage.textContent = response.mensagem || "Código enviado.";
        showStoreToast("Código de recuperação solicitado.");
    } catch (error) {
        showStoreToast(error.message);
    } finally {
        storeEls.requestResetButton.disabled = false;
    }
}

async function confirmStorePasswordReset(event) {
    event.preventDefault();

    const payload = {
        email: storeEls.resetEmail.value.trim() || storeEls.accountEmail.value.trim(),
        codigo: storeEls.resetCode.value.trim(),
        novaSenha: storeEls.resetNewPassword.value
    };

    if (!payload.email.includes("@") || !payload.codigo || payload.novaSenha.length < 6) {
        showStoreToast("Informe e-mail, código e nova senha com 6 caracteres.");
        return;
    }

    try {
        storeEls.confirmResetButton.disabled = true;
        await storeApi("/clientes/redefinir-senha", {
            method: "POST",
            body: JSON.stringify(payload)
        });

        storeEls.accountEmail.value = payload.email;
        storeEls.accountPassword.value = "";
        storeEls.resetCode.value = "";
        storeEls.resetNewPassword.value = "";
        storeEls.resetPasswordMessage.textContent = "Senha alterada. Entre com a nova senha.";
        showStoreToast("Senha alterada com sucesso.");
    } catch (error) {
        showStoreToast(error.message);
    } finally {
        storeEls.confirmResetButton.disabled = false;
    }
}

async function logoutStoreCustomer() {
    try {
        await storeApi("/clientes/logout", { method: "POST" });
        storeState.customer = null;
        storeState.orders = [];
        storeEls.customerLoginForm.reset();
        renderStoreCustomer();
        renderStoreOrders();
        showStoreToast("Você saiu da conta.");
    } catch (error) {
        showStoreToast(error.message);
    }
}

function syncStoreCart() {
    const quantitiesByItem = new Map();
    const nextCart = [];

    for (const item of storeState.cart) {
        const product = storeState.products.find((candidate) => candidate.id === item.produtoId);
        if (!product || product.quantidadeEmEstoque <= 0) {
            continue;
        }

        const variation = normalizeStoreVariation(item);
        const key = getStoreCartItemKey(product.id, variation);
        const usedQuantity = quantitiesByItem.get(key) || 0;
        const available = getStoreAvailableQuantity(product, variation) - usedQuantity;
        if (available <= 0) {
            continue;
        }

        const quantity = Math.min(item.quantidade, available);
        nextCart.push({
            id: key,
            produtoId: product.id,
            quantidade: quantity,
            ...variation
        });
        quantitiesByItem.set(key, usedQuantity + quantity);
    }

    storeState.cart = nextCart;
}

function getStoreCartQuantity(productId, variation = null) {
    const normalized = variation ? normalizeStoreVariation(variation) : null;
    return storeState.cart
        .filter((item) => item.produtoId === productId)
        .filter((item) => !normalized || getStoreCartItemKey(productId, item) === getStoreCartItemKey(productId, normalized))
        .reduce((sum, item) => sum + item.quantidade, 0);
}

function getStoreAvailableQuantity(product, variation = null) {
    const variants = product?.variacoesEstoque || [];
    if (!variants.length) {
        return product?.quantidadeEmEstoque || 0;
    }

    return findStoreVariantStock(product, variation)?.quantidade || 0;
}

function findStoreVariantStock(product, variation = {}) {
    const selected = normalizeStoreVariation(variation);
    return (product?.variacoesEstoque || []).find((candidate) =>
        normalizeStore(candidate.tamanho || "") === normalizeStore(selected.tamanho || "") &&
        normalizeStore(candidate.cor || "") === normalizeStore(selected.cor || "") &&
        normalizeStore(candidate.modelo || "") === normalizeStore(selected.modelo || ""));
}

function getStoreCartSubtotal() {
    return storeState.cart.reduce((sum, item) => {
        const product = storeState.products.find((candidate) => candidate.id === item.produtoId);
        return sum + ((product?.preco || 0) * item.quantidade);
    }, 0);
}

function getBestStoreCoupon() {
    return [...storeState.coupons]
        .sort((a, b) => Number(b.percentualDesconto || 0) - Number(a.percentualDesconto || 0))
        [0] || null;
}

function getStoreCoupon() {
    const code = storeState.couponCode.trim().toUpperCase();
    const subtotal = getStoreCartSubtotal();
    if (!code) {
        return { code: null, discount: 0, message: "", invalid: false };
    }

    const coupon = storeState.coupons.find((item) => item.codigo?.toUpperCase() === code);
    if (!coupon) {
        return { code, discount: 0, message: "Cupom inválido.", invalid: true };
    }

    if (subtotal < coupon.valorMinimoPedido) {
        return {
            code,
            discount: 0,
            message: `${coupon.codigo} vale em compras acima de ${storeCurrency.format(coupon.valorMinimoPedido)}.`,
            invalid: true
        };
    }

    return {
        code: coupon.codigo,
        discount: Math.round(subtotal * (Number(coupon.percentualDesconto || 0) / 100) * 100) / 100,
        message: `Cupom ${coupon.codigo} aplicado: ${formatStorePercent(coupon.percentualDesconto)} OFF.`,
        invalid: false
    };
}

function formatStorePercent(value) {
    return `${Number(value || 0).toLocaleString("pt-BR", {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
    })}%`;
}

function getStoreCartTotal() {
    const subtotal = getStoreCartSubtotal();
    const coupon = getStoreCoupon();
    return Math.max(0, subtotal - coupon.discount);
}

function getStoreOrderTotal() {
    const delivery = getSelectedStoreDelivery();
    return getStoreCartTotal() + Number(delivery?.valor || 0);
}

function getSelectedStoreProduct() {
    return storeState.products.find((product) => product.id === storeState.selectedProductId) || null;
}

function getStoreProductImages(product) {
    if (!product) {
        return [];
    }

    const images = [product.imagemUrl, ...(product.imagensExtras || [])];
    const seen = new Set();
    return images
        .map((image) => String(image || "").trim())
        .filter(Boolean)
        .filter((image) => {
            const key = image.toLowerCase();
            if (seen.has(key)) {
                return false;
            }

            seen.add(key);
            return true;
        });
}

function collectProductOptions(field) {
    const seen = new Set();
    return storeState.products
        .flatMap((product) => product[field] || [])
        .map((option) => String(option || "").trim())
        .filter(Boolean)
        .filter((option) => {
            const key = normalizeStore(option);
            if (seen.has(key)) {
                return false;
            }

            seen.add(key);
            return true;
        })
        .sort((a, b) => a.localeCompare(b, "pt-BR"));
}

function productHasVariations(product) {
    return Boolean(product?.tamanhos?.length || product?.cores?.length || product?.modelos?.length);
}

function renderDetailVariationControls(product) {
    const controls = [
        renderVariationSelect("detailSize", "Tamanho", product.tamanhos || []),
        renderVariationSelect("detailColor", "Cor", product.cores || []),
        renderVariationSelect("detailModel", "Modelo", product.modelos || [])
    ].filter(Boolean);

    if (!controls.length) {
        return "";
    }

    return `<div class="variation-picker">${controls.join("")}</div>`;
}

function renderStoreVariantStock(product) {
    const variants = product?.variacoesEstoque || [];
    if (!variants.length) {
        return "";
    }

    const items = variants
        .filter((variation) => Number(variation.quantidade || 0) > 0)
        .map((variation) => `
            <span>
                ${escapeStoreHtml(formatStoreVariation(variation) || "Variação")}
                <strong>${Number(variation.quantidade || 0)} un.</strong>
            </span>
        `)
        .join("");

    return items
        ? `<div class="variant-stock-list">${items}</div>`
        : `<div class="variant-stock-list"><span>Variações temporariamente sem estoque.</span></div>`;
}

function renderVariationSelect(id, label, options) {
    if (!options.length) {
        return "";
    }

    return `
        <label>
            <span>${label}</span>
            <select id="${id}">
                ${options.map((option) => `<option value="${escapeStoreHtml(option)}">${escapeStoreHtml(option)}</option>`).join("")}
            </select>
        </label>
    `;
}

function getSelectedDetailVariation(product) {
    return {
        tamanho: product.tamanhos?.length ? document.querySelector("#detailSize")?.value || null : null,
        cor: product.cores?.length ? document.querySelector("#detailColor")?.value || null : null,
        modelo: product.modelos?.length ? document.querySelector("#detailModel")?.value || null : null
    };
}

function normalizeStoreVariation(variation = {}) {
    return {
        tamanho: emptyStoreToNull(String(variation?.tamanho || "")),
        cor: emptyStoreToNull(String(variation?.cor || "")),
        modelo: emptyStoreToNull(String(variation?.modelo || ""))
    };
}

function validateProductVariation(product, variation) {
    const checks = [
        ["tamanho", "tamanho", product.tamanhos || []],
        ["cor", "cor", product.cores || []],
        ["modelo", "modelo", product.modelos || []]
    ];

    for (const [field, label, options] of checks) {
        if (!options.length) {
            continue;
        }

        if (!variation[field]) {
            return `Escolha ${label} para continuar.`;
        }

        if (!options.some((option) => normalizeStore(option) === normalizeStore(variation[field]))) {
            return `${label} indisponível para essa peça.`;
        }
    }

    if ((product.variacoesEstoque || []).length && getStoreAvailableQuantity(product, variation) <= 0) {
        return "Essa variação está sem estoque.";
    }

    return null;
}

function getStoreCartItemKey(productId, variation) {
    const parts = [variation.tamanho, variation.cor, variation.modelo]
        .map((value) => encodeURIComponent(value || ""));
    return `${productId}|${parts.join("|")}`;
}

function formatStoreVariation(item) {
    return [
        item.tamanho ? `Tam. ${item.tamanho}` : null,
        item.cor ? `Cor ${item.cor}` : null,
        item.modelo ? `Modelo ${item.modelo}` : null
    ].filter(Boolean).join(" · ");
}

function getRelatedStoreProducts(product) {
    return storeState.products
        .filter((candidate) => candidate.id !== product.id)
        .filter((candidate) => candidate.quantidadeEmEstoque > 0)
        .filter((candidate) => candidate.categoriaId === product.categoriaId)
        .slice(0, 3);
}

function buildStoreAddress() {
    const parts = [
        emptyStoreToNull(storeEls.customerStreet.value),
        emptyStoreToNull(storeEls.customerNumber.value),
        emptyStoreToNull(storeEls.customerComplement.value),
        emptyStoreToNull(storeEls.customerDistrict.value),
        emptyStoreToNull(storeEls.customerCity.value),
        emptyStoreToNull(storeEls.customerState.value)?.toUpperCase(),
        emptyStoreToNull(storeEls.customerCep.value)
    ].filter(Boolean);

    return parts.join(", ");
}

function getStoreProductInitials(name) {
    return name
        .split(" ")
        .filter(Boolean)
        .slice(0, 2)
        .map((part) => part[0])
        .join("")
        .toUpperCase();
}

function getStoreRouteFromHash() {
    const hash = decodeURIComponent(window.location.hash || "");
    if (!hash || hash === "#inicio") {
        return { view: "home", productId: null };
    }

    if (hash === "#produtos") {
        return { view: "catalog", productId: null };
    }

    if (hash === "#sacola") {
        return { view: "bag", productId: null };
    }

    if (hash === "#frete") {
        return { view: "shipping", productId: null };
    }

    if (hash === "#cliente") {
        return { view: "account", productId: null };
    }

    if (hash === "#pedidos") {
        return { view: "orders", productId: null };
    }

    if (hash === "#privacidade") {
        return { view: "privacy", productId: null };
    }

    if (hash === "#trocas") {
        return { view: "exchanges", productId: null };
    }

    if (hash.startsWith("#produto-")) {
        return { view: "product", productId: hash.replace("#produto-", "") };
    }

    return { view: "home", productId: null };
}

function applyStoreRouteFromHash() {
    const route = getStoreRouteFromHash();
    storeState.view = route.view;
    storeState.selectedProductId = route.productId;

    if (route.view === "product") {
        const product = getSelectedStoreProduct();
        if (!product) {
            storeState.view = "catalog";
            storeState.selectedProductId = null;
            showStoreView("catalog", false);
            renderStoreProductDetail();
            return;
        }

        storeState.selectedImageUrl = getStoreProductImages(product)[0] || null;
    }

    showStoreView(storeState.view, false);
    renderStoreProductDetail();
}

function getStoreConfig() {
    return { ...defaultStoreConfig, ...(storeState.config || {}) };
}

// Só carrega os scripts de rastreamento se o painel tiver um ID configurado -
// enquanto os campos estiverem vazios (padrão), nada é injetado na página.
const storeAnalyticsState = { ga: false, meta: false };

function aplicarAnalyticsStoreConfig(config) {
    const googleAnalyticsId = String(config.googleAnalyticsId || "").trim();
    if (googleAnalyticsId && !storeAnalyticsState.ga) {
        storeAnalyticsState.ga = true;
        const gtagScript = document.createElement("script");
        gtagScript.async = true;
        gtagScript.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(googleAnalyticsId)}`;
        document.head.appendChild(gtagScript);

        window.dataLayer = window.dataLayer || [];
        window.gtag = function gtag() {
            window.dataLayer.push(arguments);
        };
        window.gtag("js", new Date());
        window.gtag("config", googleAnalyticsId);
    }

    const metaPixelId = String(config.metaPixelId || "").trim();
    if (metaPixelId && !storeAnalyticsState.meta) {
        storeAnalyticsState.meta = true;
        (function (f, b, e, v, n, t, s) {
            if (f.fbq) return;
            n = f.fbq = function () {
                n.callMethod ? n.callMethod.apply(n, arguments) : n.queue.push(arguments);
            };
            if (!f._fbq) f._fbq = n;
            n.push = n;
            n.loaded = true;
            n.version = "2.0";
            n.queue = [];
            t = b.createElement(e);
            t.async = true;
            t.src = v;
            s = b.getElementsByTagName(e)[0];
            s.parentNode.insertBefore(t, s);
        })(window, document, "script", "https://connect.facebook.net/en_US/fbevents.js");
        window.fbq("init", metaPixelId);
        window.fbq("track", "PageView");
    }
}

function formatStoreText(value) {
    const paragraphs = String(value || "")
        .split(/\n{2,}/)
        .map((paragraph) => paragraph.trim())
        .filter(Boolean);

    if (!paragraphs.length) {
        return "<p>Conteúdo em atualização.</p>";
    }

    return paragraphs
        .map((paragraph) => `<p>${escapeStoreHtml(paragraph).replace(/\n/g, "<br>")}</p>`)
        .join("");
}

function formatStorePayment(payment) {
    const names = {
        Dinheiro: "Dinheiro",
        Pix: "Pix",
        CartaoDebito: "Cartão débito",
        CartaoCredito: "Cartão crédito"
    };
    return names[payment] || payment;
}

function formatStoreOrderStatus(status) {
    const names = {
        Recebido: "Aguardando pagamento",
        Pago: "Pago",
        Separando: "Separando",
        Enviado: "Enviado",
        Entregue: "Entregue",
        Cancelado: "Cancelado"
    };
    return names[status] || status;
}

function formatStoreDate(value) {
    return value ? storeDate.format(new Date(value)) : "-";
}

function emptyStoreToNull(value) {
    const trimmed = String(value || "").trim();
    return trimmed ? trimmed : null;
}

function isValidStoreDocument(value) {
    const digits = String(value || "").replace(/\D/g, "");
    return digits.length === 11 || digits.length === 14;
}

function normalizeStore(value) {
    return value
        .toLowerCase()
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "");
}

function escapeStoreHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function showStoreToast(message) {
    storeEls.toast.textContent = message;
    storeEls.toast.classList.add("is-visible");
    window.clearTimeout(showStoreToast.timeoutId);
    showStoreToast.timeoutId = window.setTimeout(() => {
        storeEls.toast.classList.remove("is-visible");
    }, 2800);
}
