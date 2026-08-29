const state = {
    categories: [],
    products: [],
    suppliers: [],
    movements: [],
    sales: [],
    orders: [],
    customers: [],
    coupons: [],
    deliveryOptions: [],
    panelUsers: [],
    activities: [],
    backups: [],
    summary: null,
    siteConfig: null,
    currentUser: null,
    cart: [],
    lastReceipt: null,
    productSearch: "",
    productCategoryFilter: "",
    productReturnRowId: null,
    storefrontSearch: "",
    pdvSearch: "",
    stockPickerSearch: "",
    onlineOrderSearch: "",
    customerSearch: "",
    onlineOrderStatusFilter: "all",
    returningSaleId: null,
    returnMode: "return"
};

const currency = new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL"
});

const dateTime = new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short"
});

const orderStatusOptions = ["Recebido", "Pago", "Separando", "Enviado", "Entregue", "Cancelado"];

const viewTitles = {
    dashboard: "Visão geral",
    products: "Produtos",
    storefront: "Vitrine",
    siteImages: "Banner e imagens",
    paymentSettings: "Pagamento",
    shippingSettings: "Frete e entrega",
    emailSettings: "E-mail e backup",
    legalSettings: "Legal e domínio",
    siteContact: "Contato do site",
    pdv: "PDV",
    stock: "Estoque",
    onlineOrders: "Pedidos online",
    customers: "Clientes",
    reports: "Relatórios",
    users: "Usuários"
};

const roleConfigs = {
    Admin: {
        label: "Administrador",
        views: ["dashboard", "products", "storefront", "siteImages", "paymentSettings", "shippingSettings", "emailSettings", "legalSettings", "siteContact", "pdv", "stock", "onlineOrders", "customers", "reports", "users"],
        permissions: ["readProducts", "manageProducts", "manageStorefront", "usePdv", "manageStock", "viewOnlineOrders", "viewCustomers", "viewReports", "manageUsers"]
    },
    Caixa: {
        label: "Caixa",
        views: ["dashboard", "pdv"],
        permissions: ["readProducts", "usePdv"]
    },
    Estoque: {
        label: "Estoque",
        views: ["dashboard", "products", "stock"],
        permissions: ["readProducts", "manageProducts", "manageStock"]
    }
};

const els = {};
let productImageObjectUrl = null;
let productExtraImageObjectUrls = [];
let storefrontImageObjectUrl = null;
let storefrontExtraImageObjectUrls = [];
let siteImageObjectUrls = [];

document.addEventListener("DOMContentLoaded", () => {
    cacheElements();
    bindEvents();
    refreshAll();
});

function cacheElements() {
    els.pageTitle = document.querySelector("#pageTitle");
    els.apiStatus = document.querySelector("#apiStatus");
    els.refreshButton = document.querySelector("#refreshButton");
    els.logoutButton = document.querySelector("#logoutButton");
    els.userProfileBadge = document.querySelector("#userProfileBadge");
    els.toast = document.querySelector("#toast");
    els.returnModal = document.querySelector("#returnModal");
    els.returnForm = document.querySelector("#returnForm");
    els.returnSaleSummary = document.querySelector("#returnSaleSummary");
    els.returnItems = document.querySelector("#returnItems");
    els.exchangeSummary = document.querySelector("#exchangeSummary");
    els.returnReason = document.querySelector("#returnReason");
    els.cancelReturnButton = document.querySelector("#cancelReturnButton");
    els.navLinks = document.querySelectorAll("[data-view-target]");
    els.views = document.querySelectorAll("[data-view]");

    els.metricRevenue = document.querySelector("#metricRevenue");
    els.metricSales = document.querySelector("#metricSales");
    els.metricOnlineOrders = document.querySelector("#metricOnlineOrders");
    els.metricActiveProducts = document.querySelector("#metricActiveProducts");
    els.metricLowStock = document.querySelector("#metricLowStock");
    els.pendingActionsList = document.querySelector("#pendingActionsList");
    els.lowStockList = document.querySelector("#lowStockList");
    els.recentSalesList = document.querySelector("#recentSalesList");
    els.recentOrdersList = document.querySelector("#recentOrdersList");
    els.activityList = document.querySelector("#activityList");
    els.productionChecklist = document.querySelector("#productionChecklist");
    els.adminReadinessList = document.querySelector("#adminReadinessList");

    els.productForm = document.querySelector("#productForm");
    els.productId = document.querySelector("#productId");
    els.productName = document.querySelector("#productName");
    els.productCategory = document.querySelector("#productCategory");
    els.productSku = document.querySelector("#productSku");
    els.productPrice = document.querySelector("#productPrice");
    els.productCost = document.querySelector("#productCost");
    els.productInitialStock = document.querySelector("#productInitialStock");
    els.productInitialStockLabel = document.querySelector("#productInitialStockLabel");
    els.productInitialStockHint = document.querySelector("#productInitialStockHint");
    els.productDescription = document.querySelector("#productDescription");
    els.productSizes = document.querySelector("#productSizes");
    els.productColors = document.querySelector("#productColors");
    els.productModels = document.querySelector("#productModels");
    els.productVariantRows = document.querySelector("#productVariantRows");
    els.addVariantRowButton = document.querySelector("#addVariantRowButton");
    els.variantQuickAddInput = document.querySelector("#variantQuickAddInput");
    els.variantQuickAddButton = document.querySelector("#variantQuickAddButton");
    els.productVariantStock = document.querySelector("#productVariantStock");
    els.productSizeGuide = document.querySelector("#productSizeGuide");
    els.storefrontImage = document.querySelector("#storefrontImage");
    els.storefrontImageFile = document.querySelector("#storefrontImageFile");
    els.storefrontImagePreview = document.querySelector("#storefrontImagePreview");
    els.storefrontExtraImages = document.querySelector("#storefrontExtraImages");
    els.storefrontExtraImageFiles = document.querySelector("#storefrontExtraImageFiles");
    els.storefrontExtraImagePreview = document.querySelector("#storefrontExtraImagePreview");
    els.productActive = document.querySelector("#productActive");
    els.cancelEditButton = document.querySelector("#cancelEditButton");
    els.productSearch = document.querySelector("#productSearch");
    els.productCategoryFilter = document.querySelector("#productCategoryFilter");
    els.productsTable = document.querySelector("#productsTable");
    els.productCount = document.querySelector("#productCount");
    els.labelPrintBox = document.querySelector("#labelPrintBox");

    els.storefrontForm = document.querySelector("#storefrontForm");
    els.storefrontProductId = document.querySelector("#storefrontProductId");
    els.storefrontProductSelect = document.querySelector("#storefrontProductSelect");
    els.storefrontPublished = document.querySelector("#storefrontPublished");
    els.storefrontFeatured = document.querySelector("#storefrontFeatured");
    els.storefrontName = document.querySelector("#storefrontName");
    els.storefrontDescription = document.querySelector("#storefrontDescription");
    els.storefrontPrice = document.querySelector("#storefrontPrice");
    els.storefrontOrder = document.querySelector("#storefrontOrder");
    els.storefrontImage = document.querySelector("#storefrontImage");
    els.storefrontImageFile = document.querySelector("#storefrontImageFile");
    els.storefrontImagePreview = document.querySelector("#storefrontImagePreview");
    els.storefrontExtraImages = document.querySelector("#storefrontExtraImages");
    els.storefrontExtraImageFiles = document.querySelector("#storefrontExtraImageFiles");
    els.storefrontExtraImagePreview = document.querySelector("#storefrontExtraImagePreview");
    els.storefrontSearch = document.querySelector("#storefrontSearch");
    els.storefrontProductList = document.querySelector("#storefrontProductList");
    els.storefrontCount = document.querySelector("#storefrontCount");
    els.paymentSettingsForm = document.querySelector("#paymentSettingsForm");
    els.paymentGatewayForm = document.querySelector("#paymentGatewayForm");
    els.shippingConfigForm = document.querySelector("#shippingConfigForm");
    els.emailSettingsForm = document.querySelector("#emailSettingsForm");
    els.backupSettingsForm = document.querySelector("#backupSettingsForm");
    els.legalSettingsForm = document.querySelector("#legalSettingsForm");
    els.domainSettingsForm = document.querySelector("#domainSettingsForm");
    els.siteCreatorNameInput = document.querySelector("#siteCreatorNameInput");
    els.shippingBasePrice = document.querySelector("#shippingBasePrice");
    els.shippingFreeThreshold = document.querySelector("#shippingFreeThreshold");
    els.shippingMinDays = document.querySelector("#shippingMinDays");
    els.shippingMaxDays = document.querySelector("#shippingMaxDays");
    els.shippingMessageInput = document.querySelector("#shippingMessageInput");
    els.customerLoginMessageInput = document.querySelector("#customerLoginMessageInput");
    els.bannerEyebrowInput = document.querySelector("#bannerEyebrowInput");
    els.bannerTitleInput = document.querySelector("#bannerTitleInput");
    els.bannerDescriptionInput = document.querySelector("#bannerDescriptionInput");
    els.bannerPrimaryButtonInput = document.querySelector("#bannerPrimaryButtonInput");
    els.bannerSecondaryButtonInput = document.querySelector("#bannerSecondaryButtonInput");
    els.bannerImageInput = document.querySelector("#bannerImageInput");
    els.siteImagesForm = document.querySelector("#siteImagesForm");
    els.sitePromoTextInput = document.querySelector("#sitePromoTextInput");
    els.bannerImageFile = document.querySelector("#bannerImageFile");
    els.bannerImagePreview = document.querySelector("#bannerImagePreview");
    els.campaignTitleInput = document.querySelector("#campaignTitleInput");
    els.campaignDescriptionInput = document.querySelector("#campaignDescriptionInput");
    els.campaignButtonInput = document.querySelector("#campaignButtonInput");
    els.campaignImageInput = document.querySelector("#campaignImageInput");
    els.campaignImageFile = document.querySelector("#campaignImageFile");
    els.campaignImagePreview = document.querySelector("#campaignImagePreview");
    els.lookbookTitle1Input = document.querySelector("#lookbookTitle1Input");
    els.lookbookImage1Input = document.querySelector("#lookbookImage1Input");
    els.lookbookImage1File = document.querySelector("#lookbookImage1File");
    els.lookbookImage1Preview = document.querySelector("#lookbookImage1Preview");
    els.lookbookTitle2Input = document.querySelector("#lookbookTitle2Input");
    els.lookbookImage2Input = document.querySelector("#lookbookImage2Input");
    els.lookbookImage2File = document.querySelector("#lookbookImage2File");
    els.lookbookImage2Preview = document.querySelector("#lookbookImage2Preview");
    els.lookbookTitle3Input = document.querySelector("#lookbookTitle3Input");
    els.lookbookImage3Input = document.querySelector("#lookbookImage3Input");
    els.lookbookImage3File = document.querySelector("#lookbookImage3File");
    els.lookbookImage3Preview = document.querySelector("#lookbookImage3Preview");
    els.siteImageAdminPreview = document.querySelector("#siteImageAdminPreview");
    els.siteContactForm = document.querySelector("#siteContactForm");
    els.storeWhatsappInput = document.querySelector("#storeWhatsappInput");
    els.storeInstagramInput = document.querySelector("#storeInstagramInput");
    els.storeAddressInput = document.querySelector("#storeAddressInput");
    els.siteContactAdminPreview = document.querySelector("#siteContactAdminPreview");
    els.pixKeyInput = document.querySelector("#pixKeyInput");
    els.pixReceiverNameInput = document.querySelector("#pixReceiverNameInput");
    els.pixCityInput = document.querySelector("#pixCityInput");
    els.pixOnlineActiveInput = document.querySelector("#pixOnlineActiveInput");
    els.cardOnlineActiveInput = document.querySelector("#cardOnlineActiveInput");
    els.cardCheckoutNameInput = document.querySelector("#cardCheckoutNameInput");
    els.cardCheckoutUrlInput = document.querySelector("#cardCheckoutUrlInput");
    els.paymentMessageInput = document.querySelector("#paymentMessageInput");
    els.cardPaymentMessageInput = document.querySelector("#cardPaymentMessageInput");
    els.emailNotificationsActiveInput = document.querySelector("#emailNotificationsActiveInput");
    els.emailProviderInput = document.querySelector("#emailProviderInput");
    els.brevoApiKeyInput = document.querySelector("#brevoApiKeyInput");
    els.emailSenderInput = document.querySelector("#emailSenderInput");
    els.emailOrdersInput = document.querySelector("#emailOrdersInput");
    els.smtpHostInput = document.querySelector("#smtpHostInput");
    els.smtpPortInput = document.querySelector("#smtpPortInput");
    els.smtpUserInput = document.querySelector("#smtpUserInput");
    els.smtpPasswordInput = document.querySelector("#smtpPasswordInput");
    els.smtpSslInput = document.querySelector("#smtpSslInput");
    els.testEmailButton = document.querySelector("#testEmailButton");
    els.autoBackupActiveInput = document.querySelector("#autoBackupActiveInput");
    els.autoBackupIntervalInput = document.querySelector("#autoBackupIntervalInput");
    els.paymentGatewayProviderInput = document.querySelector("#paymentGatewayProviderInput");
    els.paymentGatewayActiveInput = document.querySelector("#paymentGatewayActiveInput");
    els.paymentGatewayProductionInput = document.querySelector("#paymentGatewayProductionInput");
    els.paymentGatewayPublicKeyInput = document.querySelector("#paymentGatewayPublicKeyInput");
    els.paymentGatewayAccessTokenInput = document.querySelector("#paymentGatewayAccessTokenInput");
    els.paymentGatewayWebhookSecretInput = document.querySelector("#paymentGatewayWebhookSecretInput");
    els.paymentGatewayWebhookUrlInput = document.querySelector("#paymentGatewayWebhookUrlInput");
    els.privacyPolicyInput = document.querySelector("#privacyPolicyInput");
    els.exchangePolicyInput = document.querySelector("#exchangePolicyInput");
    els.companyLegalNameInput = document.querySelector("#companyLegalNameInput");
    els.companyCnpjInput = document.querySelector("#companyCnpjInput");
    els.siteCanonicalUrlInput = document.querySelector("#siteCanonicalUrlInput");
    els.googleAnalyticsIdInput = document.querySelector("#googleAnalyticsIdInput");
    els.metaPixelIdInput = document.querySelector("#metaPixelIdInput");
    els.backupEmailActiveInput = document.querySelector("#backupEmailActiveInput");
    els.backupEmailDestinationInput = document.querySelector("#backupEmailDestinationInput");
    els.deliveryOptionForm = document.querySelector("#deliveryOptionForm");
    els.deliveryOptionId = document.querySelector("#deliveryOptionId");
    els.deliveryName = document.querySelector("#deliveryName");
    els.deliveryType = document.querySelector("#deliveryType");
    els.deliveryPrice = document.querySelector("#deliveryPrice");
    els.deliveryFreeAbove = document.querySelector("#deliveryFreeAbove");
    els.deliveryMinDays = document.querySelector("#deliveryMinDays");
    els.deliveryMaxDays = document.querySelector("#deliveryMaxDays");
    els.deliveryCepStart = document.querySelector("#deliveryCepStart");
    els.deliveryCepEnd = document.querySelector("#deliveryCepEnd");
    els.deliveryDescription = document.querySelector("#deliveryDescription");
    els.deliveryCities = document.querySelector("#deliveryCities");
    els.deliveryDistricts = document.querySelector("#deliveryDistricts");
    els.deliveryStates = document.querySelector("#deliveryStates");
    els.deliveryOrder = document.querySelector("#deliveryOrder");
    els.deliveryActive = document.querySelector("#deliveryActive");
    els.deliveryOptionList = document.querySelector("#deliveryOptionList");
    els.deliveryOptionCount = document.querySelector("#deliveryOptionCount");
    els.cancelDeliveryEditButton = document.querySelector("#cancelDeliveryEditButton");
    els.couponForm = document.querySelector("#couponForm");
    els.couponId = document.querySelector("#couponId");
    els.couponCode = document.querySelector("#couponCode");
    els.couponPercent = document.querySelector("#couponPercent");
    els.couponMinimum = document.querySelector("#couponMinimum");
    els.couponValidUntil = document.querySelector("#couponValidUntil");
    els.couponDescription = document.querySelector("#couponDescription");
    els.couponActive = document.querySelector("#couponActive");
    els.couponList = document.querySelector("#couponList");
    els.couponCount = document.querySelector("#couponCount");
    els.cancelCouponEditButton = document.querySelector("#cancelCouponEditButton");

    els.categoryForm = document.querySelector("#categoryForm");
    els.categoryName = document.querySelector("#categoryName");
    els.categoryParent = document.querySelector("#categoryParent");
    els.categoryList = document.querySelector("#categoryList");
    els.categoryCount = document.querySelector("#categoryCount");

    els.pdvSearch = document.querySelector("#pdvSearch");
    els.pdvProductGrid = document.querySelector("#pdvProductGrid");
    els.cartList = document.querySelector("#cartList");
    els.cartCount = document.querySelector("#cartCount");
    els.cartSubtotal = document.querySelector("#cartSubtotal");
    els.cartTotal = document.querySelector("#cartTotal");
    els.saleDiscount = document.querySelector("#saleDiscount");
    els.saleReceived = document.querySelector("#saleReceived");
    els.saleChange = document.querySelector("#saleChange");
    els.saleNote = document.querySelector("#saleNote");
    els.receiptBox = document.querySelector("#receiptBox");
    els.finishSaleButton = document.querySelector("#finishSaleButton");

    els.stockForm = document.querySelector("#stockForm");
    els.stockProduct = document.querySelector("#stockProduct");
    els.stockProductSearch = document.querySelector("#stockProductSearch");
    els.stockProductGrid = document.querySelector("#stockProductGrid");
    els.stockMetricUnits = document.querySelector("#stockMetricUnits");
    els.stockMetricCostValue = document.querySelector("#stockMetricCostValue");
    els.stockMetricSaleValue = document.querySelector("#stockMetricSaleValue");
    els.stockMetricProfit = document.querySelector("#stockMetricProfit");
    els.stockValueByCategoryTable = document.querySelector("#stockValueByCategoryTable");
    els.stockSizeWrap = document.querySelector("#stockSizeWrap");
    els.stockColorWrap = document.querySelector("#stockColorWrap");
    els.stockModelWrap = document.querySelector("#stockModelWrap");
    els.stockSize = document.querySelector("#stockSize");
    els.stockColor = document.querySelector("#stockColor");
    els.stockModel = document.querySelector("#stockModel");
    els.stockQuantity = document.querySelector("#stockQuantity");
    els.stockSupplier = document.querySelector("#stockSupplier");
    els.stockCost = document.querySelector("#stockCost");
    els.stockDocument = document.querySelector("#stockDocument");
    els.stockNote = document.querySelector("#stockNote");
    els.stockMap = document.querySelector("#stockMap");
    els.movementsTable = document.querySelector("#movementsTable");
    els.movementCount = document.querySelector("#movementCount");
    els.supplierForm = document.querySelector("#supplierForm");
    els.supplierName = document.querySelector("#supplierName");
    els.supplierDocument = document.querySelector("#supplierDocument");
    els.supplierPhone = document.querySelector("#supplierPhone");
    els.supplierEmail = document.querySelector("#supplierEmail");
    els.supplierList = document.querySelector("#supplierList");
    els.supplierCount = document.querySelector("#supplierCount");

    els.onlineOrderSearch = document.querySelector("#onlineOrderSearch");
    els.onlineOrderStatusFilter = document.querySelector("#onlineOrderStatusFilter");
    els.onlineOrderList = document.querySelector("#onlineOrderList");
    els.onlineOrdersCount = document.querySelector("#onlineOrdersCount");
    els.onlineOrderStatusSummary = document.querySelector("#onlineOrderStatusSummary");
    els.onlineOrderOpsSummary = document.querySelector("#onlineOrderOpsSummary");
    els.onlineReceiptBox = document.querySelector("#onlineReceiptBox");

    els.customerSearch = document.querySelector("#customerSearch");
    els.customerList = document.querySelector("#customerList");
    els.customerCount = document.querySelector("#customerCount");
    els.customerSummary = document.querySelector("#customerSummary");

    els.reportStartDate = document.querySelector("#reportStartDate");
    els.reportEndDate = document.querySelector("#reportEndDate");
    els.reportChannelFilter = document.querySelector("#reportChannelFilter");
    els.reportPaymentFilter = document.querySelector("#reportPaymentFilter");
    els.clearReportFilters = document.querySelector("#clearReportFilters");
    els.reportKpiStrip = document.querySelector("#reportKpiStrip");
    els.refreshBackupsButton = document.querySelector("#refreshBackupsButton");
    els.backupFileCount = document.querySelector("#backupFileCount");
    els.backupFileList = document.querySelector("#backupFileList");
    els.paymentSummary = document.querySelector("#paymentSummary");
    els.topProductsReport = document.querySelector("#topProductsReport");
    els.dailySalesReport = document.querySelector("#dailySalesReport");
    els.supplierEntriesReport = document.querySelector("#supplierEntriesReport");
    els.lowStockReport = document.querySelector("#lowStockReport");
    els.reportIndicators = document.querySelector("#reportIndicators");
    els.reportExportActions = document.querySelector("#reportExportActions");
    els.salesTable = document.querySelector("#salesTable");
    els.salesCount = document.querySelector("#salesCount");
    els.ordersTable = document.querySelector("#ordersTable");
    els.ordersCount = document.querySelector("#ordersCount");
    els.panelUserForm = document.querySelector("#panelUserForm");
    els.panelUserId = document.querySelector("#panelUserId");
    els.panelUserLogin = document.querySelector("#panelUserLogin");
    els.panelUserName = document.querySelector("#panelUserName");
    els.panelUserRole = document.querySelector("#panelUserRole");
    els.panelUserPassword = document.querySelector("#panelUserPassword");
    els.panelUserActive = document.querySelector("#panelUserActive");
    els.panelUserList = document.querySelector("#panelUserList");
    els.panelUserCount = document.querySelector("#panelUserCount");
    els.userFormMode = document.querySelector("#userFormMode");
    els.cancelUserEditButton = document.querySelector("#cancelUserEditButton");
}

function bindEvents() {
    els.navLinks.forEach((button) => {
        button.addEventListener("click", () => showView(button.dataset.viewTarget));
    });

    els.refreshButton.addEventListener("click", refreshAll);
    els.logoutButton.addEventListener("click", logoutAdmin);
    els.productForm.addEventListener("submit", saveProduct);
    els.addVariantRowButton.addEventListener("click", () => addProductVariantRow());
    els.variantQuickAddButton.addEventListener("click", applyVariantQuickAdd);
    els.productSku.addEventListener("input", refreshAutoVariantSkus);
    els.variantQuickAddInput.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            applyVariantQuickAdd();
        }
    });
    els.productVariantRows.addEventListener("input", syncProductVariantTextarea);
    els.productVariantRows.addEventListener("click", (event) => {
        const button = event.target.closest("[data-variant-action='remove']");
        if (!button) {
            return;
        }

        button.closest("[data-variant-row]")?.remove();
        syncProductVariantTextarea();
    });
    els.storefrontImage.addEventListener("input", () => {
        if (!els.storefrontImageFile.files.length) {
            setStorefrontImagePreview(els.storefrontImage.value);
        }
    });
    els.storefrontImageFile.addEventListener("change", previewSelectedStorefrontImage);
    els.storefrontExtraImages.addEventListener("input", () => {
        if (!els.storefrontExtraImageFiles.files.length) {
            setStorefrontExtraImagePreview(parseImageList(els.storefrontExtraImages.value));
        }
    });
    els.storefrontExtraImageFiles.addEventListener("change", previewSelectedStorefrontExtraImages);
    els.storefrontForm.addEventListener("submit", saveStorefrontProduct);
    els.storefrontProductSelect.addEventListener("change", () => {
        const product = state.products.find((item) => item.id === els.storefrontProductSelect.value);
        if (product) {
            editStorefrontProduct(product);
        }
    });
    els.storefrontImage.addEventListener("input", () => {
        if (!els.storefrontImageFile.files.length) {
            setStorefrontImagePreview(els.storefrontImage.value);
        }
    });
    els.storefrontImageFile.addEventListener("change", previewSelectedStorefrontImage);
    els.storefrontExtraImages.addEventListener("input", () => {
        if (!els.storefrontExtraImageFiles.files.length) {
            setStorefrontExtraImagePreview(parseImageList(els.storefrontExtraImages.value));
        }
    });
    els.storefrontExtraImageFiles.addEventListener("change", previewSelectedStorefrontExtraImages);
    els.paymentSettingsForm.addEventListener("submit", saveSiteConfig);
    els.paymentGatewayForm.addEventListener("submit", saveSiteConfig);
    els.shippingConfigForm.addEventListener("submit", saveSiteConfig);
    els.emailSettingsForm.addEventListener("submit", saveSiteConfig);
    els.backupSettingsForm.addEventListener("submit", saveSiteConfig);
    els.legalSettingsForm.addEventListener("submit", saveSiteConfig);
    els.domainSettingsForm.addEventListener("submit", saveSiteConfig);
    els.siteImagesForm.addEventListener("submit", saveSiteImagesConfig);
    els.siteContactForm.addEventListener("submit", saveSiteContactConfig);
    bindSiteImagePreview(els.bannerImageInput, els.bannerImageFile, els.bannerImagePreview, "Nenhum banner principal selecionado");
    bindSiteImagePreview(els.campaignImageInput, els.campaignImageFile, els.campaignImagePreview, "Nenhuma campanha selecionada");
    bindSiteImagePreview(els.lookbookImage1Input, els.lookbookImage1File, els.lookbookImage1Preview, "Nenhuma imagem");
    bindSiteImagePreview(els.lookbookImage2Input, els.lookbookImage2File, els.lookbookImage2Preview, "Nenhuma imagem");
    bindSiteImagePreview(els.lookbookImage3Input, els.lookbookImage3File, els.lookbookImage3Preview, "Nenhuma imagem");
    [
        els.bannerTitleInput,
        els.campaignTitleInput,
        els.lookbookTitle1Input,
        els.lookbookTitle2Input,
        els.lookbookTitle3Input
    ].forEach((input) => input.addEventListener("input", renderSiteImageAdminPreview));
    [
        els.storeWhatsappInput,
        els.storeInstagramInput,
        els.storeAddressInput
    ].forEach((input) => input.addEventListener("input", renderSiteContactAdminPreview));
    els.testEmailButton.addEventListener("click", testEmailConfig);
    els.deliveryOptionForm.addEventListener("submit", saveDeliveryOption);
    els.cancelDeliveryEditButton.addEventListener("click", resetDeliveryOptionForm);
    els.couponForm.addEventListener("submit", saveCoupon);
    els.cancelCouponEditButton.addEventListener("click", resetCouponForm);
    els.categoryForm.addEventListener("submit", saveCategory);
    els.categoryList.addEventListener("click", handleDeleteCategoryClick);
    els.cancelEditButton.addEventListener("click", resetProductForm);
    els.stockForm.addEventListener("submit", saveStockEntry);
    els.stockProductSearch.addEventListener("input", (event) => {
        state.stockPickerSearch = event.target.value;
        renderStockProductPicker();
    });
    els.stockProductGrid.addEventListener("click", (event) => {
        const card = event.target.closest("[data-stock-product]");
        if (!card) {
            return;
        }

        els.stockProduct.value = card.dataset.stockProduct;
        renderStockProductPicker();
        renderStockVariationFields();
    });
    els.supplierForm.addEventListener("submit", saveSupplier);
    els.panelUserForm.addEventListener("submit", savePanelUser);
    els.cancelUserEditButton.addEventListener("click", resetPanelUserForm);
    els.finishSaleButton.addEventListener("click", finishSale);
    els.returnForm.addEventListener("submit", submitReturnSale);
    els.cancelReturnButton.addEventListener("click", closeReturnDialog);
    els.saleDiscount.addEventListener("input", renderCart);
    els.saleReceived.addEventListener("input", renderCart);

    els.productSearch.addEventListener("input", (event) => {
        state.productSearch = event.target.value;
        renderProductsTable();
    });
    els.productCategoryFilter.addEventListener("change", (event) => {
        state.productCategoryFilter = event.target.value;
        renderProductsTable();
    });

    els.storefrontSearch.addEventListener("input", (event) => {
        state.storefrontSearch = event.target.value;
        renderStorefront();
    });

    els.pdvSearch.addEventListener("input", (event) => {
        state.pdvSearch = event.target.value;
        renderPdvProducts();
    });
    els.pdvSearch.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            addPdvProductByExactCode(event);
        }
    });

    els.onlineOrderSearch.addEventListener("input", (event) => {
        state.onlineOrderSearch = event.target.value;
        renderOnlineOrders();
    });

    els.onlineOrderStatusFilter.addEventListener("change", (event) => {
        state.onlineOrderStatusFilter = event.target.value;
        renderOnlineOrders();
    });

    els.customerSearch.addEventListener("input", (event) => {
        state.customerSearch = event.target.value;
        renderCustomers();
    });

    [els.reportStartDate, els.reportEndDate, els.reportChannelFilter, els.reportPaymentFilter].forEach((input) => {
        input.addEventListener("change", renderReports);
    });
    els.clearReportFilters.addEventListener("click", () => {
        els.reportStartDate.value = "";
        els.reportEndDate.value = "";
        els.reportChannelFilter.value = "all";
        els.reportPaymentFilter.value = "all";
        renderReports();
    });
    els.refreshBackupsButton.addEventListener("click", loadBackups);
    els.backupFileList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-backup-download]");
        if (!button) {
            return;
        }

        downloadBackupFile(button.dataset.backupDownload);
    });

    els.productsTable.addEventListener("click", async (event) => {
        const button = event.target.closest("[data-action]");
        if (!button) {
            return;
        }

        const product = state.products.find((item) => item.id === button.dataset.id);
        if (!product) {
            return;
        }

        if (button.dataset.action === "edit") {
            editProduct(product);
        }

        if (button.dataset.action === "delete") {
            await deleteProduct(product);
        }

        if (button.dataset.action === "labels") {
            if (!renderLabelPrintBox(product)) {
                showToast("Esse produto não tem unidades em estoque para gerar etiqueta.");
            }
        }
    });

    els.labelPrintBox.addEventListener("click", (event) => {
        const button = event.target.closest("[data-label-action]");
        if (!button) {
            return;
        }

        if (button.dataset.labelAction === "print") {
            printLabels();
        }

        if (button.dataset.labelAction === "close") {
            els.labelPrintBox.classList.add("hidden");
            els.labelPrintBox.innerHTML = "";
        }
    });

    els.storefrontProductList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-site-action]");
        if (!button) {
            return;
        }

        const product = state.products.find((item) => item.id === button.dataset.id);
        if (!product) {
            return;
        }

        if (button.dataset.siteAction === "edit") {
            editStorefrontProduct(product);
        }
    });

    els.couponList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-coupon-action]");
        if (!button) {
            return;
        }

        const coupon = state.coupons.find((item) => item.id === button.dataset.id);
        if (!coupon) {
            return;
        }

        if (button.dataset.couponAction === "edit") {
            editCoupon(coupon);
        }
    });

    els.deliveryOptionList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-delivery-action]");
        if (!button) {
            return;
        }

        const option = state.deliveryOptions.find((item) => item.id === button.dataset.id);
        if (!option) {
            return;
        }

        if (button.dataset.deliveryAction === "edit") {
            editDeliveryOption(option);
        }
    });

    els.panelUserList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-user-action]");
        if (!button) {
            return;
        }

        const user = state.panelUsers.find((item) => item.id === button.dataset.id);
        if (!user) {
            return;
        }

        if (button.dataset.userAction === "edit") {
            editPanelUser(user);
        }
    });

    els.pdvProductGrid.addEventListener("click", (event) => {
        const button = event.target.closest("[data-add-product]");
        if (!button) {
            return;
        }

        const card = button.closest("[data-pdv-product]");
        addToCart(button.dataset.addProduct, card ? getPdvCardVariation(card) : null);
    });

    els.cartList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-cart-action]");
        if (!button) {
            return;
        }

        changeCart(button.dataset.id, button.dataset.cartAction);
    });

    els.ordersTable.addEventListener("change", (event) => {
        const select = event.target.closest("[data-order-status]");
        if (!select) {
            return;
        }

        updateOrderStatus(select.dataset.id, select.value, select);
    });

    els.onlineOrderList.addEventListener("change", (event) => {
        const select = event.target.closest("[data-online-order-status]");
        if (!select) {
            return;
        }

        updateOrderStatus(select.dataset.id, select.value, select);
    });

    els.onlineOrderList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-online-order-action]");
        if (!button) {
            return;
        }

        if (button.dataset.onlineOrderAction === "copy") {
            copyOnlineOrderSummary(button.dataset.id);
        }

        if (button.dataset.onlineOrderAction === "tracking") {
            saveOnlineOrderTracking(button.dataset.id);
        }

        if (button.dataset.onlineOrderAction === "payment") {
            saveOnlineOrderPayment(button.dataset.id);
        }

        if (button.dataset.onlineOrderAction === "quick-payment") {
            confirmOnlineOrderPayment(button.dataset.id);
        }

        if (button.dataset.onlineOrderAction === "quick-status") {
            quickUpdateOnlineOrderStatus(button.dataset.id, button.dataset.status);
        }

        if (button.dataset.onlineOrderAction === "receipt") {
            renderOnlineOrderReceipt(button.dataset.id);
        }
    });

    els.salesTable.addEventListener("click", (event) => {
        const button = event.target.closest("[data-sale-action]");
        if (!button) {
            return;
        }

        if (button.dataset.saleAction === "receipt") {
            const sale = state.sales.find((item) => item.id === button.dataset.id);
            if (sale) {
                state.lastReceipt = sale;
                renderReceipt(sale);
                showView("pdv");
            }
        }

        if (button.dataset.saleAction === "return") {
            openReturnDialog(button.dataset.id, "return");
        }

        if (button.dataset.saleAction === "exchange") {
            openReturnDialog(button.dataset.id, "exchange");
        }
    });

    els.receiptBox.addEventListener("click", (event) => {
        const button = event.target.closest("[data-receipt-action]");
        if (!button || !state.lastReceipt) {
            return;
        }

        if (button.dataset.receiptAction === "copy") {
            copySaleReceipt(state.lastReceipt);
        }

        if (button.dataset.receiptAction === "print") {
            printReceipt();
        }
    });

    els.onlineReceiptBox.addEventListener("click", (event) => {
        const button = event.target.closest("[data-online-receipt-action]");
        if (!button) {
            return;
        }

        const order = state.orders.find((item) => item.id === button.dataset.id);
        if (!order) {
            return;
        }

        if (button.dataset.onlineReceiptAction === "copy") {
            copyOnlineOrderSummary(order.id);
        }

        if (button.dataset.onlineReceiptAction === "print") {
            printReceipt(els.onlineReceiptBox);
        }
    });

    els.pendingActionsList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-pending-action]");
        if (!button) {
            return;
        }

        openPendingAction(button.dataset.pendingAction, button.dataset.status || "");
    });
    els.adminReadinessList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-view-target]");
        if (!button) {
            return;
        }

        showView(button.dataset.viewTarget);
    });

    els.reportExportActions.addEventListener("click", (event) => {
        const button = event.target.closest("[data-export-report]");
        if (!button) {
            return;
        }

        exportReport(button.dataset.exportReport);
    });
}

// Um loader por fatia de estado. refreshAll() usa todos (carga inicial e botão
// "Atualizar"); refreshScoped() usa só os relevantes pra cada ação, pra não
// buscar e redesenhar o painel inteiro a cada salvamento.
const stateLoaders = {
    categories: async () => { state.categories = can("readProducts") ? await api("/categorias") : []; },
    products: async () => { state.products = can("readProducts") ? await api("/produtos?apenasAtivos=false") : []; },
    suppliers: async () => { state.suppliers = can("manageStock") ? await api("/fornecedores") : []; },
    movements: async () => { state.movements = can("manageStock") ? await api("/estoque/movimentacoes") : []; },
    sales: async () => { state.sales = can("usePdv") ? await api("/pdv/vendas") : []; },
    orders: async () => { state.orders = can("viewOnlineOrders") ? await api("/pedidos-online") : []; },
    customers: async () => { state.customers = can("viewCustomers") ? await api("/clientes-painel") : []; },
    summary: async () => { state.summary = can("viewReports") ? await api("/relatorios/resumo") : null; },
    siteConfig: async () => { state.siteConfig = can("manageStorefront") ? await api("/loja-configuracao") : null; },
    coupons: async () => { state.coupons = can("manageStorefront") ? await api("/cupons") : []; },
    deliveryOptions: async () => { state.deliveryOptions = can("manageStorefront") ? await api("/opcoes-entrega") : []; },
    panelUsers: async () => { state.panelUsers = can("manageUsers") ? await api("/usuarios-painel") : []; },
    activities: async () => { state.activities = can("manageUsers") ? await api("/atividades-painel") : []; },
    backups: async () => { state.backups = can("viewReports") ? await api("/backup/arquivos") : []; }
};

// Quais telas dependem de cada fatia de estado, pra redesenhar só o necessário.
// renderDashboard sempre roda (é redesenho de DOM local, não pesa) — garante
// que os cartões/atalhos do painel principal nunca fiquem desatualizados.
const scopeRenderers = {
    categories: [
        ["", renderCategoryOptions],
        ["manageProducts", renderCategories],
        ["manageProducts", renderProductsTable]
    ],
    products: [
        ["", renderCategoryOptions],
        ["manageProducts", renderProductsTable],
        ["manageStorefront", renderStorefront],
        ["usePdv", renderPdvProducts],
        ["usePdv", renderCart],
        ["manageStock", renderStock],
        ["viewReports", renderReports]
    ],
    suppliers: [["manageStock", renderStock]],
    movements: [["manageStock", renderStock]],
    sales: [
        ["viewReports", renderReports],
        ["usePdv", renderCart]
    ],
    orders: [
        ["viewOnlineOrders", renderOnlineOrders],
        ["viewReports", renderReports]
    ],
    customers: [["viewCustomers", renderCustomers]],
    summary: [["viewReports", renderReports]],
    siteConfig: [["manageStorefront", renderSiteConfig]],
    coupons: [["manageStorefront", renderCoupons]],
    deliveryOptions: [["manageStorefront", renderDeliveryOptions]],
    panelUsers: [["manageUsers", renderPanelUsers]],
    activities: [],
    backups: [["viewReports", renderReports]]
};

async function refreshAll() {
    setStatus("Carregando", "loading");

    try {
        state.currentUser = await api("/auth/status");
        applyRoleUi();

        await Promise.all(Object.values(stateLoaders).map((load) => load()));

        syncCartWithStock();
        renderAll();
        setStatus("Online", "online");
    } catch (error) {
        setStatus("Offline", "offline");
        showToast(error.message || "Não foi possível carregar os dados.");
    }
}

// Recarrega só as fatias de estado passadas em `scopes` (chaves de stateLoaders)
// e redesenha só as telas que dependem delas, em vez de tudo. Usado depois de
// ações pontuais (salvar produto, registrar venda, mudar status de pedido...).
async function refreshScoped(scopes) {
    setStatus("Carregando", "loading");

    try {
        await Promise.all(scopes.map((scope) => stateLoaders[scope]()));

        if (scopes.includes("products") || scopes.includes("movements")) {
            syncCartWithStock();
        }

        const toRender = new Map();
        for (const scope of scopes) {
            for (const [permission, renderFn] of scopeRenderers[scope] || []) {
                if (!permission || can(permission)) {
                    toRender.set(renderFn, true);
                }
            }
        }
        for (const renderFn of toRender.keys()) {
            renderFn();
        }
        renderDashboard();

        setStatus("Online", "online");
    } catch (error) {
        setStatus("Offline", "offline");
        showToast(error.message || "Não foi possível carregar os dados.");
    }
}

async function api(path, options = {}) {
    const response = await fetch(path, {
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        },
        ...options
    });

    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;

    if (response.status === 401) {
        window.location.href = "/login.html";
        throw new Error("Faça login para continuar.");
    }

    if (response.status === 403) {
        throw new Error("Seu perfil não tem permissão para essa área.");
    }

    if (!response.ok) {
        throw new Error(payload?.erro || "Erro na operação.");
    }

    return payload;
}

async function uploadProductImage(file) {
    const formData = new FormData();
    formData.append("imagem", file);

    const response = await fetch("/produtos/imagem", {
        method: "POST",
        body: formData
    });

    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;

    if (response.status === 401) {
        window.location.href = "/login.html";
        throw new Error("Faça login para continuar.");
    }

    if (response.status === 403) {
        throw new Error("Seu perfil não tem permissão para enviar imagem.");
    }

    if (!response.ok) {
        throw new Error(payload?.erro || "Não foi possível enviar a imagem.");
    }

    return payload;
}

async function logoutAdmin() {
    await fetch("/auth/logout", { method: "POST" });
    window.location.href = "/login.html";
}

function renderAll() {
    renderCategoryOptions();
    renderDashboard();
    if (can("manageProducts")) {
        renderCategories();
        renderProductsTable();
    }
    if (can("manageStorefront")) {
        renderStorefront();
        renderSiteConfig();
        renderDeliveryOptions();
        renderCoupons();
    }
    if (can("usePdv")) {
        renderPdvProducts();
        renderCart();
    }
    if (can("manageStock")) {
        renderStock();
    }
    if (can("viewOnlineOrders")) {
        renderOnlineOrders();
    }
    if (can("viewCustomers")) {
        renderCustomers();
    }
    if (can("viewReports")) {
        renderReports();
    }
    if (can("manageUsers")) {
        renderPanelUsers();
    }
}

function showView(viewName) {
    if (!canView(viewName)) {
        viewName = getRoleConfig().views[0] || "dashboard";
    }

    els.navLinks.forEach((button) => {
        button.classList.toggle("is-active", button.dataset.viewTarget === viewName);
    });

    els.views.forEach((view) => {
        view.classList.toggle("is-active", view.dataset.view === viewName);
    });

    els.pageTitle.textContent = viewTitles[viewName] || "Painel";
}

function applyRoleUi() {
    const config = getRoleConfig();
    els.userProfileBadge.textContent = config.label;
    els.navLinks.forEach((button) => {
        button.classList.toggle("hidden", !config.views.includes(button.dataset.viewTarget));
    });

    const activeView = document.querySelector(".view.is-active")?.dataset.view || "dashboard";
    if (!canView(activeView)) {
        showView(config.views[0] || "dashboard");
    }
}

function getRoleConfig() {
    return roleConfigs[state.currentUser?.perfil] || roleConfigs.Admin;
}

function can(permission) {
    return getRoleConfig().permissions.includes(permission);
}

function canView(viewName) {
    return getRoleConfig().views.includes(viewName);
}

function renderDashboard() {
    const summary = getDashboardSummary();
    els.metricRevenue.textContent = currency.format(summary.faturamentoTotal || summary.faturamentoLoja || 0);
    els.metricSales.textContent = summary.vendasLoja || 0;
    els.metricOnlineOrders.textContent = summary.pedidosOnline || 0;
    els.metricActiveProducts.textContent = summary.produtosAtivos || 0;
    const lowStockItems = getLowStockItems();
    els.metricLowStock.textContent = lowStockItems.length;

    renderPendingActions();

    els.lowStockList.innerHTML = lowStockItems.length
        ? lowStockItems.map((item) => `
            <div class="list-item">
                <div>
                    <strong>${escapeHtml(item.nome)}</strong>
                    <span>${escapeHtml(item.detalhe)}</span>
                </div>
                <span class="badge badge-warn">${item.quantidade} un.</span>
            </div>
        `).join("")
        : `<div class="empty-state">Nenhum produto com estoque baixo.</div>`;

    const recentSales = state.sales.slice(0, 5);
    els.recentSalesList.innerHTML = recentSales.length
        ? recentSales.map((sale) => `
            <div class="list-item">
                <div>
                    <strong>${currency.format(sale.total)}</strong>
                    <span>${formatPayment(sale.formaPagamento)} · ${formatDate(sale.criadaEm)}</span>
                </div>
                ${sale.devolvida
                    ? '<span class="badge badge-muted">Devolvida</span>'
                    : sale.devolucaoParcial
                        ? '<span class="badge badge-warn">Parcial</span>'
                        : `<span class="badge badge-ok">${sale.itens.length} itens</span>`}
            </div>
        `).join("")
        : `<div class="empty-state">Nenhuma venda registrada.</div>`;

    const recentOrders = state.orders.slice(0, 5);
    els.recentOrdersList.innerHTML = recentOrders.length
        ? recentOrders.map((order) => `
            <div class="list-item">
                <div>
                    <strong>${currency.format(order.total)}</strong>
                    <span>${escapeHtml(order.nomeCliente)} · ${formatDate(order.criadoEm)}</span>
                </div>
                <span class="badge badge-ok">${formatOrderStatus(order.status)}</span>
            </div>
        `).join("")
        : `<div class="empty-state">Nenhum pedido online registrado.</div>`;

    const activities = state.activities.slice(0, 8);
    els.activityList.innerHTML = activities.length
        ? activities.map((activity) => `
            <div class="list-item">
                <div>
                    <strong>${escapeHtml(activity.acao)}</strong>
                    <span>${escapeHtml(activity.usuario)} · ${formatDate(activity.criadaEm)}${activity.detalhe ? ` · ${escapeHtml(activity.detalhe)}` : ""}</span>
                </div>
            </div>
        `).join("")
        : `<div class="empty-state">Nenhuma atividade registrada ainda.</div>`;

    renderProductionChecklist();
    renderAdminReadiness();
}

function renderProductionChecklist() {
    const config = state.siteConfig || {};
    const publishedProducts = state.products.filter((product) => product.ativo && product.publicadoNaLoja);
    const hasSiteImages = Boolean(config.bannerImagemUrl || config.campanhaImagemUrl || config.vitrineImagem1Url || config.vitrineImagem2Url || config.vitrineImagem3Url);
    const hasDelivery = state.deliveryOptions.some((option) => option.ativo);
    const hasAdminUsers = state.panelUsers.some((user) => user.perfil === "Admin" && user.ativo);
    const checklist = [
        {
            title: "Produtos publicados",
            detail: `${publishedProducts.length} produto(s) visíveis no site`,
            done: publishedProducts.length > 0
        },
        {
            title: "Imagens comerciais",
            detail: "Banner, campanha ou vitrines configurados em Imagens do site",
            done: hasSiteImages
        },
        {
            title: "Contato público",
            detail: "WhatsApp, Instagram ou endereço visíveis na loja",
            done: Boolean(config.whatsappLoja || config.instagramLoja || config.enderecoLoja)
        },
        {
            title: "Backup seguro",
            detail: config.backupAutomaticoAtivo ? `Automático a cada ${config.backupIntervaloHoras || 24}h` : "Use o botão Backup banco antes de publicar",
            done: Boolean(config.backupAutomaticoAtivo || state.activities.some((activity) => activity.acao === "Backup do banco"))
        },
        {
            title: "Banner da loja",
            detail: "Título, descrição e imagem principal preenchidos",
            done: Boolean(config.bannerTitulo && config.bannerDescricao && config.bannerImagemUrl)
        },
        {
            title: "Pix configurado",
            detail: "Chave Pix e nome do recebedor preenchidos",
            done: Boolean(config.pixChave && config.pixChave !== "Configure a chave Pix no painel" && config.pixNomeRecebedor)
        },
        {
            title: "Cartão/link configurado",
            detail: "Link de checkout pronto ou cartão desativado",
            done: config.cartaoOnlineAtivo === false || Boolean(config.checkoutCartaoUrl)
        },
        {
            title: "Frete ativo",
            detail: hasDelivery ? "Opções de entrega ativas no site" : "Cadastre ao menos uma opção de entrega",
            done: hasDelivery
        },
        {
            title: "Gateway preparado",
            detail: "Asaas ativo com token e segredo de webhook",
            done: Boolean(config.gatewayPagamentoAtivo && config.gatewayPagamentoProvedor === "Asaas" && config.gatewayPagamentoAccessTokenConfigurado && config.gatewayPagamentoWebhookSecretConfigurado)
        },
        {
            title: "E-mail automático",
            detail: "SMTP ativo para avisos de pedido",
            done: Boolean(config.emailNotificacoesAtivo && config.emailRemetente && config.emailPedidosDestino && config.smtpHost)
        },
        {
            title: "Acessos do painel",
            detail: "Admin ativo e usuários de caixa/estoque separados quando necessário",
            done: hasAdminUsers && state.panelUsers.length > 1
        },
        {
            title: "Estoque crítico revisado",
            detail: "Sem produtos ou variações com 3 un. ou menos",
            done: getLowStockItems().length === 0
        },
        {
            title: "Domínio e HTTPS",
            detail: "Configurar no provedor de hospedagem",
            done: false
        }
    ];

    els.productionChecklist.innerHTML = checklist.map((item) => `
        <div class="list-item">
            <div>
                <strong>${escapeHtml(item.title)}</strong>
                <span>${escapeHtml(item.detail)}</span>
            </div>
            <span class="badge ${item.done ? "badge-ok" : "badge-warn"}">${item.done ? "Ok" : "Pendente"}</span>
        </div>
    `).join("");
}

function renderAdminReadiness() {
    if (!els.adminReadinessList) {
        return;
    }

    const config = state.siteConfig || {};
    const publishedProducts = state.products.filter((product) => product.ativo && product.publicadoNaLoja);
    const pendingPayments = state.orders.filter((order) => order.status === "Recebido").length;
    const paidToSeparate = state.orders.filter((order) => order.status === "Pago").length;
    const lowStock = getLowStockItems().length;
    const hasContact = Boolean(config.whatsappLoja || config.instagramLoja || config.enderecoLoja);
    const hasImages = Boolean(config.bannerImagemUrl || config.campanhaImagemUrl || config.vitrineImagem1Url || config.vitrineImagem2Url || config.vitrineImagem3Url);
    const hasDelivery = state.deliveryOptions.some((option) => option.ativo);
    const hasUsers = state.panelUsers.length > 1;
    const gatewayReady = Boolean(config.gatewayPagamentoAtivo && config.gatewayPagamentoAccessTokenConfigurado && config.gatewayPagamentoWebhookSecretConfigurado);

    const items = [
        {
            title: "Loja online",
            detail: `${publishedProducts.length} produtos publicados · ${hasImages ? "imagens ok" : "faltam banners"} · ${hasContact ? "contato ok" : "faltam contatos"}`,
            level: publishedProducts.length && hasImages && hasContact ? "ok" : "warn",
            view: "storefront"
        },
        {
            title: "Pedidos",
            detail: `${pendingPayments} aguardando pagamento · ${paidToSeparate} pagos para separar`,
            level: pendingPayments || paidToSeparate ? "warn" : "ok",
            view: "onlineOrders"
        },
        {
            title: "Estoque",
            detail: lowStock ? `${lowStock} item(ns) em estoque crítico` : "Sem alerta crítico agora",
            level: lowStock ? "warn" : "ok",
            view: "stock"
        },
        {
            title: "Checkout",
            detail: `${hasDelivery ? "frete ativo" : "faltam opções de entrega"} · ${gatewayReady ? "gateway pronto" : "pagamento real pendente"}`,
            level: hasDelivery && gatewayReady ? "ok" : "warn",
            view: "storefront"
        },
        {
            title: "Segurança",
            detail: hasUsers ? "Usuários separados cadastrados" : "Crie usuários separados para caixa/estoque",
            level: hasUsers ? "ok" : "warn",
            view: "users"
        }
    ];

    els.adminReadinessList.innerHTML = items.map((item) => `
        <button class="list-item list-button readiness-item" type="button" data-view-target="${item.view}">
            <div>
                <strong>${escapeHtml(item.title)}</strong>
                <span>${escapeHtml(item.detail)}</span>
            </div>
            <span class="badge ${item.level === "ok" ? "badge-ok" : "badge-warn"}">${item.level === "ok" ? "Ok" : "Ajustar"}</span>
        </button>
    `).join("");
}

function renderPendingActions() {
    const pending = [];
    const receivedOrders = state.orders.filter((order) => order.status === "Recebido");
    const paidOrders = state.orders.filter((order) => order.status === "Pago");
    const sentWithoutTracking = state.orders.filter((order) => order.status === "Enviado" && !order.codigoRastreio);
    const lowStock = getLowStockItems();
    const unpublishedProducts = state.products.filter((product) => product.ativo && !product.publicadoNaLoja);

    if (can("viewOnlineOrders") && receivedOrders.length) {
        pending.push({
            title: "Confirmar pagamentos online",
            detail: `${receivedOrders.length} ${receivedOrders.length === 1 ? "pedido aguardando" : "pedidos aguardando"}`,
            badge: "badge-warn",
            action: "orders",
            status: "Recebido"
        });
    }

    if (can("viewOnlineOrders") && paidOrders.length) {
        pending.push({
            title: "Separar pedidos pagos",
            detail: `${paidOrders.length} ${paidOrders.length === 1 ? "pedido pronto" : "pedidos prontos"} para separação`,
            badge: "badge-info",
            action: "orders",
            status: "Pago"
        });
    }

    if (can("viewOnlineOrders") && sentWithoutTracking.length) {
        pending.push({
            title: "Adicionar rastreio",
            detail: `${sentWithoutTracking.length} ${sentWithoutTracking.length === 1 ? "pedido enviado sem código" : "pedidos enviados sem código"}`,
            badge: "badge-info",
            action: "orders",
            status: "Enviado"
        });
    }

    if (can("manageStock") && lowStock.length) {
        pending.push({
            title: "Repor estoque crítico",
            detail: `${lowStock.length} ${lowStock.length === 1 ? "item com 3 un. ou menos" : "itens com 3 un. ou menos"}`,
            badge: "badge-warn",
            action: "stock"
        });
    }

    if (can("manageStorefront") && unpublishedProducts.length) {
        pending.push({
            title: "Publicar produtos no site",
            detail: `${unpublishedProducts.length} ${unpublishedProducts.length === 1 ? "produto ativo fora da loja online" : "produtos ativos fora da loja online"}`,
            badge: "badge-muted",
            action: "storefront"
        });
    }

    els.pendingActionsList.innerHTML = pending.length
        ? pending.map((item) => `
            <button class="list-item list-button" type="button" data-pending-action="${item.action}" data-status="${item.status || ""}">
                <div>
                    <strong>${escapeHtml(item.title)}</strong>
                    <span>${escapeHtml(item.detail)}</span>
                </div>
                <span class="badge ${item.badge}">Abrir</span>
            </button>
        `).join("")
        : `<div class="empty-state">Nenhuma ação urgente agora.</div>`;
}

function openPendingAction(action, status) {
    if (action === "orders" && canView("onlineOrders")) {
        state.onlineOrderStatusFilter = status || "all";
        els.onlineOrderStatusFilter.value = state.onlineOrderStatusFilter;
        showView("onlineOrders");
        renderOnlineOrders();
        return;
    }

    if (action === "stock" && canView("stock")) {
        showView("stock");
        return;
    }

    if (action === "storefront" && canView("storefront")) {
        showView("storefront");
    }
}

function getDashboardSummary() {
    if (state.summary) {
        return state.summary;
    }

    const activeSales = state.sales.filter((sale) => !sale.devolvida);
    const faturamentoLoja = activeSales.reduce((sum, sale) => sum + sale.total, 0);
    return {
        faturamentoTotal: faturamentoLoja,
        faturamentoLoja,
        vendasLoja: activeSales.length,
        pedidosOnline: state.orders.length,
        produtosAtivos: state.products.filter((product) => product.ativo).length,
        produtosComEstoqueBaixo: state.products.filter((product) => product.ativo && product.quantidadeEmEstoque <= 3).length
    };
}

function renderCategories() {
    els.categoryCount.textContent = `${state.categories.length} cadastradas`;

    const parents = state.categories.filter((category) => !category.categoriaPaiId);
    const renderChip = (category, extraClass = "") => `
        <span class="chip ${extraClass}">
            ${escapeHtml(category.nome)}
            <button class="chip-remove" type="button" data-remove-category="${category.id}" title="Excluir categoria">×</button>
        </span>
    `;
    els.categoryList.innerHTML = state.categories.length
        ? parents.map((parent) => {
            const children = state.categories.filter((category) => category.categoriaPaiId === parent.id);
            return `
                <div class="category-chip-group">
                    ${renderChip(parent)}
                    ${children.length ? `<div class="category-chip-group-children">${children.map((child) => renderChip(child, "chip-sub")).join("")}</div>` : ""}
                </div>
            `;
        }).join("")
        : `<div class="empty-state">Cadastre a primeira categoria.</div>`;

    els.categoryParent.innerHTML = [`<option value="">Categoria principal</option>`]
        .concat(parents.map((category) => `<option value="${category.id}">Subcategoria de ${escapeHtml(category.nome)}</option>`))
        .join("");
}

function renderCategoryOptions() {
    const parents = state.categories.filter((category) => !category.categoriaPaiId);
    const options = parents
        .map((category) => {
            const children = state.categories.filter((child) => child.categoriaPaiId === category.id);
            const own = `<option value="${category.id}">${escapeHtml(category.nome)}</option>`;
            const childOptions = children
                .map((child) => `<option value="${child.id}">&nbsp;&nbsp;— ${escapeHtml(child.nome)}</option>`)
                .join("");
            return own + childOptions;
        })
        .join("");

    els.productCategory.innerHTML = options;

    if (els.productCategoryFilter) {
        const currentFilter = els.productCategoryFilter.value;
        els.productCategoryFilter.innerHTML = `<option value="">Todas as categorias</option>${options}`;
        if ([...els.productCategoryFilter.options].some((option) => option.value === currentFilter)) {
            els.productCategoryFilter.value = currentFilter;
        }
    }

    renderSupplierOptions();
    renderStockVariationFields();
}

function renderSupplierOptions() {
    if (!els.stockSupplier) {
        return;
    }

    const activeSuppliers = state.suppliers.filter((supplier) => supplier.ativo);
    els.stockSupplier.innerHTML = [
        '<option value="">Sem fornecedor</option>',
        ...activeSuppliers.map((supplier) => `<option value="${supplier.id}">${escapeHtml(supplier.nome)}</option>`)
    ].join("");
}

function renderStockProductPicker() {
    if (!els.stockProductGrid) {
        return;
    }

    const term = normalize(state.stockPickerSearch);
    const products = state.products
        .filter((product) => product.ativo)
        .filter((product) => normalize(`${product.nome} ${product.sku || ""} ${product.categoria}`).includes(term));

    els.stockProductGrid.innerHTML = products.length
        ? products.map((product) => `
            <button
                class="product-pick product-pick-compact ${product.id === els.stockProduct.value ? "is-selected" : ""}"
                type="button"
                data-stock-product="${product.id}"
            >
                <strong>${escapeHtml(product.nome)}</strong>
                <span>${escapeHtml(product.categoria)}${product.sku ? ` · ${escapeHtml(product.sku)}` : ""}</span>
                <span>${product.quantidadeEmEstoque} un. em estoque</span>
            </button>
        `).join("")
        : `<div class="empty-state">Nenhum produto encontrado.</div>`;
}

function renderStockVariationFields() {
    if (!els.stockProduct) {
        return;
    }

    const product = state.products.find((item) => item.id === els.stockProduct.value);
    renderStockVariationSelect(els.stockSizeWrap, els.stockSize, product?.tamanhos || []);
    renderStockVariationSelect(els.stockColorWrap, els.stockColor, product?.cores || []);
    renderStockVariationSelect(els.stockModelWrap, els.stockModel, product?.modelos || []);
}

function renderStockVariationSelect(wrapper, select, options) {
    const visible = options.length > 0;
    wrapper.classList.toggle("hidden", !visible);
    select.disabled = !visible;
    select.innerHTML = visible
        ? [
            '<option value="">Selecione</option>',
            ...options.map((option) => `<option value="${escapeHtml(option)}">${escapeHtml(option)}</option>`)
        ].join("")
        : '<option value=""></option>';
}

const COLOR_SWATCHES = {
    preto: "#111111", branco: "#f5f5f5", cinza: "#9a9a9a", chumbo: "#4a4a4a",
    marrom: "#6b4226", caramelo: "#b3722c", bege: "#e3d0ad", nude: "#dcb89c",
    azul: "#2f5da0", "azul marinho": "#1c2f52", "azul claro": "#7fb4e0",
    vermelho: "#b3312c", vinho: "#5e1f26", rosa: "#e3a0c0", pink: "#e0428a",
    verde: "#3f7a4e", "verde militar": "#5c6a45", amarelo: "#e0c23f",
    laranja: "#d97b2c", roxo: "#6a3f92", lilas: "#b79fd1", dourado: "#c9a24b",
    prata: "#b7b7b7", "off white": "#efe8dc"
};

function colorSwatchHex(colorName) {
    const key = normalize(colorName || "").trim();
    return COLOR_SWATCHES[key] || null;
}

function renderProductsTable() {
    const term = normalize(state.productSearch);
    const categoryFilter = state.productCategoryFilter;
    const childCategoryIds = categoryFilter
        ? state.categories.filter((category) => category.categoriaPaiId === categoryFilter).map((category) => category.id)
        : [];

    const products = state.products
        .filter((product) => {
            if (!categoryFilter) {
                return true;
            }
            return product.categoriaId === categoryFilter || childCategoryIds.includes(product.categoriaId);
        })
        .filter((product) => {
            const searchable = normalize(`${product.nome} ${product.sku || ""} ${product.categoria} ${product.descricao || ""} ${(product.tamanhos || []).join(" ")} ${(product.cores || []).join(" ")} ${(product.modelos || []).join(" ")}`);
            return searchable.includes(term);
        });

    const totalUnidades = products.reduce((total, product) => total + Number(product.quantidadeEmEstoque || 0), 0);
    els.productCount.textContent = `${products.length} ${products.length === 1 ? "produto" : "produtos"} · ${totalUnidades} un. em estoque`;

    if (!products.length) {
        els.productsTable.innerHTML = `<tr><td colspan="7"><div class="empty-state">Nenhum produto encontrado.</div></td></tr>`;
        return;
    }

    const groups = new Map();
    products.forEach((product) => {
        const key = product.categoria || "Sem categoria";
        if (!groups.has(key)) {
            groups.set(key, []);
        }
        groups.get(key).push(product);
    });

    const sortedGroupNames = [...groups.keys()].sort((a, b) => a.localeCompare(b, "pt-BR"));

    els.productsTable.innerHTML = sortedGroupNames.map((groupName) => {
        const groupProducts = groups.get(groupName)
            .slice()
            .sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR") || (a.cores?.[0] || "").localeCompare(b.cores?.[0] || "", "pt-BR"));

        const rows = groupProducts.map((product) => {
            const margin = product.preco > 0 ? ((product.preco - (product.custo || 0)) / product.preco) * 100 : 0;
            const identityChips = [
                ...(product.cores?.length ? product.cores.map((cor) => {
                    const hex = colorSwatchHex(cor);
                    const dot = hex ? `<span class="chip-swatch" style="background:${hex}"></span>` : "";
                    return `<span class="chip chip-sub">${dot}${escapeHtml(cor)}</span>`;
                }) : []),
                ...(product.tamanhos?.length ? product.tamanhos.map((tam) => `<span class="chip chip-sub">${escapeHtml(tam)}</span>`) : []),
                ...(product.modelos?.length ? product.modelos.map((modelo) => `<span class="chip chip-sub">${escapeHtml(modelo)}</span>`) : [])
            ].join("");

            const details = [
                product.sku ? `SKU ${product.sku}` : null,
                product.custo ? `Custo ${currency.format(product.custo)} · Margem ${margin.toFixed(1)}%` : null,
                product.variacoesEstoque?.length ? `${product.variacoesEstoque.length} variações com estoque` : null
            ].filter(Boolean).join(" · ");

            const primaryColor = colorSwatchHex(product.cores?.[0]);
            const thumbImage = getStorefrontImage(product);
            const thumb = thumbImage
                ? `<img class="product-thumb" src="${escapeHtml(thumbImage)}" alt="${escapeHtml(product.nome)}">`
                : `<span class="product-thumb product-thumb-empty" ${primaryColor ? `style="background:${primaryColor}"` : ""}>${escapeHtml((product.nome || "?").charAt(0).toUpperCase())}</span>`;

            return `
                <tr data-product-row="${product.id}">
                    <td>${thumb}</td>
                    <td>
                        <strong>${escapeHtml(product.nome)}</strong>
                        ${identityChips ? `<div class="product-row-chips">${identityChips}</div>` : ""}
                        <span class="panel-note">${escapeHtml(details || product.descricao || "Sem descrição")}</span>
                    </td>
                    <td>${escapeHtml(product.categoria)}</td>
                    <td>${currency.format(product.preco)}</td>
                    <td>${stockBadge(product)}</td>
                    <td>${product.ativo ? '<span class="badge badge-ok">Ativo</span>' : '<span class="badge badge-muted">Inativo</span>'}</td>
                    <td>
                        <div class="table-actions">
                            <button class="button button-secondary" type="button" data-action="edit" data-id="${product.id}" title="Editar produto">Editar</button>
                            <button class="button button-secondary" type="button" data-action="labels" data-id="${product.id}" title="Imprimir etiquetas">Etiquetas</button>
                            <button class="button button-danger" type="button" data-action="delete" data-id="${product.id}" title="Excluir produto">Excluir</button>
                        </div>
                    </td>
                </tr>
            `;
        }).join("");

        return `
            <tr class="table-group-row">
                <td colspan="7">${escapeHtml(groupName)} <span class="panel-note">· ${groupProducts.length} ${groupProducts.length === 1 ? "item" : "itens"}</span></td>
            </tr>
            ${rows}
        `;
    }).join("");
}

function renderStorefront() {
    const term = normalize(state.storefrontSearch);
    const products = state.products.filter((product) => {
        const searchable = normalize(`${product.nome} ${product.nomeLoja || ""} ${product.sku || ""} ${product.categoria} ${product.descricao || ""} ${product.descricaoLoja || ""} ${(product.tamanhos || []).join(" ")} ${(product.cores || []).join(" ")} ${(product.modelos || []).join(" ")}`);
        return searchable.includes(term);
    });
    const publishedCount = state.products.filter((product) => product.ativo && product.publicadoNaLoja).length;
    const selectedId = els.storefrontProductId.value || els.storefrontProductSelect.value || state.products[0]?.id || "";

    els.storefrontCount.textContent = `${publishedCount} publicados`;
    els.storefrontProductSelect.innerHTML = state.products
        .map((product) => `<option value="${product.id}" ${product.id === selectedId ? "selected" : ""}>${escapeHtml(product.nome)} · ${product.quantidadeEmEstoque} un.</option>`)
        .join("");

    els.storefrontProductList.innerHTML = products.length
        ? products.map((product) => {
            const image = getStorefrontImage(product);
            const visual = image
                ? `<img src="${escapeHtml(image)}" alt="${escapeHtml(getStorefrontName(product))}">`
                : `<span>${escapeHtml(product.nome.slice(0, 2).toUpperCase())}</span>`;
            const status = product.ativo && product.publicadoNaLoja
                ? '<span class="badge badge-ok">No site</span>'
                : '<span class="badge badge-muted">Oculto</span>';
            const featured = product.destaqueLoja ? '<span class="badge badge-warn">Destaque</span>' : "";

            return `
                <article class="site-product-card">
                    <div class="site-product-media">${visual}</div>
                    <div>
                        <strong>${escapeHtml(getStorefrontName(product))}</strong>
                        <span>${escapeHtml(product.nome)} · ${escapeHtml(product.categoria)}${product.sku ? ` · SKU ${escapeHtml(product.sku)}` : ""}</span>
                        <div class="site-product-badges">${status}${featured}</div>
                    </div>
                    <div class="site-product-side">
                        <strong>${currency.format(getStorefrontPrice(product))}</strong>
                        <span>${product.quantidadeEmEstoque} un. no estoque</span>
                        <button class="button button-secondary" type="button" data-site-action="edit" data-id="${product.id}">Editar site</button>
                    </div>
                </article>
            `;
        }).join("")
        : `<div class="empty-state">Nenhum produto encontrado para configurar no site.</div>`;

    if (!els.storefrontProductId.value && state.products[0]) {
        editStorefrontProduct(state.products.find((product) => product.id === selectedId) || state.products[0], false, false);
    }
}

function renderSiteConfig() {
    const config = state.siteConfig;
    if (!config) {
        return;
    }

    els.siteCreatorNameInput.value = config.nomeCriadorSite || "";
    els.shippingBasePrice.value = config.freteValorPadrao ?? 0;
    els.shippingFreeThreshold.value = config.freteGratisAcimaDe ?? 0;
    els.shippingMinDays.value = config.prazoMinimoDias ?? 0;
    els.shippingMaxDays.value = config.prazoMaximoDias ?? 0;
    els.shippingMessageInput.value = config.mensagemFrete || "";
    els.customerLoginMessageInput.value = config.mensagemLoginCliente || "";
    els.bannerEyebrowInput.value = config.bannerEyebrow || "";
    els.bannerTitleInput.value = config.bannerTitulo || "";
    els.bannerDescriptionInput.value = config.bannerDescricao || "";
    els.bannerPrimaryButtonInput.value = config.bannerBotaoPrimario || "";
    els.bannerSecondaryButtonInput.value = config.bannerBotaoSecundario || "";
    els.bannerImageInput.value = config.bannerImagemUrl || "";
    els.sitePromoTextInput.value = config.promocaoTopoTexto || "";
    els.campaignTitleInput.value = config.campanhaTitulo || "";
    els.campaignDescriptionInput.value = config.campanhaDescricao || "";
    els.campaignButtonInput.value = config.campanhaBotaoTexto || "";
    els.campaignImageInput.value = config.campanhaImagemUrl || "";
    els.lookbookTitle1Input.value = config.vitrineImagem1Titulo || "";
    els.lookbookImage1Input.value = config.vitrineImagem1Url || "";
    els.lookbookTitle2Input.value = config.vitrineImagem2Titulo || "";
    els.lookbookImage2Input.value = config.vitrineImagem2Url || "";
    els.lookbookTitle3Input.value = config.vitrineImagem3Titulo || "";
    els.lookbookImage3Input.value = config.vitrineImagem3Url || "";
    renderSiteImagePreviews();
    els.storeWhatsappInput.value = config.whatsappLoja || "";
    els.storeInstagramInput.value = config.instagramLoja || "";
    els.storeAddressInput.value = config.enderecoLoja || "";
    renderSiteContactAdminPreview();
    els.pixKeyInput.value = config.pixChave || "";
    els.pixReceiverNameInput.value = config.pixNomeRecebedor || "";
    els.pixCityInput.value = config.pixCidade || "";
    els.pixOnlineActiveInput.checked = config.pixOnlineAtivo !== false;
    els.cardOnlineActiveInput.checked = config.cartaoOnlineAtivo !== false;
    els.cardCheckoutNameInput.value = config.checkoutCartaoNome || "";
    els.cardCheckoutUrlInput.value = config.checkoutCartaoUrl || "";
    els.paymentMessageInput.value = config.mensagemPagamento || "";
    els.cardPaymentMessageInput.value = config.mensagemPagamentoCartao || "";
    els.emailNotificationsActiveInput.checked = Boolean(config.emailNotificacoesAtivo);
    els.emailProviderInput.value = config.emailProvedor || "Brevo";
    els.brevoApiKeyInput.value = "";
    els.brevoApiKeyInput.placeholder = config.brevoApiKeyConfigurada ? "Chave Brevo já configurada" : "Cole a chave criada na Brevo";
    els.emailSenderInput.value = config.emailRemetente || "";
    els.emailOrdersInput.value = config.emailPedidosDestino || "";
    els.smtpHostInput.value = config.smtpHost || "";
    els.smtpPortInput.value = config.smtpPorta || 587;
    els.smtpUserInput.value = config.smtpUsuario || "";
    els.smtpPasswordInput.value = "";
    els.smtpSslInput.checked = config.smtpSsl !== false;
    els.autoBackupActiveInput.checked = config.backupAutomaticoAtivo !== false;
    els.autoBackupIntervalInput.value = config.backupIntervaloHoras || 24;
    els.paymentGatewayProviderInput.value = config.gatewayPagamentoProvedor || "";
    els.paymentGatewayActiveInput.checked = Boolean(config.gatewayPagamentoAtivo);
    els.paymentGatewayProductionInput.checked = Boolean(config.gatewayPagamentoProducao);
    els.paymentGatewayPublicKeyInput.value = config.gatewayPagamentoPublicKey || "";
    els.paymentGatewayAccessTokenInput.value = "";
    els.paymentGatewayAccessTokenInput.placeholder = config.gatewayPagamentoAccessTokenConfigurado ? "Token já configurado" : "Cole o token do provedor";
    els.paymentGatewayWebhookSecretInput.value = "";
    els.paymentGatewayWebhookSecretInput.placeholder = config.gatewayPagamentoWebhookSecretConfigurado ? "Segredo já configurado" : "Cole o segredo do webhook";
    els.paymentGatewayWebhookUrlInput.value = config.gatewayPagamentoWebhookUrl || "";
    els.privacyPolicyInput.value = config.politicaPrivacidade || "";
    els.exchangePolicyInput.value = config.politicaTrocaDevolucao || "";
    els.companyLegalNameInput.value = config.razaoSocial || "";
    els.companyCnpjInput.value = config.cnpj || "";
    els.siteCanonicalUrlInput.value = config.siteUrlCanonica || "";
    els.googleAnalyticsIdInput.value = config.googleAnalyticsId || "";
    els.metaPixelIdInput.value = config.metaPixelId || "";
    els.backupEmailActiveInput.checked = Boolean(config.backupEmailAtivo);
    els.backupEmailDestinationInput.value = config.backupEmailDestino || "";
}

function renderDeliveryOptions() {
    els.deliveryOptionCount.textContent = `${state.deliveryOptions.length} ${state.deliveryOptions.length === 1 ? "cadastrada" : "cadastradas"}`;
    els.deliveryOptionList.innerHTML = state.deliveryOptions.length
        ? state.deliveryOptions.map((option) => {
            const status = option.ativo
                ? '<span class="badge badge-ok">No site</span>'
                : '<span class="badge badge-muted">Inativa</span>';
            const range = [formatCepRange(option), formatListRange("Cidades", option.cidades), formatListRange("Bairros", option.bairros), formatListRange("UF", option.estados)]
                .filter(Boolean)
                .join(" · ") || "Atende todas as regiões";
            const shippingText = option.valor === 0 ? "Grátis" : currency.format(option.valor);
            const freeText = option.freteGratisAcimaDe > 0 ? `Grátis acima de ${currency.format(option.freteGratisAcimaDe)}` : "Sem faixa grátis";

            return `
                <article class="site-product-card delivery-card">
                    <div>
                        <strong>${escapeHtml(option.nome)}</strong>
                        <span>${formatDeliveryType(option.tipo)} · ${escapeHtml(option.descricao || "Sem descrição")}</span>
                        <div class="site-product-badges">${status}</div>
                    </div>
                    <div>
                        <strong>${shippingText}</strong>
                        <span>${freeText} · ${option.prazoMinimoDias} a ${option.prazoMaximoDias} dias</span>
                        <span>${escapeHtml(range)}</span>
                    </div>
                    <div class="site-product-side">
                        <span>Ordem ${option.ordem}</span>
                        <button class="button button-secondary" type="button" data-delivery-action="edit" data-id="${option.id}">Editar entrega</button>
                    </div>
                </article>
            `;
        }).join("")
        : `<div class="empty-state">Cadastre retirada, motoboy, correios ou transportadora.</div>`;
}

function renderCoupons() {
    els.couponCount.textContent = `${state.coupons.length} ${state.coupons.length === 1 ? "cadastrado" : "cadastrados"}`;
    els.couponList.innerHTML = state.coupons.length
        ? state.coupons.map((coupon) => {
            const status = coupon.disponivelNaLoja
                ? '<span class="badge badge-ok">Disponível no site</span>'
                : coupon.ativo
                    ? '<span class="badge badge-warn">Indisponível</span>'
                    : '<span class="badge badge-muted">Inativo</span>';
            const validade = coupon.validoAte ? `Validade ${formatShortDate(coupon.validoAte)}` : "Sem validade definida";

            return `
                <article class="site-product-card coupon-card">
                    <div>
                        <strong>${escapeHtml(coupon.codigo)}</strong>
                        <span>${escapeHtml(coupon.descricao || "Sem descrição")}</span>
                        <div class="site-product-badges">${status}</div>
                    </div>
                    <div>
                        <strong>${Number(coupon.percentualDesconto || 0).toFixed(2)}% OFF</strong>
                        <span>Acima de ${currency.format(coupon.valorMinimoPedido || 0)} · ${escapeHtml(validade)}</span>
                    </div>
                    <div class="site-product-side">
                        <span>Atualizado em ${formatDate(coupon.atualizadoEm)}</span>
                        <button class="button button-secondary" type="button" data-coupon-action="edit" data-id="${coupon.id}">Editar cupom</button>
                    </div>
                </article>
            `;
        }).join("")
        : `<div class="empty-state">Cadastre o primeiro cupom para aparecer no site.</div>`;
}

function renderPdvProducts() {
    const term = normalize(state.pdvSearch);
    const products = state.products
        .filter((product) => product.ativo)
        .filter((product) => normalize(`${product.nome} ${product.sku || ""} ${product.categoria}`).includes(term));

    els.pdvProductGrid.innerHTML = products.length
        ? products.map((product) => {
            const cartQty = cartQuantity(product.id);
            const available = Math.max(product.quantidadeEmEstoque - cartQty, 0);
            const initials = product.nome.split(" ").slice(0, 2).map((part) => part[0]).join("").toUpperCase();
            const pdvImage = getStorefrontImage(product);
            const image = pdvImage
                ? `<img src="${escapeHtml(pdvImage)}" alt="${escapeHtml(product.nome)}">`
                : `<span>${escapeHtml(initials)}</span>`;
            const variationControls = renderPdvVariationControls(product);

            return `
                <article class="product-pick" data-pdv-product="${product.id}">
                    <div class="product-thumb">${image}</div>
                    <div>
                        <strong>${escapeHtml(product.nome)}</strong>
                        <span>${escapeHtml(product.categoria)}${product.sku ? ` · SKU ${escapeHtml(product.sku)}` : ""} · ${available} un. disponíveis</span>
                    </div>
                    ${variationControls}
                    <footer>
                        <strong>${currency.format(product.preco)}</strong>
                        <button class="button button-secondary" type="button" data-add-product="${product.id}" ${available === 0 ? "disabled" : ""}>${variationControls ? "Escolher" : "Adicionar"}</button>
                    </footer>
                </article>
            `;
        }).join("")
        : `<div class="empty-state">Nenhum produto disponível para venda.</div>`;
}

function renderPdvVariationControls(product) {
    const controls = [
        renderPdvVariationSelect("tamanho", "Tam.", product.tamanhos || []),
        renderPdvVariationSelect("cor", "Cor", product.cores || []),
        renderPdvVariationSelect("modelo", "Modelo", product.modelos || [])
    ].filter(Boolean);

    if (!controls.length) {
        return "";
    }

    return `<div class="pdv-variation-picker">${controls.join("")}</div>`;
}

function renderPdvVariationSelect(field, label, options) {
    if (!options.length) {
        return "";
    }

    return `
        <label>
            <span>${label}</span>
            <select data-pdv-variation="${field}">
                ${options.map((option) => `<option value="${escapeHtml(option)}">${escapeHtml(option)}</option>`).join("")}
            </select>
        </label>
    `;
}

function getPdvCardVariation(card) {
    return {
        tamanho: card.querySelector('[data-pdv-variation="tamanho"]')?.value || null,
        cor: card.querySelector('[data-pdv-variation="cor"]')?.value || null,
        modelo: card.querySelector('[data-pdv-variation="modelo"]')?.value || null
    };
}

function addPdvProductByExactCode(event) {
    event.preventDefault();
    const term = normalize(els.pdvSearch.value);
    if (!term) {
        return;
    }

    let matchedVariation = null;
    const product = state.products.find((item) => {
        if (!item.ativo) {
            return false;
        }
        if (normalize(item.sku || "") === term || normalize(item.nome) === term) {
            return true;
        }
        const variation = (item.variacoesEstoque || []).find((v) => normalize(v.sku || "") === term);
        if (variation) {
            matchedVariation = variation;
            return true;
        }
        return false;
    });

    if (!product) {
        showToast("Nenhum produto com esse SKU exato.");
        return;
    }

    addToCart(product.id, matchedVariation);
    els.pdvSearch.value = "";
    state.pdvSearch = "";
    renderPdvProducts();
}

function renderCart() {
    const totalItems = state.cart.reduce((sum, item) => sum + item.quantidade, 0);
    const subtotal = cartTotal();
    const discount = getSaleDiscount();
    const total = Math.max(0, subtotal - discount);
    const received = Number(els.saleReceived.value || 0);
    const change = Math.max(0, received - total);

    els.cartCount.textContent = `${totalItems} itens`;
    els.cartSubtotal.textContent = currency.format(subtotal);
    els.cartTotal.textContent = currency.format(total);
    els.saleChange.textContent = currency.format(change);
    els.finishSaleButton.disabled = state.cart.length === 0;
    if (discount > subtotal && els.saleDiscount.value) {
        els.saleDiscount.value = subtotal.toFixed(2);
    }

    els.cartList.innerHTML = state.cart.length
        ? state.cart.map((item) => {
            const product = state.products.find((productItem) => productItem.id === item.produtoId);
            if (!product) {
                return "";
            }

            const variation = formatPdvVariation(item);
            return `
                <div class="cart-item">
                    <div class="cart-line">
                        <div>
                            <strong>${escapeHtml(product.nome)}</strong>
                            <span>${currency.format(product.preco)} cada</span>
                            ${variation ? `<span>${escapeHtml(variation)}</span>` : ""}
                        </div>
                        <button class="button button-danger" type="button" data-cart-action="remove" data-id="${escapeHtml(item.id)}" title="Remover item">Remover</button>
                    </div>
                    <div class="cart-line">
                        <div class="qty-control">
                            <button type="button" data-cart-action="decrease" data-id="${escapeHtml(item.id)}" title="Diminuir quantidade">-</button>
                            <span>${item.quantidade}</span>
                            <button type="button" data-cart-action="increase" data-id="${escapeHtml(item.id)}" title="Aumentar quantidade">+</button>
                        </div>
                        <strong>${currency.format(product.preco * item.quantidade)}</strong>
                    </div>
                </div>
            `;
        }).join("")
        : `<div class="empty-state">Carrinho vazio.</div>`;

    if (state.returnMode === "exchange" && !els.returnModal.classList.contains("hidden")) {
        renderExchangeSummary();
    }
}

function renderStockValueSummary() {
    const activeProducts = state.products.filter((product) => product.ativo);

    const totals = activeProducts.reduce((acc, product) => {
        const quantidade = Number(product.quantidadeEmEstoque || 0);
        acc.unidades += quantidade;
        acc.custo += quantidade * Number(product.custo || 0);
        acc.venda += quantidade * Number(product.preco || 0);
        return acc;
    }, { unidades: 0, custo: 0, venda: 0 });

    els.stockMetricUnits.textContent = totals.unidades;
    els.stockMetricCostValue.textContent = currency.format(totals.custo);
    els.stockMetricSaleValue.textContent = currency.format(totals.venda);
    els.stockMetricProfit.textContent = currency.format(totals.venda - totals.custo);

    const byCategory = new Map();
    activeProducts.forEach((product) => {
        const key = product.categoria || "Sem categoria";
        const quantidade = Number(product.quantidadeEmEstoque || 0);
        const entry = byCategory.get(key) || { produtos: 0, pecas: 0, custo: 0, venda: 0 };
        entry.produtos += 1;
        entry.pecas += quantidade;
        entry.custo += quantidade * Number(product.custo || 0);
        entry.venda += quantidade * Number(product.preco || 0);
        byCategory.set(key, entry);
    });

    const rows = [...byCategory.entries()].sort((a, b) => a[0].localeCompare(b[0], "pt-BR"));

    els.stockValueByCategoryTable.innerHTML = rows.length
        ? rows.map(([categoria, entry]) => `
            <tr>
                <td>${escapeHtml(categoria)}</td>
                <td>${entry.produtos}</td>
                <td>${entry.pecas}</td>
                <td>${currency.format(entry.custo)}</td>
                <td>${currency.format(entry.venda)}</td>
            </tr>
        `).join("")
        : `<tr><td colspan="5"><div class="empty-state">Nenhum produto ativo cadastrado.</div></td></tr>`;
}

function renderStock() {
    renderSuppliers();
    renderStockProductPicker();
    renderStockValueSummary();

    const maxStock = Math.max(...state.products.map((product) => product.quantidadeEmEstoque), 1);

    els.stockMap.innerHTML = state.products.length
        ? state.products
            .slice()
            .sort((a, b) => a.quantidadeEmEstoque - b.quantidadeEmEstoque)
            .map((product) => {
                const width = Math.max((product.quantidadeEmEstoque / maxStock) * 100, product.quantidadeEmEstoque > 0 ? 8 : 0);
                return `
                    <div class="stock-item">
                        <div>
                            <strong>${escapeHtml(product.nome)}</strong>
                            <span>${escapeHtml(product.categoria)}</span>
                            <div class="stock-bar"><span style="width: ${width}%"></span></div>
                        </div>
                        ${stockBadge(product)}
                    </div>
                `;
            }).join("")
        : `<div class="empty-state">Nenhum produto cadastrado.</div>`;

    els.movementCount.textContent = `${state.movements.length} registros`;
    els.movementsTable.innerHTML = state.movements.length
        ? state.movements.map((movement) => `
            <tr>
                <td>${formatDate(movement.criadaEm)}</td>
                <td>${escapeHtml(movement.produtoNome)}</td>
                <td>${formatMovement(movement.tipo)}</td>
                <td>${movement.quantidade}</td>
                <td>${movement.estoqueAposMovimento}</td>
                <td>${renderMovementDetails(movement)}</td>
            </tr>
        `).join("")
        : `<tr><td colspan="6"><div class="empty-state">Nenhuma movimentação registrada.</div></td></tr>`;
}

function renderSuppliers() {
    els.supplierCount.textContent = `${state.suppliers.length} cadastrados`;
    els.supplierList.innerHTML = state.suppliers.length
        ? state.suppliers.map((supplier) => {
            const details = [
                supplier.documento,
                supplier.telefone,
                supplier.email
            ].filter(Boolean).join(" · ");

            return `
                <div class="list-item">
                    <div>
                        <strong>${escapeHtml(supplier.nome)}</strong>
                        <span>${details ? escapeHtml(details) : "Sem contato cadastrado"}</span>
                    </div>
                    <span class="badge ${supplier.ativo ? "badge-ok" : "badge-muted"}">${supplier.ativo ? "Ativo" : "Inativo"}</span>
                </div>
            `;
        }).join("")
        : `<div class="empty-state">Cadastre fornecedores para vincular às entradas de mercadoria.</div>`;
}

function renderPanelUsers() {
    els.panelUserCount.textContent = `${state.panelUsers.length} ${state.panelUsers.length === 1 ? "usuário" : "usuários"}`;
    els.panelUserList.innerHTML = state.panelUsers.length
        ? state.panelUsers.map((user) => `
            <div class="list-item">
                <div>
                    <strong>${escapeHtml(user.nomeExibicao)}</strong>
                    <span>${escapeHtml(user.usuario)} · ${formatPanelRole(user.perfil)} · atualizado em ${formatDate(user.atualizadoEm)}</span>
                </div>
                <div class="table-actions">
                    <span class="badge ${user.ativo ? "badge-ok" : "badge-muted"}">${user.ativo ? "Ativo" : "Inativo"}</span>
                    <button class="button button-ghost" type="button" data-user-action="edit" data-id="${user.id}">Editar</button>
                </div>
            </div>
        `).join("")
        : `<div class="empty-state">Nenhum usuário cadastrado.</div>`;
}

function renderMovementDetails(movement) {
    const extras = [
        movement.fornecedorNome ? `Fornecedor: ${movement.fornecedorNome}` : null,
        movement.custoUnitario ? `Custo: ${currency.format(movement.custoUnitario)}` : null,
        movement.documento ? `Doc: ${movement.documento}` : null
    ].filter(Boolean).join(" · ");

    return `
        ${escapeHtml(movement.origem)}
        ${extras ? `<br><span class="panel-note">${escapeHtml(extras)}</span>` : ""}
    `;
}

function renderOnlineOrders() {
    const filteredOrders = getFilteredOnlineOrders();
    els.onlineOrdersCount.textContent = `${filteredOrders.length} ${filteredOrders.length === 1 ? "pedido" : "pedidos"}`;

    renderOnlineOrderStatusSummary();
    renderOnlineOrderOpsSummary();

    els.onlineOrderList.innerHTML = filteredOrders.length
        ? filteredOrders.map(renderOnlineOrderCard).join("")
        : `<div class="empty-state">Nenhum pedido encontrado para esse filtro.</div>`;
}

function renderOnlineOrderStatusSummary() {
    const counts = new Map(orderStatusOptions.map((status) => [status, 0]));
    state.orders.forEach((order) => {
        counts.set(order.status, (counts.get(order.status) || 0) + 1);
    });

    els.onlineOrderStatusSummary.innerHTML = orderStatusOptions.map((status) => `
        <button class="order-status-filter ${state.onlineOrderStatusFilter === status ? "is-active" : ""}" type="button" data-status-filter="${status}">
            <span>${formatOrderStatus(status)}</span>
            <strong>${counts.get(status) || 0}</strong>
        </button>
    `).join("");

    els.onlineOrderStatusSummary.querySelectorAll("[data-status-filter]").forEach((button) => {
        button.addEventListener("click", () => {
            state.onlineOrderStatusFilter = button.dataset.statusFilter;
            els.onlineOrderStatusFilter.value = state.onlineOrderStatusFilter;
            renderOnlineOrders();
        });
    });
}

function renderOnlineOrderOpsSummary() {
    if (!els.onlineOrderOpsSummary) {
        return;
    }

    const activeOrders = state.orders.filter((order) => order.status !== "Cancelado" && order.status !== "Entregue");
    const pendingPayment = state.orders.filter((order) => order.status === "Recebido").length;
    const paidOrders = state.orders.filter((order) => order.status === "Pago").length;
    const separating = state.orders.filter((order) => order.status === "Separando").length;
    const sentWithoutTracking = state.orders.filter((order) => order.status === "Enviado" && !order.codigoRastreio).length;
    const activeTotal = activeOrders.reduce((sum, order) => sum + Number(order.total || 0), 0);

    const items = [
        { label: "Fila ativa", value: activeOrders.length, note: currency.format(activeTotal) },
        { label: "A confirmar", value: pendingPayment, note: "pagamento" },
        { label: "Separar", value: paidOrders + separating, note: "preparo" },
        { label: "Sem rastreio", value: sentWithoutTracking, note: "envio" }
    ];

    els.onlineOrderOpsSummary.innerHTML = items.map((item) => `
        <div>
            <span>${escapeHtml(item.label)}</span>
            <strong>${item.value}</strong>
            <small>${escapeHtml(item.note)}</small>
        </div>
    `).join("");
}

function getFilteredOnlineOrders() {
    const term = normalize(state.onlineOrderSearch);
    return state.orders
        .filter((order) => state.onlineOrderStatusFilter === "all" || order.status === state.onlineOrderStatusFilter)
        .filter((order) => {
            const searchable = normalize([
                order.id,
                order.nomeCliente,
                order.emailCliente,
                order.telefoneCliente,
                order.enderecoEntrega,
                order.formaPagamento,
                order.status,
                ...(order.itens || []).map((item) => `${item.produtoNome} ${item.tamanho || ""} ${item.cor || ""} ${item.modelo || ""}`)
            ].join(" "));
            return searchable.includes(term);
        })
        .slice()
        .sort((a, b) => new Date(b.criadoEm) - new Date(a.criadoEm));
}

function renderOnlineOrderCard(order) {
    const shortId = order.id.slice(0, 8).toUpperCase();
    const statusClass = getOrderStatusClass(order.status);
    const items = (order.itens || []).map((item) => `
        <li>
            <span>${item.quantidade}x ${escapeHtml(item.produtoNome)}${formatOrderVariation(item) ? ` · ${escapeHtml(formatOrderVariation(item))}` : ""}</span>
            <strong>${currency.format(item.subtotal)}</strong>
        </li>
    `).join("");
    const addressParts = [
        order.ruaEntrega && order.numeroEntrega ? `${order.ruaEntrega}, ${order.numeroEntrega}` : null,
        order.complementoEntrega,
        order.bairroEntrega,
        [order.cidadeEntrega, order.estadoEntrega].filter(Boolean).join(" - "),
        order.cepEntrega
    ].filter(Boolean).join(" · ");
    const address = addressParts || order.enderecoEntrega || "Endereço não informado";
    const operationNotes = getOnlineOrderOperationNotes(order);

    return `
        <article class="online-order-card">
            <header class="online-order-head">
                <div>
                    <span>Pedido ${shortId}</span>
                    <strong>${escapeHtml(order.nomeCliente)}</strong>
                    <small>${formatDate(order.criadoEm)}</small>
                </div>
                <span class="badge ${statusClass}">${formatOrderStatus(order.status)}</span>
            </header>

            <div class="online-order-grid">
                <div>
                    <span>Contato</span>
                    <strong>${escapeHtml(order.emailCliente)}</strong>
                    <small>${escapeHtml(order.telefoneCliente || "Telefone não informado")}</small>
                </div>
                <div>
                    <span>Pagamento</span>
                    <strong>${formatPayment(order.formaPagamento)}</strong>
                    <small>${order.status === "Recebido" ? "Aguardando confirmação" : "Status atualizado"}</small>
                </div>
                <div>
                    <span>Total</span>
                    <strong>${currency.format(order.total)}</strong>
                    <small>${formatOrderTotalNote(order)}</small>
                </div>
            </div>

            <ul class="online-order-items">${items}</ul>

            <div class="online-order-address">
                <span>Entrega</span>
                <p>${escapeHtml(address)}</p>
                ${order.observacao ? `<p><strong>Obs.</strong> ${escapeHtml(order.observacao)}</p>` : ""}
            </div>

            <div class="online-order-operation">
                ${operationNotes.map((note) => `
                    <span class="${note.ok ? "is-ok" : "is-pending"}">${escapeHtml(note.label)}</span>
                `).join("")}
            </div>

            <div class="online-order-tracking online-order-payment">
                <label>
                    Referência / comprovante
                    <input type="text" value="${escapeHtml(order.referenciaPagamento || "")}" data-payment-reference="${order.id}" placeholder="ID Pix, NSU, link ou anotação">
                </label>
                <label>
                    Observação do pagamento
                    <textarea rows="2" data-payment-note="${order.id}" placeholder="Ex: comprovante recebido pelo WhatsApp">${escapeHtml(order.observacaoPagamento || "")}</textarea>
                </label>
                <label class="checkbox-line">
                    <input type="checkbox" data-payment-confirm="${order.id}" ${order.pagamentoConfirmadoEm || order.status === "Pago" ? "checked" : ""}>
                    Confirmado
                </label>
                ${order.pagamentoConfirmadoEm ? `<span>Pago em ${formatDate(order.pagamentoConfirmadoEm)}</span>` : order.pagamentoAtualizadoEm ? `<span>Atualizado em ${formatDate(order.pagamentoAtualizadoEm)}</span>` : ""}
            </div>

            <div class="online-order-tracking">
                <label>
                    Código de rastreio
                    <input type="text" value="${escapeHtml(order.codigoRastreio || "")}" data-tracking-code="${order.id}" placeholder="Ex: BR123456789">
                </label>
                <label>
                    Observação da entrega
                    <textarea rows="2" data-tracking-note="${order.id}" placeholder="Ex: saiu para entrega hoje">${escapeHtml(order.observacaoEntrega || "")}</textarea>
                </label>
                ${order.rastreamentoAtualizadoEm ? `<span>Atualizado em ${formatDate(order.rastreamentoAtualizadoEm)}</span>` : ""}
            </div>

            <footer class="online-order-actions">
                ${renderOrderStatusSelect(order, "online")}
                <div class="online-order-action-grid">
                    ${renderOnlineOrderQuickActions(order)}
                    <button class="button button-secondary" type="button" data-online-order-action="payment" data-id="${order.id}">Salvar pagamento</button>
                    <button class="button button-secondary" type="button" data-online-order-action="tracking" data-id="${order.id}">Salvar rastreio</button>
                    <button class="button button-secondary" type="button" data-online-order-action="receipt" data-id="${order.id}">Comprovante</button>
                    <button class="button button-secondary" type="button" data-online-order-action="copy" data-id="${order.id}">Copiar resumo</button>
                </div>
            </footer>
        </article>
    `;
}

function getOnlineOrderOperationNotes(order) {
    return [
        {
            ok: Boolean(order.pagamentoConfirmadoEm || ["Pago", "Separando", "Enviado", "Entregue"].includes(order.status)),
            label: order.pagamentoConfirmadoEm ? `Pagamento confirmado ${formatDate(order.pagamentoConfirmadoEm)}` : "Pagamento pendente"
        },
        {
            ok: Boolean(order.codigoRastreio || order.status !== "Enviado"),
            label: order.codigoRastreio ? `Rastreio ${order.codigoRastreio}` : "Rastreio pendente"
        },
        {
            ok: order.status === "Entregue",
            label: order.status === "Entregue" ? "Pedido entregue" : `Etapa atual: ${formatOrderStatus(order.status)}`
        }
    ];
}

function renderOnlineOrderQuickActions(order) {
    const actions = [];

    if (order.status === "Recebido") {
        actions.push({
            label: "Confirmar pagamento",
            action: "quick-payment"
        });
    }

    if (order.status === "Pago") {
        actions.push({ label: "Separar pedido", action: "quick-status", status: "Separando" });
    }

    if (order.status === "Separando") {
        actions.push({ label: "Marcar enviado", action: "quick-status", status: "Enviado" });
    }

    if (order.status === "Enviado") {
        actions.push({ label: "Marcar entregue", action: "quick-status", status: "Entregue" });
    }

    if (!["Cancelado", "Entregue"].includes(order.status)) {
        actions.push({ label: "Cancelar", action: "quick-status", status: "Cancelado", danger: true });
    }

    return actions.map((item) => `
        <button
            class="button ${item.danger ? "button-ghost" : "button-primary"}"
            type="button"
            data-online-order-action="${item.action}"
            data-id="${order.id}"
            ${item.status ? `data-status="${item.status}"` : ""}
        >${item.label}</button>
    `).join("");
}

function renderOnlineOrderReceipt(orderId) {
    const order = state.orders.find((item) => item.id === orderId);
    if (!order) {
        showToast("Pedido não encontrado.");
        return;
    }

    const shortId = order.id.slice(0, 8).toUpperCase();
    const address = [
        order.ruaEntrega && order.numeroEntrega ? `${order.ruaEntrega}, ${order.numeroEntrega}` : order.enderecoEntrega,
        order.complementoEntrega,
        order.bairroEntrega,
        [order.cidadeEntrega, order.estadoEntrega].filter(Boolean).join(" - "),
        order.cepEntrega
    ].filter(Boolean).join(" · ");

    els.onlineReceiptBox.classList.remove("hidden");
    els.onlineReceiptBox.innerHTML = `
        <div class="receipt-head">
            <div>
                <strong>Nana Modas</strong>
                <span>Pedido online ${shortId}</span>
            </div>
            <span>${formatDate(order.criadoEm)}</span>
        </div>
        <div class="receipt-meta">
            <span>${formatPayment(order.formaPagamento)}</span>
            <span class="badge ${getOrderStatusClass(order.status)}">${formatOrderStatus(order.status)}</span>
        </div>
        <div class="receipt-lines">
            ${(order.itens || []).map((item) => {
                const variation = formatOrderVariation(item);
                return `
                    <div>
                        <span>
                            ${item.quantidade}x ${escapeHtml(item.produtoNome)}
                            ${variation ? `<small>${escapeHtml(variation)}</small>` : ""}
                            <small>${currency.format(item.precoUnitario)} cada</small>
                        </span>
                        <strong>${currency.format(item.subtotal)}</strong>
                    </div>
                `;
            }).join("")}
        </div>
        <div class="receipt-total">
            <span>Produtos</span>
            <strong>${currency.format(order.totalBruto || 0)}</strong>
        </div>
        <div class="receipt-total">
            <span>Desconto</span>
            <strong>${currency.format(order.desconto || 0)}</strong>
        </div>
        <div class="receipt-total">
            <span>Entrega</span>
            <strong>${currency.format(order.entregaValor || 0)}</strong>
        </div>
        <div class="receipt-total is-final">
            <span>Total</span>
            <strong>${currency.format(order.total || 0)}</strong>
        </div>
        <p><strong>Cliente:</strong> ${escapeHtml(order.nomeCliente)} · ${escapeHtml(order.emailCliente)}${order.telefoneCliente ? ` · ${escapeHtml(order.telefoneCliente)}` : ""}</p>
        <p><strong>Entrega:</strong> ${escapeHtml(address || "Não informado")}</p>
        ${order.entregaNome ? `<p><strong>Forma de entrega:</strong> ${escapeHtml(order.entregaNome)}</p>` : ""}
        ${order.codigoRastreio ? `<p><strong>Rastreio:</strong> ${escapeHtml(order.codigoRastreio)}</p>` : ""}
        ${order.referenciaPagamento ? `<p><strong>Ref. pagamento:</strong> ${escapeHtml(order.referenciaPagamento)}</p>` : ""}
        ${order.observacao ? `<p><strong>Obs. cliente:</strong> ${escapeHtml(order.observacao)}</p>` : ""}
        <p>Comprovante gerado pelo painel Nana Modas.</p>
        <div class="receipt-actions">
            <button class="button button-secondary" type="button" data-online-receipt-action="copy" data-id="${order.id}">Copiar resumo</button>
            <button class="button button-secondary" type="button" data-online-receipt-action="print" data-id="${order.id}">Imprimir</button>
        </div>
    `;
    els.onlineReceiptBox.scrollIntoView({ behavior: "smooth", block: "nearest" });
}

function renderCustomers() {
    const customers = getFilteredCustomers();
    const totalCustomers = state.customers.length;
    const customersWithOrders = state.customers.filter((customer) => customer.pedidos > 0).length;
    const totalRevenue = state.customers.reduce((sum, customer) => sum + Number(customer.totalCompras || 0), 0);
    const averageTicket = customersWithOrders > 0 ? totalRevenue / customersWithOrders : 0;

    els.customerCount.textContent = `${customers.length} ${customers.length === 1 ? "cliente" : "clientes"}`;
    els.customerSummary.innerHTML = `
        <div class="payment-item">
            <span>Clientes cadastrados</span>
            <strong>${totalCustomers}</strong>
        </div>
        <div class="payment-item">
            <span>Com pedidos</span>
            <strong>${customersWithOrders}</strong>
        </div>
        <div class="payment-item">
            <span>Total comprado</span>
            <strong>${currency.format(totalRevenue)}</strong>
        </div>
        <div class="payment-item">
            <span>Média por cliente comprador</span>
            <strong>${currency.format(averageTicket)}</strong>
        </div>
    `;

    els.customerList.innerHTML = customers.length
        ? customers.map(renderCustomerCard).join("")
        : `<div class="empty-state">Nenhum cliente encontrado.</div>`;
}

function getFilteredCustomers() {
    const term = normalize(state.customerSearch);
    return state.customers
        .filter((customer) => {
            const searchable = normalize([
                customer.nome,
                customer.email,
                customer.telefone,
                customer.ultimoStatus ? formatOrderStatus(customer.ultimoStatus) : ""
            ].join(" "));
            return searchable.includes(term);
        })
        .slice()
        .sort((a, b) => {
            const dateA = new Date(a.ultimoPedidoEm || a.atualizadoEm || a.criadoEm).getTime();
            const dateB = new Date(b.ultimoPedidoEm || b.atualizadoEm || b.criadoEm).getTime();
            return dateB - dateA;
        });
}

function renderCustomerCard(customer) {
    const lastStatus = customer.ultimoStatus ? formatOrderStatus(customer.ultimoStatus) : "Sem pedido";
    const lastOrder = customer.ultimoPedidoEm ? formatDate(customer.ultimoPedidoEm) : "Ainda não comprou";
    const statusClass = customer.ultimoStatus ? getOrderStatusClass(customer.ultimoStatus) : "badge-muted";

    return `
        <article class="customer-card">
            <div class="customer-card-head">
                <div>
                    <span>Cliente</span>
                    <strong>${escapeHtml(customer.nome)}</strong>
                    <small>${escapeHtml(customer.email)}${customer.telefone ? ` · ${escapeHtml(customer.telefone)}` : ""}</small>
                </div>
                <span class="badge ${statusClass}">${lastStatus}</span>
            </div>

            <div class="customer-stats">
                <div>
                    <span>Pedidos</span>
                    <strong>${customer.pedidos}</strong>
                    <small>${customer.pedidosValidos} válidos</small>
                </div>
                <div>
                    <span>Total comprado</span>
                    <strong>${currency.format(customer.totalCompras || 0)}</strong>
                    <small>Pedidos não cancelados</small>
                </div>
                <div>
                    <span>Último pedido</span>
                    <strong>${lastOrder}</strong>
                    <small>Cadastrado em ${formatDate(customer.criadoEm)}</small>
                </div>
            </div>
        </article>
    `;
}

function renderReports() {
    const summary = state.summary || {};
    const reportData = buildFilteredReportData();
    renderBackups();
    if (els.reportKpiStrip) {
        const kpis = [
            { label: "Faturamento filtrado", value: currency.format(reportData.faturamentoTotal), detail: `${reportData.transacoesTotal} transações` },
            { label: "Lucro estimado", value: currency.format(reportData.lucroEstimado), detail: `Margem ${reportData.margemLucroPercentual.toFixed(2)}%` },
            { label: "Ticket médio", value: currency.format(reportData.ticketMedio), detail: "PDV + online" },
            { label: "Descontos", value: currency.format(reportData.descontosTotal), detail: `PDV ${currency.format(reportData.descontosPdv)} · Online ${currency.format(reportData.descontosOnline)}` }
        ];

        els.reportKpiStrip.innerHTML = kpis.map((item) => `
            <article>
                <span>${escapeHtml(item.label)}</span>
                <strong>${escapeHtml(item.value)}</strong>
                <small>${escapeHtml(item.detail)}</small>
            </article>
        `).join("");
    }

    const paymentItems = reportData.paymentItems;
    els.paymentSummary.innerHTML = paymentItems.length
        ? paymentItems.map((item) => `
            <div class="payment-item">
                <div>
                    <strong>${formatPayment(item.formaPagamento)}</strong>
                    <span>${item.quantidade} vendas</span>
                </div>
                <strong>${currency.format(item.total)}</strong>
            </div>
        `).join("")
        : `<div class="empty-state">Sem vendas para agrupar.</div>`;

    const inventoryValue = state.products.reduce((sum, product) => sum + (product.preco * product.quantidadeEmEstoque), 0);
    const totalUnits = state.products.reduce((sum, product) => sum + product.quantidadeEmEstoque, 0);
    els.reportIndicators.innerHTML = `
        <div class="report-item">
            <span>Valor estimado em estoque</span>
            <strong>${currency.format(inventoryValue)}</strong>
        </div>
        <div class="report-item">
            <span>Custo estimado em estoque</span>
            <strong>${currency.format(summary.custoEstoque || 0)}</strong>
        </div>
        <div class="report-item">
            <span>Unidades disponíveis</span>
            <strong>${totalUnits}</strong>
        </div>
        <div class="report-item">
            <span>Lucro estimado</span>
            <strong>${currency.format(reportData.lucroEstimado)}</strong>
        </div>
        <div class="report-item">
            <span>Margem estimada</span>
            <strong>${reportData.margemLucroPercentual.toFixed(2)}%</strong>
        </div>
        <div class="report-item">
            <span>Ticket médio</span>
            <strong>${currency.format(reportData.ticketMedio)}</strong>
        </div>
        <div class="report-item">
            <span>Descontos concedidos</span>
            <div class="report-money-stack">
                <strong>${currency.format(reportData.descontosTotal)}</strong>
                <small>PDV ${currency.format(reportData.descontosPdv)} · Online ${currency.format(reportData.descontosOnline)}</small>
            </div>
        </div>
        <div class="report-item">
            <span>Faturamento online</span>
            <strong>${currency.format(reportData.faturamentoOnline)}</strong>
        </div>
        <div class="report-item">
            <span>Faturamento PDV</span>
            <strong>${currency.format(reportData.faturamentoPdv)}</strong>
        </div>
    `;

    const topProducts = reportData.topProducts;
    els.topProductsReport.innerHTML = topProducts.length
        ? topProducts.map((product) => `
            <div class="payment-item">
                <div>
                    <strong>${escapeHtml(product.produtoNome)}</strong>
                    <span>${product.quantidade} unidades</span>
                </div>
                <strong>${currency.format(product.total)}</strong>
            </div>
        `).join("")
        : `<div class="empty-state">Sem produtos vendidos para ranquear.</div>`;

    const dailySales = reportData.dailySales;
    els.dailySalesReport.innerHTML = dailySales.length
        ? dailySales.map((day) => `
            <div class="payment-item">
                <div>
                    <strong>${formatReportDate(day.data)}</strong>
                    <span>${day.transacoes} transações · PDV ${day.vendasPdv} · Online ${day.pedidosOnline}</span>
                </div>
                <div class="report-money-stack">
                    <strong>${currency.format(day.faturamento)}</strong>
                    <span>Lucro ${currency.format(day.lucro)}</span>
                </div>
            </div>
        `).join("")
        : `<div class="empty-state">Sem vendas por dia para mostrar.</div>`;

    const supplierEntries = summary.entradasPorFornecedor || [];
    els.supplierEntriesReport.innerHTML = supplierEntries.length
        ? supplierEntries.map((entry) => `
            <div class="payment-item">
                <div>
                    <strong>${escapeHtml(entry.fornecedorNome)}</strong>
                    <span>${entry.entradas} entradas · ${entry.unidades} unidades${entry.ultimaEntrada ? ` · Última ${formatReportDate(entry.ultimaEntrada)}` : ""}</span>
                </div>
                <strong>${currency.format(entry.custoTotal)}</strong>
            </div>
        `).join("")
        : `<div class="empty-state">Sem entradas de fornecedor registradas.</div>`;

    const lowStockItems = getLowStockItems();
    els.lowStockReport.innerHTML = lowStockItems.length
        ? lowStockItems.map((item) => `
            <div class="payment-item">
                <div>
                    <strong>${escapeHtml(item.nome)}</strong>
                    <span>${escapeHtml(item.detalhe)}</span>
                </div>
                <span class="badge badge-warn">${item.quantidade} un.</span>
            </div>
        `).join("")
        : `<div class="empty-state">Nenhum produto em estoque crítico.</div>`;

    els.ordersCount.textContent = `${reportData.orders.length} pedidos`;
    els.ordersTable.innerHTML = reportData.orders.length
        ? reportData.orders.map((order) => `
            <tr>
                <td>${formatDate(order.criadoEm)}</td>
                <td>
                    <strong>${escapeHtml(order.nomeCliente)}</strong><br>
                    <span class="panel-note">${escapeHtml(order.emailCliente)}</span>
                </td>
                <td>${renderOrderStatusSelect(order)}</td>
                <td>${formatPayment(order.formaPagamento)}</td>
                <td>${order.itens.map(formatOrderItem).join("<br>")}</td>
                <td>${currency.format(order.total)}</td>
            </tr>
        `).join("")
        : `<tr><td colspan="6"><div class="empty-state">Nenhum pedido online registrado.</div></td></tr>`;

    els.salesCount.textContent = `${reportData.salesTable.length} vendas`;
    els.salesTable.innerHTML = reportData.salesTable.length
        ? reportData.salesTable.map((sale) => `
            <tr>
                <td>${formatDate(sale.criadaEm)}</td>
                <td>${formatPayment(sale.formaPagamento)}</td>
                <td>${sale.itens.map(formatSaleItem).join("<br>")}</td>
                <td>
                    ${currency.format(sale.total)}
                    ${sale.valorDevolvido ? `<br><span class="panel-note">Devolvido ${currency.format(sale.valorDevolvido)}</span>` : ""}
                    ${sale.desconto ? `<br><span class="panel-note">Desc. ${currency.format(sale.desconto)}</span>` : ""}
                </td>
                <td>${formatSaleStatusBadge(sale)}</td>
                <td>
                    <div class="table-actions">
                        <button class="button button-secondary" type="button" data-sale-action="receipt" data-id="${sale.id}">Comprovante</button>
                        <button class="button button-secondary" type="button" data-sale-action="exchange" data-id="${sale.id}" ${sale.devolvida ? "disabled" : ""}>Trocar</button>
                        <button class="button button-danger" type="button" data-sale-action="return" data-id="${sale.id}" ${sale.devolvida ? "disabled" : ""}>${sale.devolucaoParcial ? "Nova devolução" : "Devolver"}</button>
                    </div>
                </td>
            </tr>
        `).join("")
        : `<tr><td colspan="6"><div class="empty-state">Nenhuma venda registrada.</div></td></tr>`;
}

function renderBackups() {
    const backups = state.backups || [];
    els.backupFileCount.textContent = `${backups.length} ${backups.length === 1 ? "arquivo" : "arquivos"}`;
    els.backupFileList.innerHTML = backups.length
        ? backups.map((backup) => `
            <div class="list-item backup-file-item">
                <div>
                    <strong>${escapeHtml(backup.nomeArquivo)}</strong>
                    <span>${backup.automatico ? "Automático" : "Manual"} · ${formatDate(backup.criadoEm)} · ${formatBytes(backup.tamanhoBytes)}</span>
                </div>
                <button class="button button-secondary" type="button" data-backup-download="${escapeHtml(backup.nomeArquivo)}">Baixar</button>
            </div>
        `).join("")
        : `<div class="empty-state">Nenhum backup automático encontrado ainda.</div>`;
}

async function loadBackups() {
    try {
        state.backups = await api("/backup/arquivos");
        renderBackups();
        showToast("Lista de backups atualizada.");
    } catch (error) {
        showToast(error.message || "Não foi possível carregar backups.");
    }
}

function downloadBackupFile(nomeArquivo) {
    if (!nomeArquivo) {
        return;
    }

    window.location.href = `/backup/arquivos/${encodeURIComponent(nomeArquivo)}`;
    showToast("Download do backup iniciado.");
}

function formatBytes(value) {
    const bytes = Number(value || 0);
    if (bytes < 1024) {
        return `${bytes} B`;
    }

    const kb = bytes / 1024;
    if (kb < 1024) {
        return `${kb.toFixed(1)} KB`;
    }

    return `${(kb / 1024).toFixed(1)} MB`;
}

function buildFilteredReportData() {
    const startDate = parseReportDate(els.reportStartDate.value, false);
    const endDate = parseReportDate(els.reportEndDate.value, true);
    const channel = els.reportChannelFilter.value;
    const payment = els.reportPaymentFilter.value;

    const matchesCommonFilters = (item, dateField) => {
        const date = new Date(item[dateField]);
        if (startDate && date < startDate) {
            return false;
        }

        if (endDate && date > endDate) {
            return false;
        }

        return payment === "all" || item.formaPagamento === payment;
    };

    const salesTable = state.sales
        .filter((sale) => channel !== "online")
        .filter((sale) => matchesCommonFilters(sale, "criadaEm"));
    const activeSales = salesTable.filter((sale) => !sale.devolvida);
    const orders = state.orders
        .filter((order) => channel !== "pdv")
        .filter((order) => order.status !== "Cancelado")
        .filter((order) => matchesCommonFilters(order, "criadoEm"));
    const paidOrders = orders.filter((order) => order.status !== "Recebido");

    const transactions = [
        ...activeSales.map((sale) => ({
            channel: "pdv",
            date: sale.criadaEm,
            formaPagamento: sale.formaPagamento,
            total: Number(sale.total || 0),
            desconto: Number(sale.descontoLiquido ?? sale.desconto ?? 0),
            custo: (sale.itens || []).reduce((sum, item) => sum + getReportItemCost(item, getSaleItemRemaining(item)), 0)
        })),
        ...paidOrders.map((order) => ({
            channel: "online",
            date: order.criadoEm,
            formaPagamento: order.formaPagamento,
            total: Number(order.total || 0),
            desconto: Number(order.desconto || 0),
            custo: (order.itens || []).reduce((sum, item) => sum + getReportItemCost(item, item.quantidade), 0)
        }))
    ];

    const paymentMap = new Map();
    transactions.forEach((transaction) => {
        const current = paymentMap.get(transaction.formaPagamento) || {
            formaPagamento: transaction.formaPagamento,
            quantidade: 0,
            total: 0
        };
        current.quantidade += 1;
        current.total += transaction.total;
        paymentMap.set(transaction.formaPagamento, current);
    });

    const productMap = new Map();
    activeSales.forEach((sale) => {
        (sale.itens || []).forEach((item) => {
            addReportProduct(productMap, item.produtoId, item.produtoNome, getSaleItemRemaining(item), Number(item.precoUnitario || 0));
        });
    });
    paidOrders.forEach((order) => {
        (order.itens || []).forEach((item) => {
            addReportProduct(productMap, item.produtoId, item.produtoNome, item.quantidade, Number(item.precoUnitario || 0));
        });
    });

    const dailyMap = new Map();
    transactions.forEach((transaction) => {
        const dateKey = toReportDateKey(transaction.date);
        const current = dailyMap.get(dateKey) || {
            data: dateKey,
            vendasPdv: 0,
            pedidosOnline: 0,
            transacoes: 0,
            faturamento: 0,
            custo: 0,
            lucro: 0
        };
        current.transacoes += 1;
        current.faturamento += transaction.total;
        current.custo += transaction.custo;
        current.lucro = current.faturamento - current.custo;
        if (transaction.channel === "pdv") {
            current.vendasPdv += 1;
        } else {
            current.pedidosOnline += 1;
        }
        dailyMap.set(dateKey, current);
    });

    const faturamentoPdv = transactions
        .filter((transaction) => transaction.channel === "pdv")
        .reduce((sum, transaction) => sum + transaction.total, 0);
    const faturamentoOnline = transactions
        .filter((transaction) => transaction.channel === "online")
        .reduce((sum, transaction) => sum + transaction.total, 0);
    const faturamentoTotal = faturamentoPdv + faturamentoOnline;
    const custoVendidoTotal = transactions.reduce((sum, transaction) => sum + transaction.custo, 0);
    const descontosPdv = transactions
        .filter((transaction) => transaction.channel === "pdv")
        .reduce((sum, transaction) => sum + transaction.desconto, 0);
    const descontosOnline = transactions
        .filter((transaction) => transaction.channel === "online")
        .reduce((sum, transaction) => sum + transaction.desconto, 0);

    return {
        salesTable,
        orders,
        paymentItems: [...paymentMap.values()].sort((a, b) => b.total - a.total),
        topProducts: [...productMap.values()]
            .filter((item) => item.quantidade > 0)
            .sort((a, b) => b.quantidade - a.quantidade || b.total - a.total)
            .slice(0, 5),
        dailySales: [...dailyMap.values()]
            .sort((a, b) => b.data.localeCompare(a.data))
            .slice(0, 14),
        faturamentoPdv,
        faturamentoOnline,
        faturamentoTotal,
        transacoesTotal: transactions.length,
        descontosPdv,
        descontosOnline,
        descontosTotal: descontosPdv + descontosOnline,
        lucroEstimado: faturamentoTotal - custoVendidoTotal,
        margemLucroPercentual: faturamentoTotal > 0 ? ((faturamentoTotal - custoVendidoTotal) / faturamentoTotal) * 100 : 0,
        ticketMedio: transactions.length > 0 ? faturamentoTotal / transactions.length : 0
    };
}

function addReportProduct(map, productId, productName, quantity, unitPrice) {
    if (!quantity || quantity <= 0) {
        return;
    }

    const current = map.get(productId) || {
        produtoId: productId,
        produtoNome: productName,
        quantidade: 0,
        total: 0
    };
    current.quantidade += quantity;
    current.total += quantity * unitPrice;
    map.set(productId, current);
}

function getReportItemCost(item, quantity) {
    const product = state.products.find((candidate) => candidate.id === item.produtoId);
    const cost = Number(item.custoUnitario || product?.custo || 0);
    return cost * Number(quantity || 0);
}

function parseReportDate(value, endOfDay) {
    if (!value) {
        return null;
    }

    return new Date(`${value}T${endOfDay ? "23:59:59.999" : "00:00:00"}`);
}

function toReportDateKey(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "" : date.toISOString().slice(0, 10);
}

async function saveProduct(event) {
    event.preventDefault();

    try {
        const editingId = els.productId.value;
        const existingProduct = editingId ? state.products.find((item) => item.id === editingId) : null;
        const imagemUrl = existingProduct?.imagemUrl || null;
        const imagensExtras = existingProduct?.imagensExtras || [];

        const variacoesEstoque = collectProductVariantRows();
        els.productVariantStock.value = formatVariantStockList(variacoesEstoque);

        const payload = {
            nome: els.productName.value.trim(),
            categoriaId: els.productCategory.value,
            sku: emptyToNull(els.productSku.value),
            preco: Number(els.productPrice.value),
            custo: Number(els.productCost.value || 0),
            descricao: emptyToNull(els.productDescription.value),
            imagemUrl,
            imagensExtras,
            tamanhos: parseTextList(els.productSizes.value),
            cores: parseTextList(els.productColors.value),
            modelos: parseTextList(els.productModels.value),
            variacoesEstoque,
            guiaMedidas: emptyToNull(els.productSizeGuide.value)
        };

        if (editingId) {
            await api(`/produtos/${editingId}`, {
                method: "PUT",
                body: JSON.stringify({
                    ...payload,
                    ativo: els.productActive.checked,
                    quantidadeEmEstoque: Number(els.productInitialStock.value || 0)
                })
            });
            showToast("Produto atualizado.");
        } else {
            const created = await api("/produtos", {
                method: "POST",
                body: JSON.stringify({
                    ...payload,
                    quantidadeInicial: Number(els.productInitialStock.value)
                })
            });
            showToast("Produto cadastrado.");
            renderLabelPrintBox(created);
        }

        const returnRowId = editingId ? state.productReturnRowId : null;
        resetProductForm();
        await refreshScoped(["products"]);

        if (returnRowId) {
            document.querySelector(`[data-product-row="${returnRowId}"]`)?.scrollIntoView({ block: "center" });
        }
    } catch (error) {
        showToast(error.message);
    }
}

async function saveStorefrontProduct(event) {
    event.preventDefault();

    const productId = els.storefrontProductId.value || els.storefrontProductSelect.value;
    if (!productId) {
        showToast("Escolha um produto do estoque.");
        return;
    }

    try {
        let imagemLojaUrl = emptyToNull(els.storefrontImage.value);
        let imagensLojaExtras = parseImageList(els.storefrontExtraImages.value);

        if (els.storefrontImageFile.files.length > 0) {
            const upload = await uploadProductImage(els.storefrontImageFile.files[0]);
            imagemLojaUrl = upload.imagemUrl;
            els.storefrontImage.value = imagemLojaUrl;
        }

        if (els.storefrontExtraImageFiles.files.length > 0) {
            const uploads = await Promise.all(
                Array.from(els.storefrontExtraImageFiles.files).map((file) => uploadProductImage(file))
            );
            imagensLojaExtras = mergeImageUrls(imagensLojaExtras, uploads.map((upload) => upload.imagemUrl));
            els.storefrontExtraImages.value = imagensLojaExtras.join("\n");
        }

        imagensLojaExtras = mergeImageUrls(imagensLojaExtras).filter((image) => image !== imagemLojaUrl);

        await api(`/produtos/${productId}/vitrine`, {
            method: "PUT",
            body: JSON.stringify({
                publicadoNaLoja: els.storefrontPublished.checked,
                destaqueLoja: els.storefrontFeatured.checked,
                ordemLoja: Number(els.storefrontOrder.value || 0),
                nomeLoja: emptyToNull(els.storefrontName.value),
                descricaoLoja: emptyToNull(els.storefrontDescription.value),
                precoLoja: els.storefrontPrice.value ? Number(els.storefrontPrice.value) : null,
                imagemLojaUrl,
                imagensLojaExtras
            })
        });

        els.storefrontImageFile.value = "";
        els.storefrontExtraImageFiles.value = "";
        showToast("Vitrine do site atualizada.");
        await refreshScoped(["products"]);

        const updatedProduct = state.products.find((product) => product.id === productId);
        if (updatedProduct) {
            editStorefrontProduct(updatedProduct, false, false);
        }
    } catch (error) {
        showToast(error.message);
    }
}

async function saveSiteConfig(event) {
    event.preventDefault();
    await persistSiteConfig("Configurações do site salvas.");
}

async function saveSiteImagesConfig(event) {
    event.preventDefault();

    try {
        await uploadSiteImageFields();
        await persistSiteConfig("Imagens do site salvas.");
    } catch (error) {
        showToast(error.message);
    }
}

async function saveSiteContactConfig(event) {
    event.preventDefault();
    await persistSiteConfig("Contato do site salvo.");
}

async function persistSiteConfig(successMessage) {
    try {
        state.siteConfig = await api("/loja-configuracao", {
            method: "PUT",
            body: JSON.stringify(buildSiteConfigPayload())
        });

        renderSiteConfig();
        showToast(successMessage);
    } catch (error) {
        showToast(error.message);
    }
}

function buildSiteConfigPayload() {
    return {
        nomeCriadorSite: els.siteCreatorNameInput.value.trim(),
        politicaPrivacidade: els.privacyPolicyInput.value.trim(),
        freteValorPadrao: Number(els.shippingBasePrice.value || 0),
        freteGratisAcimaDe: Number(els.shippingFreeThreshold.value || 0),
        prazoMinimoDias: Number(els.shippingMinDays.value || 0),
        prazoMaximoDias: Number(els.shippingMaxDays.value || 0),
        mensagemFrete: emptyToNull(els.shippingMessageInput.value),
        mensagemLoginCliente: emptyToNull(els.customerLoginMessageInput.value),
        bannerEyebrow: emptyToNull(els.bannerEyebrowInput.value),
        bannerTitulo: emptyToNull(els.bannerTitleInput.value),
        bannerDescricao: emptyToNull(els.bannerDescriptionInput.value),
        bannerBotaoPrimario: emptyToNull(els.bannerPrimaryButtonInput.value),
        bannerBotaoSecundario: emptyToNull(els.bannerSecondaryButtonInput.value),
        bannerImagemUrl: emptyToNull(els.bannerImageInput.value),
        promocaoTopoTexto: emptyToNull(els.sitePromoTextInput.value),
        campanhaTitulo: emptyToNull(els.campaignTitleInput.value),
        campanhaDescricao: emptyToNull(els.campaignDescriptionInput.value),
        campanhaBotaoTexto: emptyToNull(els.campaignButtonInput.value),
        campanhaImagemUrl: emptyToNull(els.campaignImageInput.value),
        vitrineImagem1Url: emptyToNull(els.lookbookImage1Input.value),
        vitrineImagem1Titulo: emptyToNull(els.lookbookTitle1Input.value),
        vitrineImagem2Url: emptyToNull(els.lookbookImage2Input.value),
        vitrineImagem2Titulo: emptyToNull(els.lookbookTitle2Input.value),
        vitrineImagem3Url: emptyToNull(els.lookbookImage3Input.value),
        vitrineImagem3Titulo: emptyToNull(els.lookbookTitle3Input.value),
        whatsappLoja: emptyToNull(els.storeWhatsappInput.value),
        instagramLoja: emptyToNull(els.storeInstagramInput.value),
        enderecoLoja: emptyToNull(els.storeAddressInput.value),
        pixChave: emptyToNull(els.pixKeyInput.value),
        pixNomeRecebedor: emptyToNull(els.pixReceiverNameInput.value),
        pixCidade: emptyToNull(els.pixCityInput.value),
        pixOnlineAtivo: els.pixOnlineActiveInput.checked,
        cartaoOnlineAtivo: els.cardOnlineActiveInput.checked,
        checkoutCartaoNome: emptyToNull(els.cardCheckoutNameInput.value),
        checkoutCartaoUrl: emptyToNull(els.cardCheckoutUrlInput.value),
        mensagemPagamento: emptyToNull(els.paymentMessageInput.value),
        mensagemPagamentoCartao: emptyToNull(els.cardPaymentMessageInput.value),
        emailNotificacoesAtivo: els.emailNotificationsActiveInput.checked,
        emailProvedor: els.emailProviderInput.value,
        emailRemetente: emptyToNull(els.emailSenderInput.value),
        emailPedidosDestino: emptyToNull(els.emailOrdersInput.value),
        brevoApiKey: emptyToNull(els.brevoApiKeyInput.value),
        smtpHost: emptyToNull(els.smtpHostInput.value),
        smtpPorta: Number(els.smtpPortInput.value || 587),
        smtpUsuario: emptyToNull(els.smtpUserInput.value),
        smtpSenha: emptyToNull(els.smtpPasswordInput.value),
        smtpSsl: els.smtpSslInput.checked,
        backupAutomaticoAtivo: els.autoBackupActiveInput.checked,
        backupIntervaloHoras: Number(els.autoBackupIntervalInput.value || 24),
        gatewayPagamentoProvedor: emptyToNull(els.paymentGatewayProviderInput.value),
        gatewayPagamentoAtivo: els.paymentGatewayActiveInput.checked,
        gatewayPagamentoProducao: els.paymentGatewayProductionInput.checked,
        gatewayPagamentoPublicKey: emptyToNull(els.paymentGatewayPublicKeyInput.value),
        gatewayPagamentoAccessToken: emptyToNull(els.paymentGatewayAccessTokenInput.value),
        gatewayPagamentoWebhookSecret: emptyToNull(els.paymentGatewayWebhookSecretInput.value),
        gatewayPagamentoWebhookUrl: emptyToNull(els.paymentGatewayWebhookUrlInput.value),
        politicaTrocaDevolucao: emptyToNull(els.exchangePolicyInput.value),
        razaoSocial: emptyToNull(els.companyLegalNameInput.value),
        cnpj: emptyToNull(els.companyCnpjInput.value),
        siteUrlCanonica: emptyToNull(els.siteCanonicalUrlInput.value),
        googleAnalyticsId: emptyToNull(els.googleAnalyticsIdInput.value),
        metaPixelId: emptyToNull(els.metaPixelIdInput.value),
        backupEmailAtivo: els.backupEmailActiveInput.checked,
        backupEmailDestino: emptyToNull(els.backupEmailDestinationInput.value)
    };
}

async function testEmailConfig() {
    try {
        els.testEmailButton.disabled = true;
        els.emailNotificationsActiveInput.checked = true;
        els.testEmailButton.textContent = "Salvando e testando...";

        state.siteConfig = await api("/loja-configuracao", {
            method: "PUT",
            body: JSON.stringify(buildSiteConfigPayload())
        });
        renderSiteConfig();

        const response = await api("/loja-configuracao/testar-email", {
            method: "POST",
            body: JSON.stringify({
                destino: emptyToNull(els.emailOrdersInput.value)
            })
        });

        showToast(response.mensagem || "E-mail de teste enviado.");
    } catch (error) {
        showToast(error.message || "Não foi possível testar o e-mail.");
    } finally {
        els.testEmailButton.disabled = false;
        els.testEmailButton.textContent = "Testar e-mail";
    }
}

async function saveDeliveryOption(event) {
    event.preventDefault();

    const editingId = els.deliveryOptionId.value;
    const payload = {
        nome: els.deliveryName.value.trim(),
        tipo: els.deliveryType.value,
        descricao: emptyToNull(els.deliveryDescription.value),
        valor: Number(els.deliveryPrice.value || 0),
        freteGratisAcimaDe: Number(els.deliveryFreeAbove.value || 0),
        prazoMinimoDias: Number(els.deliveryMinDays.value || 0),
        prazoMaximoDias: Number(els.deliveryMaxDays.value || 0),
        cepInicial: emptyToNull(els.deliveryCepStart.value),
        cepFinal: emptyToNull(els.deliveryCepEnd.value),
        cidades: parseTextList(els.deliveryCities.value),
        bairros: parseTextList(els.deliveryDistricts.value),
        estados: parseTextList(els.deliveryStates.value),
        ativo: els.deliveryActive.checked,
        ordem: Number(els.deliveryOrder.value || 0)
    };

    try {
        if (editingId) {
            await api(`/opcoes-entrega/${editingId}`, {
                method: "PUT",
                body: JSON.stringify(payload)
            });
            showToast("Opção de entrega atualizada.");
        } else {
            await api("/opcoes-entrega", {
                method: "POST",
                body: JSON.stringify(payload)
            });
            showToast("Opção de entrega cadastrada.");
        }

        resetDeliveryOptionForm();
        await refreshScoped(["deliveryOptions"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function saveCoupon(event) {
    event.preventDefault();

    const editingId = els.couponId.value;
    const payload = {
        codigo: els.couponCode.value.trim(),
        descricao: emptyToNull(els.couponDescription.value),
        percentualDesconto: Number(els.couponPercent.value || 0),
        valorMinimoPedido: Number(els.couponMinimum.value || 0),
        ativo: els.couponActive.checked,
        validoAte: els.couponValidUntil.value ? `${els.couponValidUntil.value}T23:59:59` : null
    };

    try {
        if (editingId) {
            await api(`/cupons/${editingId}`, {
                method: "PUT",
                body: JSON.stringify(payload)
            });
            showToast("Cupom atualizado.");
        } else {
            await api("/cupons", {
                method: "POST",
                body: JSON.stringify(payload)
            });
            showToast("Cupom cadastrado.");
        }

        resetCouponForm();
        await refreshScoped(["coupons"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function savePanelUser(event) {
    event.preventDefault();

    const editingId = els.panelUserId.value;
    const payload = {
        usuario: els.panelUserLogin.value.trim(),
        nomeExibicao: els.panelUserName.value.trim(),
        perfil: els.panelUserRole.value,
        senha: emptyToNull(els.panelUserPassword.value),
        ativo: els.panelUserActive.checked
    };

    if (!editingId && !payload.senha) {
        showToast("Informe uma senha para o novo usuário.");
        return;
    }

    try {
        if (editingId) {
            await api(`/usuarios-painel/${editingId}`, {
                method: "PUT",
                body: JSON.stringify(payload)
            });
            showToast("Usuário atualizado.");
        } else {
            await api("/usuarios-painel", {
                method: "POST",
                body: JSON.stringify(payload)
            });
            showToast("Usuário cadastrado.");
        }

        resetPanelUserForm();
        await refreshScoped(["panelUsers"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function saveCategory(event) {
    event.preventDefault();

    try {
        await api("/categorias", {
            method: "POST",
            body: JSON.stringify({
                nome: els.categoryName.value.trim(),
                categoriaPaiId: emptyToNull(els.categoryParent.value)
            })
        });
        els.categoryName.value = "";
        els.categoryParent.value = "";
        showToast("Categoria adicionada.");
        await refreshScoped(["categories", "products"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function deleteProduct(product) {
    if (!window.confirm(`Excluir "${product.nome}"? Essa ação não pode ser desfeita.`)) {
        return;
    }

    try {
        await api(`/produtos/${product.id}`, { method: "DELETE" });
        showToast("Produto excluído.");
        await refreshScoped(["products"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function handleDeleteCategoryClick(event) {
    const button = event.target.closest("[data-remove-category]");
    if (!button) {
        return;
    }

    if (!window.confirm("Excluir esta categoria? Só é possível excluir categorias sem produtos e sem subcategorias.")) {
        return;
    }

    try {
        await api(`/categorias/${button.dataset.removeCategory}`, { method: "DELETE" });
        showToast("Categoria excluída.");
        await refreshScoped(["categories", "products"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function saveSupplier(event) {
    event.preventDefault();

    try {
        await api("/fornecedores", {
            method: "POST",
            body: JSON.stringify({
                nome: els.supplierName.value.trim(),
                documento: emptyToNull(els.supplierDocument.value),
                telefone: emptyToNull(els.supplierPhone.value),
                email: emptyToNull(els.supplierEmail.value)
            })
        });

        els.supplierForm.reset();
        showToast("Fornecedor cadastrado.");
        await refreshScoped(["suppliers"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function saveStockEntry(event) {
    event.preventDefault();

    if (!els.stockProduct.value) {
        showToast("Escolha um produto na lista antes de registrar a entrada.");
        return;
    }

    try {
        await api(`/produtos/${els.stockProduct.value}/estoque/entrada`, {
            method: "POST",
            body: JSON.stringify({
                quantidade: Number(els.stockQuantity.value),
                observacao: emptyToNull(els.stockNote.value),
                fornecedorId: els.stockSupplier.value || null,
                custoUnitario: els.stockCost.value ? Number(els.stockCost.value) : null,
                documento: emptyToNull(els.stockDocument.value),
                tamanho: emptyToNull(els.stockSize.value),
                cor: emptyToNull(els.stockColor.value),
                modelo: emptyToNull(els.stockModel.value)
            })
        });
        els.stockQuantity.value = "";
        els.stockSize.value = "";
        els.stockColor.value = "";
        els.stockModel.value = "";
        els.stockCost.value = "";
        els.stockDocument.value = "";
        els.stockNote.value = "";
        showToast("Entrada de estoque registrada.");
        await refreshScoped(["products", "movements"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function finishSale() {
    if (state.cart.length === 0) {
        return;
    }

    const formaPagamento = document.querySelector("input[name='payment']:checked").value;
    const subtotal = cartTotal();
    const desconto = getSaleDiscount();
    const total = Math.max(0, subtotal - desconto);
    const valorRecebido = Number(els.saleReceived.value || 0);

    if (desconto > subtotal) {
        showToast("O desconto não pode ser maior que o total.");
        return;
    }

    if (formaPagamento === "Dinheiro" && valorRecebido > 0 && valorRecebido < total) {
        showToast("Valor recebido menor que o total.");
        return;
    }

    try {
        const sale = await api("/pdv/vendas", {
            method: "POST",
            body: JSON.stringify({
                formaPagamento,
                desconto,
                valorRecebido: valorRecebido || total,
                observacao: emptyToNull(els.saleNote.value),
                itens: state.cart.map((item) => ({
                    produtoId: item.produtoId,
                    quantidade: item.quantidade,
                    tamanho: item.tamanho || null,
                    cor: item.cor || null,
                    modelo: item.modelo || null
                }))
            })
        });

        state.cart = [];
        state.lastReceipt = sale;
        els.saleDiscount.value = "";
        els.saleReceived.value = "";
        els.saleNote.value = "";
        showToast("Venda finalizada e estoque atualizado.");
        await refreshScoped(["products", "movements", "sales", "summary"]);
        renderReceipt(sale);
    } catch (error) {
        showToast(error.message);
    }
}

function openReturnDialog(saleId, mode = "return") {
    const sale = state.sales.find((item) => item.id === saleId);
    if (!sale || sale.devolvida) {
        return;
    }

    const remainingItems = sale.itens.filter((item) => getSaleItemRemaining(item) > 0);
    if (!remainingItems.length) {
        showToast("Essa venda não possui itens disponíveis para devolução.");
        return;
    }

    state.returningSaleId = saleId;
    state.returnMode = mode;
    els.returnReason.value = sale.motivoDevolucao || "";
    document.querySelector("#returnTitle").textContent = mode === "exchange" ? "Troca PDV" : "Devolução PDV";
    els.returnSaleSummary.textContent = mode === "exchange"
        ? `Venda ${sale.id.slice(0, 8).toUpperCase()} · escolha o que voltou e confirme as peças novas do carrinho`
        : `Venda ${sale.id.slice(0, 8).toUpperCase()} · ${currency.format(sale.totalOriginal ?? sale.total)}`;
    els.returnItems.innerHTML = remainingItems.map((item) => {
        const remaining = getSaleItemRemaining(item);
        const variation = formatPdvVariation(item);
        return `
            <label class="return-item">
                <span>
                    <strong>${escapeHtml(item.produtoNome)}</strong>
                    ${variation ? `<small>${escapeHtml(variation)}</small>` : ""}
                    <small>${item.quantidade} vendido${item.quantidade === 1 ? "" : "s"} · ${item.quantidadeDevolvida || 0} devolvido${item.quantidadeDevolvida === 1 ? "" : "s"}</small>
                </span>
                <input type="number" min="0" max="${remaining}" step="1" value="${remaining}" data-return-item="${escapeHtml(getPdvCartItemKey(item.produtoId, item))}">
            </label>
        `;
    }).join("");
    renderExchangeSummary();
    els.returnModal.classList.remove("hidden");
}

function closeReturnDialog() {
    state.returningSaleId = null;
    state.returnMode = "return";
    els.returnForm.reset();
    els.returnItems.innerHTML = "";
    els.exchangeSummary.innerHTML = "";
    els.exchangeSummary.classList.add("hidden");
    els.returnModal.classList.add("hidden");
}

async function submitReturnSale(event) {
    event.preventDefault();

    const sale = state.sales.find((item) => item.id === state.returningSaleId);
    if (!sale) {
        closeReturnDialog();
        return;
    }

    const itens = Array.from(els.returnItems.querySelectorAll("[data-return-item]"))
        .map((input) => {
            const saleItem = sale.itens.find((item) => getPdvCartItemKey(item.produtoId, item) === input.dataset.returnItem);
            return saleItem
                ? {
                    produtoId: saleItem.produtoId,
                    quantidade: Number(input.value || 0),
                    tamanho: saleItem.tamanho || null,
                    cor: saleItem.cor || null,
                    modelo: saleItem.modelo || null
                }
                : null;
        })
        .filter(Boolean)
        .filter((item) => item.quantidade > 0);

    if (!itens.length) {
        showToast("Informe pelo menos uma quantidade para devolver.");
        return;
    }

    try {
        if (state.returnMode === "exchange") {
            if (state.cart.length === 0) {
                showToast("Adicione as peças novas no carrinho do PDV antes de registrar a troca.");
                return;
            }

            const response = await api(`/pdv/vendas/${sale.id}/troca`, {
                method: "POST",
                body: JSON.stringify({
                    motivo: emptyToNull(els.returnReason.value),
                    itensDevolvidos: itens,
                    itensNovos: state.cart.map((item) => ({
                        produtoId: item.produtoId,
                        quantidade: item.quantidade,
                        tamanho: item.tamanho || null,
                        cor: item.cor || null,
                        modelo: item.modelo || null
                    })),
                    formaPagamento: "Troca",
                    valorRecebido: 0,
                    observacao: "Peças novas registradas pelo carrinho do PDV"
                })
            });

            state.cart = [];
            showToast("Troca registrada e estoque atualizado.");
            closeReturnDialog();
            await refreshScoped(["products", "movements", "sales", "summary"]);
            renderReceipt(response.vendaTroca);
            showView("pdv");
            return;
        }

        await api(`/pdv/vendas/${sale.id}/devolucao`, {
            method: "POST",
            body: JSON.stringify({
                motivo: emptyToNull(els.returnReason.value),
                itens
            })
        });

        showToast("Venda devolvida e estoque atualizado.");
        closeReturnDialog();
        await refreshScoped(["products", "movements", "sales", "summary"]);
    } catch (error) {
        showToast(error.message || "Não foi possível devolver a venda.");
    }
}

function renderExchangeSummary() {
    if (state.returnMode !== "exchange") {
        els.exchangeSummary.classList.add("hidden");
        els.exchangeSummary.innerHTML = "";
        return;
    }

    const total = cartTotal();
    els.exchangeSummary.classList.remove("hidden");
    els.exchangeSummary.innerHTML = `
        <span>Peças novas da troca</span>
        ${state.cart.length
            ? `<ul>${state.cart.map((item) => {
                const product = state.products.find((candidate) => candidate.id === item.produtoId);
                const variation = formatPdvVariation(item);
                return product ? `<li>${item.quantidade}x ${escapeHtml(product.nome)}${variation ? ` (${escapeHtml(variation)})` : ""} <strong>${currency.format(product.preco * item.quantidade)}</strong></li>` : "";
            }).join("")}</ul>
            <div><span>Total das peças novas</span><strong>${currency.format(total)}</strong></div>`
            : `<p>Adicione as peças novas no carrinho do PDV antes de confirmar a troca.</p>`}
    `;
}

function renderReceipt(sale) {
    if (!sale) {
        els.receiptBox.classList.add("hidden");
        els.receiptBox.innerHTML = "";
        return;
    }

    const shortId = sale.id.slice(0, 8).toUpperCase();
    els.receiptBox.classList.remove("hidden");
    els.receiptBox.innerHTML = `
        <div class="receipt-head">
            <div>
                <strong>Nana Modas</strong>
                <span>Comprovante ${shortId}</span>
            </div>
            <span>${formatDate(sale.criadaEm)}</span>
        </div>
        <div class="receipt-meta">
            <span>${formatPayment(sale.formaPagamento)}</span>
            ${sale.devolvida ? '<span class="badge badge-muted">Venda devolvida</span>' : '<span class="badge badge-ok">Venda concluída</span>'}
        </div>
        <div class="receipt-lines">
            ${sale.itens.map((item) => {
                const variation = formatPdvVariation(item);
                return `
                    <div>
                        <span>
                            ${item.quantidade}x ${escapeHtml(item.produtoNome)}
                            ${variation ? `<small>${escapeHtml(variation)}</small>` : ""}
                            <small>${currency.format(item.precoUnitario)} cada${item.quantidadeDevolvida ? ` · ${item.quantidadeDevolvida} devolvido${item.quantidadeDevolvida === 1 ? "" : "s"}` : ""}</small>
                        </span>
                        <strong>${currency.format(item.subtotal)}</strong>
                    </div>
                `;
            }).join("")}
        </div>
        <div class="receipt-total">
            <span>Subtotal</span>
            <strong>${currency.format(sale.totalBruto ?? sale.total)}</strong>
        </div>
        <div class="receipt-total">
            <span>Desconto</span>
            <strong>${currency.format(sale.desconto || 0)}</strong>
        </div>
        <div class="receipt-total is-final">
            <span>Total</span>
            <strong>${currency.format(sale.totalOriginal ?? sale.total)}</strong>
        </div>
        ${sale.valorDevolvido ? `
            <div class="receipt-total">
                <span>Devolvido</span>
                <strong>${currency.format(sale.valorDevolvido)}</strong>
            </div>
            <div class="receipt-total is-final">
                <span>Total líquido</span>
                <strong>${currency.format(sale.total)}</strong>
            </div>
        ` : ""}
        <div class="receipt-total">
            <span>Recebido</span>
            <strong>${currency.format(sale.valorRecebido || sale.total)}</strong>
        </div>
        <div class="receipt-total">
            <span>Troco</span>
            <strong>${currency.format(sale.troco || 0)}</strong>
        </div>
        <p>Obrigado pela preferência.</p>
        ${sale.observacao ? `<p>${escapeHtml(sale.observacao)}</p>` : ""}
        ${sale.devolvida && sale.motivoDevolucao ? `<p>Devolução: ${escapeHtml(sale.motivoDevolucao)}</p>` : ""}
        <div class="receipt-actions">
            <button class="button button-secondary" type="button" data-receipt-action="copy">Copiar</button>
            <button class="button button-secondary" type="button" data-receipt-action="print">Imprimir</button>
        </div>
    `;
}

async function copySaleReceipt(sale) {
    try {
        await navigator.clipboard.writeText(buildSaleReceiptText(sale));
        showToast("Comprovante copiado.");
    } catch {
        showToast("Não foi possível copiar automaticamente.");
    }
}

function printReceipt(target = els.receiptBox) {
    target?.classList.add("is-printing");
    document.body.classList.add("printing-receipt");
    window.print();
    window.setTimeout(() => {
        document.body.classList.remove("printing-receipt");
        target?.classList.remove("is-printing");
    }, 300);
}

function buildProductLabels(product) {
    if (!product) {
        return [];
    }

    if (product.variacoesEstoque?.length) {
        return product.variacoesEstoque.flatMap((variation) => {
            const quantidade = Math.max(0, Math.floor(Number(variation.quantidade || 0)));
            const sku = variation.sku || product.sku || "";
            return Array.from({ length: quantidade }, () => ({
                nome: product.nome,
                preco: product.preco,
                tamanho: variation.tamanho || "",
                cor: variation.cor || "",
                sku
            }));
        });
    }

    const quantidade = Math.max(0, Math.floor(Number(product.quantidadeEmEstoque || 0)));
    return Array.from({ length: quantidade }, () => ({
        nome: product.nome,
        preco: product.preco,
        tamanho: "",
        cor: "",
        sku: product.sku || ""
    }));
}

function renderLabelPrintBox(product) {
    const labels = buildProductLabels(product);

    if (!labels.length) {
        els.labelPrintBox.classList.add("hidden");
        els.labelPrintBox.innerHTML = "";
        return false;
    }

    els.labelPrintBox.classList.remove("hidden");
    els.labelPrintBox.innerHTML = `
        <div class="label-print-head">
            <strong>${labels.length} etiqueta${labels.length === 1 ? "" : "s"} — ${escapeHtml(product.nome)}</strong>
            <span class="panel-note">1 etiqueta por unidade em estoque, 40x40mm</span>
        </div>
        <div class="label-grid">
            ${labels.map((label) => `
                <div class="label-item">
                    <strong class="label-item-name">${escapeHtml(label.nome)}</strong>
                    ${(label.tamanho || label.cor) ? `<span class="label-item-variation">${escapeHtml([label.tamanho, label.cor].filter(Boolean).join(" · "))}</span>` : ""}
                    <span class="label-item-price">${currency.format(label.preco)}</span>
                    ${label.sku ? `
                        ${window.buildBarcodeSvg?.(label.sku, { width: 132, height: 34 }) || ""}
                        <span class="label-item-sku">${escapeHtml(label.sku)}</span>
                    ` : ""}
                </div>
            `).join("")}
        </div>
        <div class="label-print-actions">
            <button class="button button-secondary" type="button" data-label-action="close">Fechar</button>
            <button class="button button-primary" type="button" data-label-action="print">Imprimir etiquetas</button>
        </div>
    `;

    els.labelPrintBox.scrollIntoView({ behavior: "smooth", block: "nearest" });
    return true;
}

function printLabels() {
    els.labelPrintBox.classList.add("is-printing");
    document.body.classList.add("printing-labels");
    window.print();
    window.setTimeout(() => {
        document.body.classList.remove("printing-labels");
        els.labelPrintBox.classList.remove("is-printing");
    }, 300);
}

function buildSaleReceiptText(sale) {
    const shortId = sale.id.slice(0, 8).toUpperCase();
    const items = sale.itens
        .map((item) => {
            const variation = formatPdvVariation(item);
            return `- ${item.quantidade}x ${item.produtoNome}${variation ? ` (${variation})` : ""} (${currency.format(item.precoUnitario)})${item.quantidadeDevolvida ? ` · ${item.quantidadeDevolvida} devolvido${item.quantidadeDevolvida === 1 ? "" : "s"}` : ""}: ${currency.format(item.subtotal)}`;
        })
        .join("\n");

    return [
        "Nana Modas",
        `Comprovante ${shortId}`,
        `Data: ${formatDate(sale.criadaEm)}`,
        `Pagamento: ${formatPayment(sale.formaPagamento)}`,
        sale.devolvida ? "Status: Venda devolvida" : "Status: Venda concluída",
        "",
        "Itens:",
        items,
        "",
        `Subtotal: ${currency.format(sale.totalBruto ?? sale.total)}`,
        `Desconto: ${currency.format(sale.desconto || 0)}`,
        `Total: ${currency.format(sale.totalOriginal ?? sale.total)}`,
        sale.valorDevolvido ? `Devolvido: ${currency.format(sale.valorDevolvido)}` : null,
        sale.valorDevolvido ? `Total líquido: ${currency.format(sale.total)}` : null,
        `Recebido: ${currency.format(sale.valorRecebido || sale.total)}`,
        `Troco: ${currency.format(sale.troco || 0)}`,
        sale.observacao ? `Observação: ${sale.observacao}` : null,
        sale.devolvida && sale.motivoDevolucao ? `Devolução: ${sale.motivoDevolucao}` : null,
        "",
        "Obrigado pela preferência."
    ].filter((line) => line !== null).join("\n");
}

function exportReport(type) {
    if (type === "backup") {
        window.location.href = "/backup/banco";
        showToast("Backup do banco iniciado.");
        return;
    }

    const exporters = {
        products: exportProductsCsv,
        sales: exportSalesCsv,
        orders: exportOrdersCsv
    };

    const exporter = exporters[type];
    if (exporter) {
        exporter();
    }
}

function exportProductsCsv() {
    const rows = [
        ["Produto", "SKU", "Categoria", "Preco", "Custo", "Estoque", "Ativo", "Publicado no site"],
        ...state.products.map((product) => [
            product.nome,
            product.sku || "",
            product.categoria,
            product.preco,
            product.custo || 0,
            product.quantidadeEmEstoque,
            product.ativo ? "Sim" : "Nao",
            product.publicadoNaLoja ? "Sim" : "Nao"
        ])
    ];

    downloadCsv("nana-modas-produtos.csv", rows);
}

function exportSalesCsv() {
    const rows = [
        ["Data", "Venda", "Pagamento", "Itens", "Subtotal", "Desconto", "Total original", "Devolvido", "Total liquido", "Recebido", "Troco", "Status", "Observacao"],
        ...state.sales.map((sale) => [
            formatDate(sale.criadaEm),
            sale.id,
            formatPayment(sale.formaPagamento),
            sale.itens.map((item) => {
                const variation = formatPdvVariation(item);
                return `${item.quantidade}x ${item.produtoNome}${variation ? ` (${variation})` : ""}${item.quantidadeDevolvida ? ` (${item.quantidadeDevolvida} devolvido)` : ""}`;
            }).join(" | "),
            sale.totalBruto ?? sale.total,
            sale.desconto || 0,
            sale.totalOriginal ?? sale.total,
            sale.valorDevolvido || 0,
            sale.total,
            sale.valorRecebido || sale.total,
            sale.troco || 0,
            sale.devolvida ? "Devolvida" : sale.devolucaoParcial ? "Parcial" : "Concluida",
            sale.observacao || ""
        ])
    ];

    downloadCsv("nana-modas-vendas-pdv.csv", rows);
}

function exportOrdersCsv() {
    const rows = [
        ["Data", "Pedido", "Cliente", "Email", "Telefone", "Status", "Pagamento", "Itens", "Cupom", "Desconto", "Entrega", "Frete", "Total", "Endereco", "Rastreio"],
        ...state.orders.map((order) => [
            formatDate(order.criadoEm),
            order.id,
            order.nomeCliente,
            order.emailCliente,
            order.telefoneCliente || "",
            formatOrderStatus(order.status),
            formatPayment(order.formaPagamento),
            (order.itens || []).map((item) => `${item.quantidade}x ${item.produtoNome}${formatOrderVariation(item) ? ` (${formatOrderVariation(item)})` : ""}`).join(" | "),
            order.cupomCodigo || "",
            order.desconto || 0,
            order.entregaNome || "",
            order.entregaValor || 0,
            order.total,
            order.enderecoEntrega,
            order.codigoRastreio || ""
        ])
    ];

    downloadCsv("nana-modas-pedidos-online.csv", rows);
}

function downloadCsv(filename, rows) {
    const csv = rows.map((row) => row.map(formatCsvCell).join(";")).join("\n");
    const blob = new Blob([`\uFEFF${csv}`], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    showToast("Arquivo CSV gerado.");
}

function formatCsvCell(value) {
    const text = String(value ?? "").replace(/\r?\n/g, " ");
    return `"${text.replace(/"/g, '""')}"`;
}

function editProduct(product) {
    state.productReturnRowId = product.id;
    showView("products");
    els.productId.value = product.id;
    els.productName.value = product.nome;
    els.productCategory.value = product.categoriaId;
    els.productSku.value = product.sku || "";
    els.productPrice.value = product.preco;
    els.productCost.value = product.custo || 0;
    const hasVariations = (product.variacoesEstoque || []).length > 0;
    els.productInitialStock.value = product.quantidadeEmEstoque;
    els.productInitialStock.disabled = hasVariations;
    els.productInitialStockLabel.textContent = "Estoque atual";
    els.productInitialStockHint.classList.toggle("hidden", !hasVariations);
    els.productDescription.value = product.descricao || "";
    els.productSizes.value = (product.tamanhos || []).join("\n");
    els.productColors.value = (product.cores || []).join("\n");
    els.productModels.value = (product.modelos || []).join("\n");
    renderProductVariantRows(product.variacoesEstoque || []);
    els.productSizeGuide.value = product.guiaMedidas || "";
    els.productActive.checked = product.ativo;
    els.cancelEditButton.classList.remove("hidden");
    els.productName.focus();
}

function editStorefrontProduct(product, focusForm = true, switchView = true) {
    if (switchView) {
        showView("storefront");
    }
    els.storefrontProductId.value = product.id;
    els.storefrontProductSelect.value = product.id;
    els.storefrontPublished.checked = product.publicadoNaLoja;
    els.storefrontFeatured.checked = product.destaqueLoja;
    els.storefrontName.value = product.nomeLoja || "";
    els.storefrontDescription.value = product.descricaoLoja || "";
    els.storefrontPrice.value = product.precoLoja ?? "";
    els.storefrontOrder.value = product.ordemLoja || 0;
    els.storefrontImage.value = product.imagemLojaUrl || "";
    els.storefrontImageFile.value = "";
    setStorefrontImagePreview(getStorefrontImage(product));
    els.storefrontExtraImages.value = (product.imagensLojaExtras || []).join("\n");
    els.storefrontExtraImageFiles.value = "";
    setStorefrontExtraImagePreview(getStorefrontImages(product));

    if (focusForm) {
        els.storefrontName.focus();
    }
}

function editCoupon(coupon) {
    showView("storefront");
    els.couponId.value = coupon.id;
    els.couponCode.value = coupon.codigo;
    els.couponDescription.value = coupon.descricao || "";
    els.couponPercent.value = coupon.percentualDesconto;
    els.couponMinimum.value = coupon.valorMinimoPedido;
    els.couponValidUntil.value = toDateInputValue(coupon.validoAte);
    els.couponActive.checked = coupon.ativo;
    els.cancelCouponEditButton.classList.remove("hidden");
    els.couponCode.focus();
}

function editDeliveryOption(option) {
    showView("storefront");
    els.deliveryOptionId.value = option.id;
    els.deliveryName.value = option.nome;
    els.deliveryType.value = option.tipo;
    els.deliveryDescription.value = option.descricao || "";
    els.deliveryPrice.value = option.valor;
    els.deliveryFreeAbove.value = option.freteGratisAcimaDe;
    els.deliveryMinDays.value = option.prazoMinimoDias;
    els.deliveryMaxDays.value = option.prazoMaximoDias;
    els.deliveryCepStart.value = formatCep(option.cepInicial);
    els.deliveryCepEnd.value = formatCep(option.cepFinal);
    els.deliveryCities.value = (option.cidades || []).join("\n");
    els.deliveryDistricts.value = (option.bairros || []).join("\n");
    els.deliveryStates.value = (option.estados || []).join("\n");
    els.deliveryOrder.value = option.ordem || 0;
    els.deliveryActive.checked = option.ativo;
    els.cancelDeliveryEditButton.classList.remove("hidden");
    els.deliveryName.focus();
}

function editPanelUser(user) {
    showView("users");
    els.panelUserId.value = user.id;
    els.panelUserLogin.value = user.usuario;
    els.panelUserName.value = user.nomeExibicao;
    els.panelUserRole.value = user.perfil;
    els.panelUserPassword.value = "";
    els.panelUserPassword.placeholder = "Deixe em branco para manter";
    els.panelUserActive.checked = user.ativo;
    els.userFormMode.textContent = "Editando acesso";
    els.cancelUserEditButton.classList.remove("hidden");
    els.panelUserName.focus();
}

function resetDeliveryOptionForm() {
    els.deliveryOptionForm.reset();
    els.deliveryOptionId.value = "";
    els.deliveryName.value = "";
    els.deliveryType.value = "Retirada";
    els.deliveryDescription.value = "";
    els.deliveryPrice.value = "0";
    els.deliveryFreeAbove.value = "0";
    els.deliveryMinDays.value = "0";
    els.deliveryMaxDays.value = "1";
    els.deliveryCepStart.value = "";
    els.deliveryCepEnd.value = "";
    els.deliveryCities.value = "";
    els.deliveryDistricts.value = "";
    els.deliveryStates.value = "";
    els.deliveryOrder.value = "0";
    els.deliveryActive.checked = true;
    els.cancelDeliveryEditButton.classList.add("hidden");
}

function resetCouponForm() {
    els.couponForm.reset();
    els.couponId.value = "";
    els.couponCode.value = "";
    els.couponDescription.value = "";
    els.couponPercent.value = "";
    els.couponMinimum.value = "";
    els.couponValidUntil.value = "";
    els.couponActive.checked = true;
    els.cancelCouponEditButton.classList.add("hidden");
}

function resetPanelUserForm() {
    els.panelUserForm.reset();
    els.panelUserId.value = "";
    els.panelUserLogin.value = "";
    els.panelUserName.value = "";
    els.panelUserRole.value = "Caixa";
    els.panelUserPassword.value = "";
    els.panelUserPassword.placeholder = "Mínimo 6 caracteres";
    els.panelUserActive.checked = true;
    els.userFormMode.textContent = "Novo acesso";
    els.cancelUserEditButton.classList.add("hidden");
}

function resetProductForm() {
    els.productForm.reset();
    els.productId.value = "";
    state.productReturnRowId = null;
    els.productInitialStock.disabled = false;
    els.productInitialStock.value = "0";
    els.productInitialStockLabel.textContent = "Estoque inicial";
    els.productInitialStockHint.classList.add("hidden");
    els.productSku.value = "";
    els.productCost.value = "";
    els.productSizes.value = "";
    els.productColors.value = "";
    els.productModels.value = "";
    renderProductVariantRows([]);
    els.productSizeGuide.value = "";
    els.productActive.checked = true;
    els.cancelEditButton.classList.add("hidden");
    if (state.categories[0]) {
        els.productCategory.value = state.categories[0].id;
    }
}

function previewSelectedStorefrontImage() {
    const file = els.storefrontImageFile.files[0];
    if (!file) {
        setStorefrontImagePreview(els.storefrontImage.value);
        return;
    }

    if (!isAcceptedImageFile(file)) {
        els.storefrontImageFile.value = "";
        showToast("Escolha uma imagem JPG, PNG ou WebP de até 5 MB.");
        return;
    }

    const objectUrl = URL.createObjectURL(file);
    setStorefrontImagePreview(objectUrl, true);
}

function setStorefrontImagePreview(source, isObjectUrl = false) {
    if (storefrontImageObjectUrl && storefrontImageObjectUrl !== source) {
        URL.revokeObjectURL(storefrontImageObjectUrl);
        storefrontImageObjectUrl = null;
    }

    if (isObjectUrl) {
        storefrontImageObjectUrl = source;
    }

    if (!source) {
        els.storefrontImagePreview.innerHTML = "<span>Nenhuma imagem do site selecionada</span>";
        return;
    }

    els.storefrontImagePreview.innerHTML = `<img src="${escapeHtml(source)}" alt="Prévia da imagem do site">`;
}

function previewSelectedStorefrontExtraImages() {
    const files = Array.from(els.storefrontExtraImageFiles.files);
    if (!files.length) {
        setStorefrontExtraImagePreview(parseImageList(els.storefrontExtraImages.value));
        return;
    }

    if (files.some((file) => !isAcceptedImageFile(file))) {
        els.storefrontExtraImageFiles.value = "";
        showToast("Escolha imagens JPG, PNG ou WebP de até 5 MB.");
        return;
    }

    const objectUrls = files.map((file) => URL.createObjectURL(file));
    setStorefrontExtraImagePreview(mergeImageUrls(parseImageList(els.storefrontExtraImages.value), objectUrls), objectUrls);
}

function setStorefrontExtraImagePreview(sources, objectUrls = []) {
    storefrontExtraImageObjectUrls.forEach((source) => URL.revokeObjectURL(source));
    storefrontExtraImageObjectUrls = objectUrls;

    const images = mergeImageUrls(sources);
    if (!images.length) {
        els.storefrontExtraImagePreview.innerHTML = "<span>Nenhuma foto extra do site selecionada</span>";
        return;
    }

    els.storefrontExtraImagePreview.innerHTML = images
        .map((source) => `<img src="${escapeHtml(source)}" alt="Prévia de foto extra do site">`)
        .join("");
}

function previewSelectedStorefrontImage() {
    const file = els.storefrontImageFile.files[0];
    if (!file) {
        setStorefrontImagePreview(els.storefrontImage.value);
        return;
    }

    if (!isAcceptedImageFile(file)) {
        els.storefrontImageFile.value = "";
        showToast("Escolha uma imagem JPG, PNG ou WebP de até 5 MB.");
        return;
    }

    const objectUrl = URL.createObjectURL(file);
    setStorefrontImagePreview(objectUrl, true);
}

function setStorefrontImagePreview(source, isObjectUrl = false) {
    if (storefrontImageObjectUrl && storefrontImageObjectUrl !== source) {
        URL.revokeObjectURL(storefrontImageObjectUrl);
        storefrontImageObjectUrl = null;
    }

    if (isObjectUrl) {
        storefrontImageObjectUrl = source;
    }

    if (!source) {
        els.storefrontImagePreview.innerHTML = "<span>Nenhuma imagem do site selecionada</span>";
        return;
    }

    els.storefrontImagePreview.innerHTML = `<img src="${escapeHtml(source)}" alt="Prévia da imagem do site">`;
}

function previewSelectedStorefrontExtraImages() {
    const files = Array.from(els.storefrontExtraImageFiles.files);
    if (!files.length) {
        setStorefrontExtraImagePreview(parseImageList(els.storefrontExtraImages.value));
        return;
    }

    if (files.some((file) => !isAcceptedImageFile(file))) {
        els.storefrontExtraImageFiles.value = "";
        showToast("Escolha imagens JPG, PNG ou WebP de até 5 MB.");
        return;
    }

    const objectUrls = files.map((file) => URL.createObjectURL(file));
    setStorefrontExtraImagePreview(mergeImageUrls(parseImageList(els.storefrontExtraImages.value), objectUrls), objectUrls);
}

function setStorefrontExtraImagePreview(sources, objectUrls = []) {
    storefrontExtraImageObjectUrls.forEach((source) => URL.revokeObjectURL(source));
    storefrontExtraImageObjectUrls = objectUrls;

    const images = mergeImageUrls(sources);
    if (!images.length) {
        els.storefrontExtraImagePreview.innerHTML = "<span>Nenhuma foto extra do site selecionada</span>";
        return;
    }

    els.storefrontExtraImagePreview.innerHTML = images
        .map((source) => `<img src="${escapeHtml(source)}" alt="Prévia de foto extra do site">`)
        .join("");
}

function bindSiteImagePreview(input, fileInput, preview, emptyMessage) {
    input.addEventListener("input", () => {
        if (!fileInput.files.length) {
            setSiteImagePreview(preview, input.value, emptyMessage);
            renderSiteImageAdminPreview();
        }
    });

    fileInput.addEventListener("change", () => {
        const file = fileInput.files[0];
        if (!file) {
            setSiteImagePreview(preview, input.value, emptyMessage);
            renderSiteImageAdminPreview();
            return;
        }

        if (!isAcceptedImageFile(file)) {
            fileInput.value = "";
            showToast("Escolha uma imagem JPG, PNG ou WebP de até 5 MB.");
            return;
        }

        const objectUrl = URL.createObjectURL(file);
        siteImageObjectUrls.push(objectUrl);
        setSiteImagePreview(preview, objectUrl, emptyMessage);
        renderSiteImageAdminPreview();
    });
}

function renderSiteImagePreviews() {
    setSiteImagePreview(els.bannerImagePreview, els.bannerImageInput.value, "Nenhum banner principal selecionado");
    setSiteImagePreview(els.campaignImagePreview, els.campaignImageInput.value, "Nenhuma campanha selecionada");
    setSiteImagePreview(els.lookbookImage1Preview, els.lookbookImage1Input.value, "Nenhuma imagem");
    setSiteImagePreview(els.lookbookImage2Preview, els.lookbookImage2Input.value, "Nenhuma imagem");
    setSiteImagePreview(els.lookbookImage3Preview, els.lookbookImage3Input.value, "Nenhuma imagem");
    renderSiteImageAdminPreview();
}

function setSiteImagePreview(preview, source, emptyMessage) {
    const normalized = String(source || "").trim();
    preview.innerHTML = normalized
        ? `<img src="${escapeHtml(normalized)}" alt="Prévia de imagem do site">`
        : `<span>${escapeHtml(emptyMessage)}</span>`;
}

async function uploadSiteImageFields() {
    const fields = [
        { input: els.bannerImageInput, file: els.bannerImageFile },
        { input: els.campaignImageInput, file: els.campaignImageFile },
        { input: els.lookbookImage1Input, file: els.lookbookImage1File },
        { input: els.lookbookImage2Input, file: els.lookbookImage2File },
        { input: els.lookbookImage3Input, file: els.lookbookImage3File }
    ];

    for (const field of fields) {
        if (!field.file.files.length) {
            continue;
        }

        const upload = await uploadProductImage(field.file.files[0]);
        field.input.value = upload.imagemUrl;
        field.file.value = "";
    }

    renderSiteImagePreviews();
}

function renderSiteImageAdminPreview() {
    const items = [
        { label: "Banner", title: els.bannerTitleInput.value || "Nana Modas", image: els.bannerImageInput.value },
        { label: "Campanha", title: els.campaignTitleInput.value || "Campanha", image: els.campaignImageInput.value },
        { label: "Vitrine", title: els.lookbookTitle1Input.value || "Vitrine 1", image: els.lookbookImage1Input.value },
        { label: "Vitrine", title: els.lookbookTitle2Input.value || "Vitrine 2", image: els.lookbookImage2Input.value },
        { label: "Vitrine", title: els.lookbookTitle3Input.value || "Vitrine 3", image: els.lookbookImage3Input.value }
    ];

    els.siteImageAdminPreview.innerHTML = items.map((item) => {
        const image = String(item.image || "").trim();
        return `
            <article class="site-image-preview-card">
                <div class="site-image-preview-media">
                    ${image ? `<img src="${escapeHtml(image)}" alt="${escapeHtml(item.title)}">` : "<span>NM</span>"}
                </div>
                <div>
                    <span>${escapeHtml(item.label)}</span>
                    <strong>${escapeHtml(item.title)}</strong>
                </div>
            </article>
        `;
    }).join("");
}

function renderSiteContactAdminPreview() {
    const whatsapp = els.storeWhatsappInput.value.trim();
    const instagram = els.storeInstagramInput.value.trim();
    const address = els.storeAddressInput.value.trim();
    const items = [
        { label: "WhatsApp", value: whatsapp || "Não informado" },
        { label: "Instagram", value: instagram || "Não informado" },
        { label: "Endereço", value: address || "Não informado" }
    ];

    els.siteContactAdminPreview.innerHTML = items.map((item) => `
        <div class="list-item">
            <div>
                <strong>${escapeHtml(item.label)}</strong>
                <span>${escapeHtml(item.value)}</span>
            </div>
        </div>
    `).join("");
}

function getStorefrontName(product) {
    return product.nomeLoja || product.nome;
}

function getStorefrontPrice(product) {
    return product.precoLoja ?? product.preco;
}

function getStorefrontImage(product) {
    return product.imagemLojaUrl || product.imagemUrl;
}

function getStorefrontImages(product) {
    return product.imagensLojaExtras?.length ? product.imagensLojaExtras : product.imagensExtras || [];
}

function isAcceptedImageFile(file) {
    const maxSize = 5 * 1024 * 1024;
    return file.type.startsWith("image/") && file.size <= maxSize;
}

function parseImageList(value) {
    return mergeImageUrls(value.split(/\r?\n/));
}

function parseTextList(value) {
    return mergeTextValues(value.split(/[\n,]/));
}

function parseVariantStockList(value) {
    return value
        .split(/\r?\n/)
        .map((line) => line.trim())
        .filter(Boolean)
        .map((line) => {
            const parts = line.split("|").map((part) => part.trim());
            const quantidade = Number(parts.length >= 4 ? parts[3] : parts.at(-1) || 0);
            const hasCompactQuantity = parts.length < 4;
            return {
                tamanho: emptyToNull(parts[0] || ""),
                cor: emptyToNull(hasCompactQuantity && parts.length === 2 ? "" : parts[1] || ""),
                modelo: emptyToNull(hasCompactQuantity ? "" : parts[2] || ""),
                quantidade: Number.isFinite(quantidade) ? Math.max(0, Math.floor(quantidade)) : 0,
                sku: emptyToNull(hasCompactQuantity ? "" : parts[4] || "")
            };
        })
        .filter((variation) => variation.tamanho || variation.cor || variation.modelo || variation.quantidade > 0 || variation.sku);
}

function renderProductVariantRows(variations = []) {
    els.productVariantRows.innerHTML = "";
    (variations || []).forEach((variation) => addProductVariantRow(variation));
    syncProductVariantTextarea();
}

function applyVariantQuickAdd() {
    const raw = els.variantQuickAddInput.value.trim();
    if (!raw) {
        return;
    }

    const chunks = raw.split(/[,\n]/).map((chunk) => chunk.trim()).filter(Boolean);
    const parsed = [];
    for (const chunk of chunks) {
        const match = chunk.match(/^(\d+)\s*-\s*(.+)$/);
        if (!match) {
            showToast(`Não entendi "${chunk}". Use o formato 1-M, 2-P.`);
            return;
        }
        parsed.push({ quantidade: Number(match[1]), tamanho: match[2].trim() });
    }

    const existingSizes = parseTextList(els.productSizes.value);
    const newSizes = parsed
        .map((item) => item.tamanho)
        .filter((tamanho) => !existingSizes.some((size) => size.toLowerCase() === tamanho.toLowerCase()));
    if (newSizes.length) {
        els.productSizes.value = [...existingSizes, ...new Set(newSizes)].join(", ");
    }

    parsed.forEach(({ quantidade, tamanho }) => {
        const existingRow = Array.from(els.productVariantRows.querySelectorAll("[data-variant-row]")).find((row) => {
            const tamanhoValue = row.querySelector('[data-variant-field="tamanho"]')?.value.trim().toLowerCase();
            const corValue = row.querySelector('[data-variant-field="cor"]')?.value.trim();
            const modeloValue = row.querySelector('[data-variant-field="modelo"]')?.value.trim();
            return tamanhoValue === tamanho.toLowerCase() && !corValue && !modeloValue;
        });

        if (existingRow) {
            existingRow.querySelector('[data-variant-field="quantidade"]').value = quantidade;
        } else {
            addProductVariantRow({ tamanho, quantidade });
        }
    });

    syncProductVariantTextarea();
    els.variantQuickAddInput.value = "";
    els.variantQuickAddInput.focus();
}

function buildVariantSku(baseSku, tamanho) {
    const base = (baseSku || "").trim();
    const size = (tamanho || "").trim();
    if (!base || !size) {
        return "";
    }
    const suffix = size
        .toUpperCase()
        .normalize("NFD")
        .replace(/[̀-ͯ]/g, "")
        .replace(/[^A-Z0-9]+/g, "");
    return suffix ? `${base}-${suffix}` : base;
}

function addProductVariantRow(variation = {}) {
    const row = document.createElement("div");
    row.className = "variant-grid variant-row";
    row.dataset.variantRow = "true";
    const autoSku = buildVariantSku(els.productSku.value, variation.tamanho);
    const hasManualSku = Boolean(variation.sku) && variation.sku !== autoSku;
    row.dataset.skuAuto = hasManualSku ? "false" : "true";
    row.innerHTML = `
        <input type="text" data-variant-field="tamanho" value="${escapeHtml(variation.tamanho || "")}" placeholder="P">
        <input type="text" data-variant-field="cor" value="${escapeHtml(variation.cor || "")}" placeholder="Preto">
        <input type="text" data-variant-field="modelo" value="${escapeHtml(variation.modelo || "")}" placeholder="Básica">
        <input type="number" min="0" step="1" data-variant-field="quantidade" value="${Number(variation.quantidade || 0)}">
        <input type="text" data-variant-field="sku" value="${escapeHtml(variation.sku || autoSku)}" placeholder="Gerado automaticamente">
        <button class="button button-danger" type="button" data-variant-action="remove">Remover</button>
    `;

    const tamanhoInput = row.querySelector('[data-variant-field="tamanho"]');
    const skuInput = row.querySelector('[data-variant-field="sku"]');
    tamanhoInput.addEventListener("input", () => {
        if (row.dataset.skuAuto !== "false") {
            skuInput.value = buildVariantSku(els.productSku.value, tamanhoInput.value);
        }
    });
    skuInput.addEventListener("input", () => {
        row.dataset.skuAuto = skuInput.value === buildVariantSku(els.productSku.value, tamanhoInput.value) ? "true" : "false";
    });

    els.productVariantRows.appendChild(row);
    syncProductVariantTextarea();
}

function refreshAutoVariantSkus() {
    if (!els.productVariantRows) {
        return;
    }
    Array.from(els.productVariantRows.querySelectorAll("[data-variant-row]")).forEach((row) => {
        if (row.dataset.skuAuto === "false") {
            return;
        }
        const tamanho = row.querySelector('[data-variant-field="tamanho"]')?.value || "";
        const skuInput = row.querySelector('[data-variant-field="sku"]');
        if (skuInput) {
            skuInput.value = buildVariantSku(els.productSku.value, tamanho);
        }
    });
    syncProductVariantTextarea();
}

function collectProductVariantRows() {
    return Array.from(els.productVariantRows.querySelectorAll("[data-variant-row]"))
        .map((row) => {
            const getValue = (field) => row.querySelector(`[data-variant-field="${field}"]`)?.value || "";
            const quantidade = Number(getValue("quantidade") || 0);
            return {
                tamanho: emptyToNull(getValue("tamanho")),
                cor: emptyToNull(getValue("cor")),
                modelo: emptyToNull(getValue("modelo")),
                quantidade: Number.isFinite(quantidade) ? Math.max(0, Math.floor(quantidade)) : 0,
                sku: emptyToNull(getValue("sku"))
            };
        })
        .filter((variation) => variation.tamanho || variation.cor || variation.modelo || variation.quantidade > 0 || variation.sku);
}

function syncProductVariantTextarea() {
    els.productVariantStock.value = formatVariantStockList(collectProductVariantRows());
}

function formatVariantStockList(variations) {
    return (variations || [])
        .map((variation) => [
            variation.tamanho || "",
            variation.cor || "",
            variation.modelo || "",
            variation.quantidade || 0,
            variation.sku || ""
        ].join(" | "))
        .join("\n");
}

function getLowStockItems(threshold = 3) {
    return state.products
        .filter((product) => product.ativo)
        .flatMap((product) => {
            if (product.variacoesEstoque?.length) {
                return product.variacoesEstoque
                    .filter((variation) => Number(variation.quantidade || 0) <= threshold)
                    .map((variation) => ({
                        id: `${product.id}-${formatPdvVariation(variation)}`,
                        nome: product.nome,
                        detalhe: `${product.categoria} · ${formatPdvVariation(variation) || "Variação"} · ${currency.format(product.preco)}`,
                        quantidade: Number(variation.quantidade || 0)
                    }));
            }

            return product.quantidadeEmEstoque <= threshold
                ? [{
                    id: product.id,
                    nome: product.nome,
                    detalhe: `${product.categoria} · ${currency.format(product.preco)}`,
                    quantidade: product.quantidadeEmEstoque
                }]
                : [];
        })
        .sort((a, b) => a.quantidade - b.quantidade || a.nome.localeCompare(b.nome, "pt-BR"));
}

function mergeTextValues(...groups) {
    const seen = new Set();
    return groups
        .flat()
        .map((source) => String(source || "").trim())
        .filter(Boolean)
        .filter((source) => {
            const key = source.toLowerCase();
            if (seen.has(key)) {
                return false;
            }

            seen.add(key);
            return true;
        });
}

function mergeImageUrls(...groups) {
    const seen = new Set();
    return groups
        .flat()
        .map((source) => String(source || "").trim())
        .filter(Boolean)
        .filter((source) => {
            const key = source.toLowerCase();
            if (seen.has(key)) {
                return false;
            }

            seen.add(key);
            return true;
        });
}

function addToCart(productId, variation = null) {
    const product = state.products.find((item) => item.id === productId);
    if (!product || product.quantidadeEmEstoque <= 0) {
        return;
    }

    const selectedVariation = normalizePdvVariation(variation || getDefaultPdvVariation(product));
    const validation = validatePdvVariation(product, selectedVariation);
    if (validation) {
        showToast(validation);
        return;
    }

    const currentQuantity = hasVariantStock(product)
        ? cartQuantity(productId, selectedVariation)
        : cartQuantity(productId);
    const availableQuantity = getPdvAvailableQuantity(product, selectedVariation);
    if (currentQuantity >= availableQuantity) {
        showToast("Quantidade máxima disponível no estoque.");
        return;
    }

    const itemId = getPdvCartItemKey(productId, selectedVariation);
    const item = state.cart.find((cartItem) => cartItem.id === itemId);
    if (item) {
        item.quantidade += 1;
    } else {
        state.cart.push({
            id: itemId,
            produtoId: productId,
            quantidade: 1,
            ...selectedVariation
        });
    }

    renderPdvProducts();
    renderCart();
}

function changeCart(itemId, action) {
    const item = state.cart.find((cartItem) => cartItem.id === itemId);
    const product = state.products.find((productItem) => productItem.id === item?.produtoId);

    if (!item || !product) {
        return;
    }

    if (action === "increase") {
        const variation = normalizePdvVariation(item);
        const currentQuantity = hasVariantStock(product)
            ? cartQuantity(product.id, variation)
            : cartQuantity(product.id);
        if (currentQuantity >= getPdvAvailableQuantity(product, variation)) {
            showToast("Quantidade máxima disponível no estoque.");
            return;
        }

        item.quantidade += 1;
    }

    if (action === "decrease") {
        item.quantidade -= 1;
    }

    if (action === "remove" || item.quantidade <= 0) {
        state.cart = state.cart.filter((cartItem) => cartItem.id !== itemId);
    }

    renderPdvProducts();
    renderCart();
}

async function updateOrderStatus(orderId, status, select = null) {
    const order = state.orders.find((item) => item.id === orderId);
    const previousStatus = order?.status;

    try {
        if (select) {
            select.disabled = true;
        }
        await api(`/pedidos-online/${orderId}/status`, {
            method: "PUT",
            body: JSON.stringify({ status })
        });

        await refreshScoped(["orders", "summary"]);
        showToast("Status do pedido atualizado.");
    } catch (error) {
        if (previousStatus) {
            select.value = previousStatus;
        }

        showToast(error.message || "Não foi possível atualizar o status.");
    } finally {
        if (select) {
            select.disabled = false;
        }
    }
}

async function quickUpdateOnlineOrderStatus(orderId, status) {
    if (!status) {
        return;
    }

    if (status === "Cancelado" && !window.confirm("Cancelar este pedido online? O estoque dos itens volta automaticamente.")) {
        return;
    }

    const select = document.querySelector(`[data-online-order-status][data-id="${orderId}"]`);
    await updateOrderStatus(orderId, status, select);
}

async function confirmOnlineOrderPayment(orderId) {
    const confirmInput = document.querySelector(`[data-payment-confirm="${orderId}"]`);
    const referenceInput = document.querySelector(`[data-payment-reference="${orderId}"]`);
    const noteInput = document.querySelector(`[data-payment-note="${orderId}"]`);

    if (confirmInput) {
        confirmInput.checked = true;
    }

    if (referenceInput && !referenceInput.value.trim()) {
        referenceInput.value = "Confirmado no painel";
    }

    if (noteInput && !noteInput.value.trim()) {
        noteInput.value = "Pagamento conferido pela loja.";
    }

    await saveOnlineOrderPayment(orderId);
}

async function saveOnlineOrderTracking(orderId) {
    const codeInput = document.querySelector(`[data-tracking-code="${orderId}"]`);
    const noteInput = document.querySelector(`[data-tracking-note="${orderId}"]`);
    if (!codeInput || !noteInput) {
        return;
    }

    try {
        await api(`/pedidos-online/${orderId}/rastreamento`, {
            method: "PUT",
            body: JSON.stringify({
                codigoRastreio: emptyToNull(codeInput.value),
                observacaoEntrega: emptyToNull(noteInput.value)
            })
        });

        showToast("Rastreamento salvo.");
        await refreshScoped(["orders"]);
    } catch (error) {
        showToast(error.message || "Não foi possível salvar o rastreamento.");
    }
}

async function saveOnlineOrderPayment(orderId) {
    const referenceInput = document.querySelector(`[data-payment-reference="${orderId}"]`);
    const noteInput = document.querySelector(`[data-payment-note="${orderId}"]`);
    const confirmInput = document.querySelector(`[data-payment-confirm="${orderId}"]`);
    if (!referenceInput || !noteInput || !confirmInput) {
        return;
    }

    try {
        await api(`/pedidos-online/${orderId}/pagamento`, {
            method: "PUT",
            body: JSON.stringify({
                referenciaPagamento: emptyToNull(referenceInput.value),
                observacaoPagamento: emptyToNull(noteInput.value),
                confirmarPagamento: confirmInput.checked
            })
        });

        showToast(confirmInput.checked ? "Pagamento confirmado." : "Dados de pagamento salvos.");
        await refreshScoped(["orders", "summary"]);
    } catch (error) {
        showToast(error.message || "Não foi possível salvar o pagamento.");
    }
}

function syncCartWithStock() {
    const usedByStockKey = new Map();
    state.cart = state.cart
        .map((item) => {
            const product = state.products.find((productItem) => productItem.id === item.produtoId);
            if (!product || product.quantidadeEmEstoque <= 0) {
                return null;
            }

            const variation = normalizePdvVariation(item);
            const itemId = getPdvCartItemKey(product.id, variation);
            const stockKey = hasVariantStock(product) ? itemId : product.id;
            const used = usedByStockKey.get(stockKey) || 0;
            const available = getPdvAvailableQuantity(product, variation) - used;
            if (available <= 0) {
                return null;
            }

            const quantity = Math.min(item.quantidade, available);
            usedByStockKey.set(stockKey, used + quantity);
            return {
                id: itemId,
                produtoId: product.id,
                quantidade: quantity,
                ...variation
            };
        })
        .filter(Boolean);
}

function cartQuantity(productId, variation = null) {
    const normalized = variation ? normalizePdvVariation(variation) : null;
    return state.cart
        .filter((item) => item.produtoId === productId)
        .filter((item) => !normalized || getPdvCartItemKey(productId, item) === getPdvCartItemKey(productId, normalized))
        .reduce((sum, item) => sum + item.quantidade, 0);
}

function getPdvAvailableQuantity(product, variation = null) {
    if (!hasVariantStock(product)) {
        return product?.quantidadeEmEstoque || 0;
    }

    return findPdvVariantStock(product, variation)?.quantidade || 0;
}

function hasVariantStock(product) {
    return Boolean(product?.variacoesEstoque?.length);
}

function findPdvVariantStock(product, variation = {}) {
    const selected = normalizePdvVariation(variation);
    return (product?.variacoesEstoque || []).find((candidate) =>
        normalize(candidate.tamanho || "") === normalize(selected.tamanho || "") &&
        normalize(candidate.cor || "") === normalize(selected.cor || "") &&
        normalize(candidate.modelo || "") === normalize(selected.modelo || ""));
}

function getDefaultPdvVariation(product) {
    const availableVariant = (product.variacoesEstoque || [])
        .find((variation) => Number(variation.quantidade || 0) > cartQuantity(product.id, variation));
    if (availableVariant) {
        return availableVariant;
    }

    return {
        tamanho: product.tamanhos?.[0] || null,
        cor: product.cores?.[0] || null,
        modelo: product.modelos?.[0] || null
    };
}

function validatePdvVariation(product, variation) {
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

        if (!options.some((option) => normalize(option) === normalize(variation[field]))) {
            return `${label} indisponível para essa peça.`;
        }
    }

    if (hasVariantStock(product) && getPdvAvailableQuantity(product, variation) <= 0) {
        return "Essa variação está sem estoque.";
    }

    return null;
}

function normalizePdvVariation(variation = {}) {
    return {
        tamanho: emptyToNull(String(variation?.tamanho || "")),
        cor: emptyToNull(String(variation?.cor || "")),
        modelo: emptyToNull(String(variation?.modelo || ""))
    };
}

function getPdvCartItemKey(productId, variation = {}) {
    const normalized = normalizePdvVariation(variation);
    return [
        productId,
        encodeURIComponent(normalized.tamanho || ""),
        encodeURIComponent(normalized.cor || ""),
        encodeURIComponent(normalized.modelo || "")
    ].join("|");
}

function cartTotal() {
    return state.cart.reduce((sum, item) => {
        const product = state.products.find((productItem) => productItem.id === item.produtoId);
        return sum + ((product?.preco || 0) * item.quantidade);
    }, 0);
}

function getSaleDiscount() {
    return Math.max(0, Number(els.saleDiscount.value || 0));
}

function stockBadge(product) {
    const className = product.quantidadeEmEstoque <= 3 ? "badge badge-warn" : "badge badge-ok";
    return `<span class="${className}">${product.quantidadeEmEstoque} un.</span>`;
}

function formatPayment(payment) {
    const names = {
        Dinheiro: "Dinheiro",
        Pix: "Pix",
        CartaoDebito: "Cartão débito",
        CartaoCredito: "Cartão crédito",
        Troca: "Troca"
    };
    return names[payment] || payment;
}

function formatDeliveryType(type) {
    const names = {
        Retirada: "Retirada na loja",
        EntregaLocal: "Entrega local",
        Correios: "Correios",
        Transportadora: "Transportadora",
        Personalizada: "Personalizada"
    };
    return names[type] || type;
}

function formatPanelRole(role) {
    return roleConfigs[role]?.label || role;
}

function formatCep(value) {
    const digits = String(value || "").replace(/\D/g, "");
    return digits.length === 8 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : "";
}

function formatCepRange(option) {
    const start = formatCep(option.cepInicial);
    const end = formatCep(option.cepFinal);
    if (start && end) {
        return `CEP ${start} até ${end}`;
    }

    if (start) {
        return `A partir do CEP ${start}`;
    }

    if (end) {
        return `Até o CEP ${end}`;
    }

    return "";
}

function formatListRange(label, items) {
    if (!items?.length) {
        return "";
    }

    return `${label}: ${items.slice(0, 3).join(", ")}${items.length > 3 ? "..." : ""}`;
}

function formatMovement(type) {
    const names = {
        Entrada: "Entrada",
        SaidaVendaLoja: "Saída PDV",
        SaidaPedidoOnline: "Saída online",
        Ajuste: "Ajuste"
    };
    return names[type] || type;
}

function formatOrderStatus(status) {
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

function renderOrderStatusSelect(order, context = "table") {
    const dataAttribute = context === "online" ? "data-online-order-status" : "data-order-status";
    const options = orderStatusOptions
        .map((status) => `<option value="${status}" ${status === order.status ? "selected" : ""}>${formatOrderStatus(status)}</option>`)
        .join("");

    return `
        <label class="order-status-control">
            <span>Status</span>
            <select ${dataAttribute} data-id="${order.id}" aria-label="Status do pedido ${order.id.slice(0, 8).toUpperCase()}">
                ${options}
            </select>
        </label>
    `;
}

function getOrderStatusClass(status) {
    const names = {
        Recebido: "badge-warn",
        Pago: "badge-ok",
        Separando: "badge-info",
        Enviado: "badge-info",
        Entregue: "badge-ok",
        Cancelado: "badge-muted"
    };
    return names[status] || "badge-muted";
}

function formatOrderVariation(item) {
    return [
        item.tamanho ? `Tam. ${item.tamanho}` : null,
        item.cor ? `Cor ${item.cor}` : null,
        item.modelo ? `Modelo ${item.modelo}` : null
    ].filter(Boolean).join(" · ");
}

function formatOrderTotalNote(order) {
    const notes = [];
    if (order.desconto) {
        notes.push(`Cupom ${order.cupomCodigo || "aplicado"} · desc. ${currency.format(order.desconto)}`);
    }

    if (order.entregaNome) {
        notes.push(`Entrega ${order.entregaNome} · ${currency.format(order.entregaValor || 0)}`);
    }

    return escapeHtml(notes.join(" · ") || `${(order.itens || []).reduce((sum, item) => sum + item.quantidade, 0)} unidades`);
}

async function copyOnlineOrderSummary(orderId) {
    const order = state.orders.find((item) => item.id === orderId);
    if (!order) {
        return;
    }

    const text = buildOnlineOrderSummary(order);

    try {
        await navigator.clipboard.writeText(text);
        showToast("Resumo do pedido copiado.");
    } catch {
        showToast("Não foi possível copiar automaticamente. Use os dados do card.");
    }
}

function buildOnlineOrderSummary(order) {
    const shortId = order.id.slice(0, 8).toUpperCase();
    const items = (order.itens || [])
        .map((item) => {
            const variation = formatOrderVariation(item);
            return `- ${item.quantidade}x ${item.produtoNome}${variation ? ` (${variation})` : ""}: ${currency.format(item.subtotal)}`;
        })
        .join("\n");
    const address = [
        order.ruaEntrega && order.numeroEntrega ? `${order.ruaEntrega}, ${order.numeroEntrega}` : order.enderecoEntrega,
        order.complementoEntrega,
        order.bairroEntrega,
        [order.cidadeEntrega, order.estadoEntrega].filter(Boolean).join(" - "),
        order.cepEntrega
    ].filter(Boolean).join(" | ");

    return [
        `Pedido ${shortId}`,
        `Status: ${formatOrderStatus(order.status)}`,
        `Cliente: ${order.nomeCliente}`,
        `E-mail: ${order.emailCliente}`,
        `Telefone: ${order.telefoneCliente || "Não informado"}`,
        `Pagamento: ${formatPayment(order.formaPagamento)}`,
        order.referenciaPagamento ? `Ref. pagamento: ${order.referenciaPagamento}` : null,
        order.observacaoPagamento ? `Obs. pagamento: ${order.observacaoPagamento}` : null,
        order.pagamentoConfirmadoEm ? `Pagamento confirmado em: ${formatDate(order.pagamentoConfirmadoEm)}` : null,
        order.desconto ? `Cupom: ${order.cupomCodigo || "Aplicado"} (- ${currency.format(order.desconto)})` : null,
        order.entregaNome ? `Entrega: ${order.entregaNome} (${currency.format(order.entregaValor || 0)})` : null,
        order.codigoRastreio ? `Rastreio: ${order.codigoRastreio}` : null,
        order.observacaoEntrega ? `Obs. entrega: ${order.observacaoEntrega}` : null,
        `Total: ${currency.format(order.total)}`,
        "",
        "Itens:",
        items,
        "",
        `Entrega: ${address || "Não informado"}`,
        order.observacao ? `Observação: ${order.observacao}` : null
    ].filter((line) => line !== null).join("\n");
}

function formatOrderItem(item) {
    const variation = [
        item.tamanho ? `Tam. ${item.tamanho}` : null,
        item.cor ? `Cor ${item.cor}` : null,
        item.modelo ? `Modelo ${item.modelo}` : null
    ].filter(Boolean).join(" · ");

    return `${item.quantidade}x ${escapeHtml(item.produtoNome)}${variation ? ` <span class="panel-note">(${escapeHtml(variation)})</span>` : ""}`;
}

function formatPdvVariation(item) {
    return [
        item.tamanho ? `Tam. ${item.tamanho}` : null,
        item.cor ? `Cor ${item.cor}` : null,
        item.modelo ? `Modelo ${item.modelo}` : null
    ].filter(Boolean).join(" · ");
}

function formatSaleItem(item) {
    const returned = item.quantidadeDevolvida || 0;
    const remaining = getSaleItemRemaining(item);
    const variation = formatPdvVariation(item);
    const note = returned
        ? ` <span class="panel-note">(${returned} devolvido${returned === 1 ? "" : "s"} · ${remaining} líquido${remaining === 1 ? "" : "s"})</span>`
        : "";

    return `${item.quantidade}x ${escapeHtml(item.produtoNome)}${variation ? ` <span class="panel-note">(${escapeHtml(variation)})</span>` : ""}${note}`;
}

function getSaleItemRemaining(item) {
    return Math.max(0, Number(item.quantidade || 0) - Number(item.quantidadeDevolvida || 0));
}

function formatSaleStatusBadge(sale) {
    if (sale.devolvida) {
        return '<span class="badge badge-muted">Devolvida</span>';
    }

    if (sale.devolucaoParcial) {
        return '<span class="badge badge-warn">Parcial</span>';
    }

    return '<span class="badge badge-ok">Concluída</span>';
}

function formatDate(value) {
    return value ? dateTime.format(new Date(value)) : "-";
}

function formatShortDate(value) {
    return value ? new Intl.DateTimeFormat("pt-BR").format(new Date(value)) : "-";
}

function toDateInputValue(value) {
    if (!value) {
        return "";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return "";
    }

    return date.toISOString().slice(0, 10);
}

function formatReportDate(value) {
    if (!value) {
        return "-";
    }

    const [year, month, day] = String(value).split("-").map(Number);
    if (!year || !month || !day) {
        return formatDate(value);
    }

    return new Intl.DateTimeFormat("pt-BR", {
        day: "2-digit",
        month: "short",
        year: "numeric"
    }).format(new Date(year, month - 1, day));
}

function emptyToNull(value) {
    const trimmed = value.trim();
    return trimmed ? trimmed : null;
}

function normalize(value) {
    return value
        .toLowerCase()
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "");
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function setStatus(text, mode) {
    els.apiStatus.textContent = text;
    els.apiStatus.classList.toggle("is-online", mode === "online");
    els.apiStatus.classList.toggle("is-offline", mode === "offline");
}

function showToast(message) {
    els.toast.textContent = message;
    els.toast.classList.add("is-visible");
    window.clearTimeout(showToast.timeoutId);
    showToast.timeoutId = window.setTimeout(() => {
        els.toast.classList.remove("is-visible");
    }, 2800);
}
