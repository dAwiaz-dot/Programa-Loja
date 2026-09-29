// Operação da loja física: caixa, fiado, vendedoras, cancelamento de venda,
// contagem de estoque e relacionamento com clientes. Usa o mesmo `state`,
// `els`, `api` e helpers do app.js (carregado antes deste arquivo).

const SELLER_STORAGE_KEY = "nana-pdv-vendedora";
const STOCK_COUNT_STORAGE_KEY = "nana-contagem-estoque";
const INACTIVE_CUSTOMER_DAYS = 60;

function cacheOperacaoElements() {
    const ids = [
        "pdvCashStatus", "saleSeller", "saleInstallmentsBox", "saleInstallments", "saleCreditBox", "saleDueDate",
        "saleExtraDetails", "quickCustomerButton", "saleCustomerInfo",
        "sellerRankingTable",
        "cashMetricExpected", "cashMetricSold", "cashMetricCount", "cashMetricCredit", "cashSessionNote",
        "cashSessionBadge", "cashSessionBody", "cashOpenForm", "cashOpenValue", "cashOpenNote", "cashMoveForm",
        "cashMoveValue", "cashMoveReason", "cashCloseForm", "cashCloseValue", "cashClosePreview", "cashCloseNote",
        "cashHistoryTable",
        "creditMetricOpen", "creditMetricCustomers", "creditMetricLate", "creditMetricPaid", "creditCount",
        "creditSearch", "creditFilter", "creditTable", "creditStatementTitle", "creditStatementNote",
        "creditPaymentForm", "creditPaymentValue", "creditPaymentMethod", "creditPaymentNote", "creditPayAllButton",
        "creditStatementList",
        "stockCountToggle", "stockCountBody", "stockCountScan", "stockCountSearch", "stockCountCategory",
        "stockCountOnlyDiff", "stockCountTable", "stockCountSummary", "stockCountClear", "stockCountApply",
        "customerFormTitle", "cancelCustomerEditButton", "customerIdInput", "customerBirthInput", "customerSubmitButton",
        "customerBirthdayList", "customerInactiveList",
        "sellerFormMode", "cancelSellerEditButton", "sellerForm", "sellerIdInput", "sellerNameInput",
        "sellerCommissionInput", "sellerActiveInput", "sellerCount", "sellerList",
        "cancelSaleModal", "cancelSaleForm", "cancelSaleSummary", "closeCancelSaleButton", "cancelSaleReason",
        "quickCustomerModal", "quickCustomerForm", "closeQuickCustomerButton", "quickCustomerName",
        "quickCustomerPhone", "quickCustomerBirth"
    ];
    ids.forEach((id) => {
        els[id] = document.querySelector(`#${id}`);
    });
}

function bindOperacaoEvents() {
    // PDV
    document.querySelectorAll("input[name='payment']").forEach((input) => {
        input.addEventListener("change", () => {
            updatePdvPaymentExtras();
            if (getSelectedPayment() === "Fiado") {
                els.saleExtraDetails.open = true;
            }
        });
    });
    els.saleSeller.addEventListener("change", () => {
        writeStorage(SELLER_STORAGE_KEY, els.saleSeller.value);
    });
    els.saleCustomer.addEventListener("change", renderSaleCustomerInfo);
    els.quickCustomerButton.addEventListener("click", openQuickCustomerDialog);
    els.closeQuickCustomerButton.addEventListener("click", closeQuickCustomerDialog);
    els.quickCustomerForm.addEventListener("submit", submitQuickCustomer);
    els.pdvCashStatus.addEventListener("click", (event) => {
        if (event.target.closest("[data-open-cash]")) {
            showView("cashRegister");
            els.cashOpenValue.focus();
        }
    });

    // Cancelamento
    els.closeCancelSaleButton.addEventListener("click", closeCancelSaleDialog);
    els.cancelSaleForm.addEventListener("submit", submitCancelSale);

    // Caixa
    els.cashOpenForm.addEventListener("submit", submitCashOpen);
    els.cashMoveForm.addEventListener("submit", submitCashMove);
    els.cashCloseForm.addEventListener("submit", submitCashClose);
    els.cashCloseValue.addEventListener("input", renderCashClosePreview);

    // Fiado
    els.creditSearch.addEventListener("input", renderCredit);
    els.creditFilter.addEventListener("change", renderCredit);
    els.creditTable.addEventListener("click", (event) => {
        const button = event.target.closest("[data-credit-open]");
        if (button) {
            loadCreditStatement(button.dataset.creditOpen);
        }
    });
    els.creditPaymentForm.addEventListener("submit", submitCreditPayment);
    els.creditPayAllButton.addEventListener("click", () => {
        const saldo = Number(state.creditStatement?.resumo?.saldo || 0);
        if (saldo > 0) {
            els.creditPaymentValue.value = saldo.toFixed(2);
            els.creditPaymentValue.focus();
        }
    });
    els.creditStatementList.addEventListener("click", (event) => {
        const reverse = event.target.closest("[data-credit-reverse]");
        if (reverse) {
            reverseCreditPayment(reverse.dataset.creditReverse);
            return;
        }

        const saleButton = event.target.closest("[data-credit-sale]");
        if (saleButton) {
            const sale = state.sales.find((item) => item.id === saleButton.dataset.creditSale);
            if (sale) {
                state.lastReceipt = sale;
                renderReceipt(sale);
            }
        }
    });

    // Vendedoras
    els.sellerForm.addEventListener("submit", submitSeller);
    els.cancelSellerEditButton.addEventListener("click", resetSellerForm);
    els.sellerList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-seller-edit]");
        if (button) {
            editSeller(button.dataset.sellerEdit);
        }
    });

    // Clientes
    els.cancelCustomerEditButton.addEventListener("click", resetCustomerForm);
    els.customerList.addEventListener("click", (event) => {
        const button = event.target.closest("[data-customer-edit]");
        if (button) {
            editCustomer(button.dataset.customerEdit);
        }
    });

    // Contagem de estoque
    els.stockCountToggle.addEventListener("click", () => {
        els.stockCountBody.classList.toggle("hidden");
        renderStockCount();
        if (!els.stockCountBody.classList.contains("hidden")) {
            els.stockCountScan.focus();
        }
    });
    els.stockCountSearch.addEventListener("input", renderStockCount);
    els.stockCountCategory.addEventListener("change", renderStockCount);
    els.stockCountOnlyDiff.addEventListener("change", renderStockCount);
    els.stockCountScan.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            scanStockCountCode(els.stockCountScan.value);
            els.stockCountScan.value = "";
        }
    });
    els.stockCountTable.addEventListener("input", (event) => {
        const input = event.target.closest("[data-count-key]");
        if (input) {
            updateStockCountValue(input.dataset.countKey, input.value);
        }
    });
    els.stockCountClear.addEventListener("click", () => {
        if (window.confirm("Apagar toda a contagem feita neste aparelho?")) {
            writeStockCounts({});
            renderStockCount();
        }
    });
    els.stockCountApply.addEventListener("click", applyStockCount);

    document.addEventListener("keydown", (event) => {
        if (event.key !== "Escape") {
            return;
        }

        closeCancelSaleDialog();
        closeQuickCustomerDialog();
    });
}

// ---------- Helpers ----------

function readStorage(key, fallback) {
    try {
        const value = window.localStorage.getItem(key);
        return value === null ? fallback : JSON.parse(value);
    } catch {
        return fallback;
    }
}

function writeStorage(key, value) {
    try {
        window.localStorage.setItem(key, JSON.stringify(value));
    } catch {
        // Navegador sem armazenamento local: segue sem lembrar.
    }
}

function isAdminUser() {
    return state.currentUser?.perfil === "Admin";
}

function getSelectedPayment() {
    return document.querySelector("input[name='payment']:checked")?.value || "Pix";
}

function localDateKey(value) {
    const date = value instanceof Date ? value : new Date(value);
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${date.getFullYear()}-${month}-${day}`;
}

function daysBetween(from, to = new Date()) {
    const start = new Date(from);
    start.setHours(0, 0, 0, 0);
    const end = new Date(to);
    end.setHours(0, 0, 0, 0);
    return Math.round((end - start) / 86400000);
}

function formatBirthday(value) {
    if (!value) {
        return "";
    }

    const [, month, day] = value.split("-");
    return `${day}/${month}`;
}

function formatSalePayment(sale) {
    const label = formatPayment(sale.formaPagamento);
    if (sale.formaPagamento === "CartaoCredito" && Number(sale.parcelas) > 1) {
        return `${label} ${sale.parcelas}x`;
    }

    if (sale.formaPagamento === "Fiado" && sale.vencimentoEm) {
        return `${label} · vence ${formatShortDate(sale.vencimentoEm)}`;
    }

    return label;
}

function whatsappUrl(phone, message) {
    let digits = String(phone || "").replace(/\D/g, "");
    if (!digits) {
        return null;
    }

    if (digits.length <= 11) {
        digits = `55${digits}`;
    }

    return `https://wa.me/${digits}?text=${encodeURIComponent(message)}`;
}

function whatsappButton(phone, message, label = "WhatsApp") {
    const url = whatsappUrl(phone, message);
    return url
        ? `<a class="button button-secondary" href="${escapeHtml(url)}" target="_blank" rel="noopener">${label}</a>`
        : "";
}

function firstName(name) {
    return String(name || "").trim().split(/\s+/)[0] || "";
}

// ---------- PDV ----------

function updatePdvPaymentExtras() {
    const payment = getSelectedPayment();
    els.saleInstallmentsBox.classList.toggle("hidden", payment !== "CartaoCredito");
    els.saleCreditBox.classList.toggle("hidden", payment !== "Fiado");
    if (payment !== "CartaoCredito") {
        els.saleInstallments.value = "1";
    }

    if (payment === "Fiado" && !els.saleDueDate.value) {
        const due = new Date();
        due.setDate(due.getDate() + 30);
        els.saleDueDate.value = localDateKey(due);
    }

    els.saleDueDate.min = localDateKey(new Date());
}

function resetPdvPaymentExtras() {
    els.saleInstallments.value = "1";
    els.saleDueDate.value = "";
    const pix = document.querySelector("input[name='payment'][value='Pix']");
    if (pix) {
        pix.checked = true;
    }

    updatePdvPaymentExtras();
    renderSaleCustomerInfo();
}

function renderSellerOptions() {
    if (!els.saleSeller) {
        return;
    }

    const current = els.saleSeller.value || readStorage(SELLER_STORAGE_KEY, "");
    const sellers = state.sellers.filter((seller) => seller.ativo);
    els.saleSeller.innerHTML = [`<option value="">Sem vendedora</option>`]
        .concat(sellers.map((seller) => `<option value="${seller.id}">${escapeHtml(seller.nome)}</option>`))
        .join("");
    els.saleSeller.value = sellers.some((seller) => seller.id === current) ? current : "";
    els.saleSeller.closest("label").classList.toggle("hidden", sellers.length === 0);
}

function renderSaleCustomerInfo() {
    if (!els.saleCustomerInfo) {
        return;
    }

    const customer = state.customersSimple.find((item) => item.id === els.saleCustomer.value);
    if (!customer) {
        els.saleCustomerInfo.classList.add("hidden");
        els.saleCustomerInfo.innerHTML = "";
        return;
    }

    const parts = [];
    if (customer.ultimaCompraEm) {
        const days = daysBetween(customer.ultimaCompraEm);
        parts.push(`Última compra ${days === 0 ? "hoje" : `há ${days} dia${days === 1 ? "" : "s"}`}`);
        parts.push(`${customer.comprasLoja} compra${customer.comprasLoja === 1 ? "" : "s"} · ${currency.format(customer.totalLoja || 0)}`);
    } else {
        parts.push("Primeira compra na loja");
    }

    const birthday = customer.dataNascimento ? getBirthdayInfo(customer.dataNascimento) : null;
    const alerts = [];
    if (Number(customer.saldoFiado) > 0) {
        alerts.push(`<span class="badge badge-warn">Fiado em aberto ${currency.format(customer.saldoFiado)}</span>`);
    }

    if (birthday && birthday.daysUntil <= 7) {
        alerts.push(`<span class="badge badge-info">${birthday.daysUntil === 0 ? "Aniversário hoje!" : `Aniversário em ${birthday.daysUntil} dia${birthday.daysUntil === 1 ? "" : "s"}`}</span>`);
    }

    els.saleCustomerInfo.classList.remove("hidden");
    els.saleCustomerInfo.innerHTML = `
        <span>${escapeHtml(parts.join(" · "))}</span>
        ${alerts.length ? `<div>${alerts.join(" ")}</div>` : ""}
    `;
}

function renderPdvCashStatus() {
    if (!els.pdvCashStatus) {
        return;
    }

    els.pdvCashStatus.classList.remove("hidden");
    if (!state.cash) {
        els.pdvCashStatus.className = "cash-status is-closed";
        els.pdvCashStatus.innerHTML = `
            <span>Caixa fechado. Abra o caixa pra conferir o dinheiro no fim do dia.</span>
            <button class="button button-secondary" type="button" data-open-cash>Abrir caixa</button>
        `;
        return;
    }

    const session = state.cash.sessao;
    els.pdvCashStatus.className = "cash-status";
    els.pdvCashStatus.innerHTML = `
        <span>Caixa aberto por ${escapeHtml(session.abertoPor)} às ${new Date(session.abertoEm).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}</span>
        <strong>${currency.format(state.cash.dinheiroEsperado)} na gaveta</strong>
    `;
}

function openQuickCustomerDialog() {
    els.quickCustomerForm.reset();
    els.quickCustomerModal.classList.remove("hidden");
    els.quickCustomerName.focus();
}

function closeQuickCustomerDialog() {
    els.quickCustomerModal?.classList.add("hidden");
}

async function submitQuickCustomer(event) {
    event.preventDefault();
    try {
        const customer = await api("/clientes-painel", {
            method: "POST",
            body: JSON.stringify({
                nome: els.quickCustomerName.value.trim(),
                telefone: emptyToNull(els.quickCustomerPhone.value),
                email: null,
                dataNascimento: emptyToNull(els.quickCustomerBirth.value)
            })
        });

        closeQuickCustomerDialog();
        await refreshScoped(can("viewCustomers") ? ["customersSimple", "customers"] : ["customersSimple"]);
        els.saleCustomer.value = customer.id;
        renderSaleCustomerInfo();
        showToast("Cliente cadastrada e escolhida na venda.");
    } catch (error) {
        showToast(error.message);
    }
}

// ---------- Cancelamento ----------

function canCancelSale(sale) {
    if (sale.cancelada || sale.devolvida || sale.devolucaoParcial) {
        return false;
    }

    return isAdminUser() || localDateKey(sale.criadaEm) === localDateKey(new Date());
}

function openCancelSaleDialog(saleId) {
    const sale = state.sales.find((item) => item.id === saleId);
    if (!sale || !canCancelSale(sale)) {
        return;
    }

    state.cancelingSaleId = saleId;
    els.cancelSaleForm.reset();
    els.cancelSaleSummary.textContent = `Venda ${formatSaleCode(sale)} · ${currency.format(sale.total)} · ${formatDate(sale.criadaEm)}`;
    els.cancelSaleModal.classList.remove("hidden");
    els.cancelSaleReason.focus();
}

function closeCancelSaleDialog() {
    state.cancelingSaleId = null;
    els.cancelSaleModal?.classList.add("hidden");
}

async function submitCancelSale(event) {
    event.preventDefault();
    const saleId = state.cancelingSaleId;
    if (!saleId) {
        return;
    }

    try {
        await api(`/pdv/vendas/${saleId}/cancelamento`, {
            method: "POST",
            body: JSON.stringify({ motivo: els.cancelSaleReason.value.trim() })
        });
        closeCancelSaleDialog();
        showToast("Venda cancelada e peças devolvidas ao estoque.");
        await refreshScoped(["products", "movements", "sales", "summary", "customers", "customersSimple", "cash", "credit"]);
    } catch (error) {
        showToast(error.message);
    }
}

// ---------- Vendas por vendedora ----------

function renderSellerRanking(activeSales) {
    if (!els.sellerRankingTable) {
        return;
    }

    const groups = new Map();
    activeSales.forEach((sale) => {
        const key = sale.vendedorId || "";
        const entry = groups.get(key) || {
            nome: sale.vendedorNome || "Sem vendedora",
            vendas: 0,
            pecas: 0,
            total: 0,
            comissao: 0
        };
        entry.vendas += 1;
        entry.pecas += sale.itens.reduce((sum, item) => sum + getSaleItemRemaining(item), 0);
        entry.total += Number(sale.total || 0);
        entry.comissao += Number(sale.total || 0) * Number(sale.comissaoPercentual || 0) / 100;
        groups.set(key, entry);
    });

    const rows = [...groups.values()].sort((a, b) => b.total - a.total);
    els.sellerRankingTable.innerHTML = rows.length
        ? rows.map((row) => `
            <tr>
                <td><strong>${escapeHtml(row.nome)}</strong></td>
                <td>${row.vendas}</td>
                <td>${row.pecas}</td>
                <td>${currency.format(row.total)}</td>
                <td>${currency.format(row.vendas ? row.total / row.vendas : 0)}</td>
                <td>${row.comissao ? currency.format(row.comissao) : '<span class="panel-note">-</span>'}</td>
            </tr>
        `).join("")
        : `<tr><td colspan="6"><div class="empty-state">Nenhuma venda no período.</div></td></tr>`;
}

// ---------- Vendedoras (cadastro) ----------

function renderSellers() {
    if (!els.sellerList) {
        return;
    }

    els.sellerCount.textContent = `${state.sellers.length} ${state.sellers.length === 1 ? "vendedora" : "vendedoras"}`;
    els.sellerList.innerHTML = state.sellers.length
        ? state.sellers.map((seller) => `
            <div class="list-item">
                <div>
                    <strong>${escapeHtml(seller.nome)}</strong>
                    <span>Comissão ${Number(seller.comissaoPercentual || 0).toLocaleString("pt-BR")}%</span>
                </div>
                <div class="table-actions">
                    <span class="badge ${seller.ativo ? "badge-ok" : "badge-muted"}">${seller.ativo ? "Ativa" : "Inativa"}</span>
                    <button class="button button-ghost" type="button" data-seller-edit="${seller.id}">Editar</button>
                </div>
            </div>
        `).join("")
        : `<div class="empty-state">Nenhuma vendedora cadastrada. Cadastre pra ela aparecer no PDV.</div>`;
}

function editSeller(id) {
    const seller = state.sellers.find((item) => item.id === id);
    if (!seller) {
        return;
    }

    els.sellerIdInput.value = seller.id;
    els.sellerNameInput.value = seller.nome;
    els.sellerCommissionInput.value = seller.comissaoPercentual;
    els.sellerActiveInput.checked = seller.ativo;
    els.sellerFormMode.textContent = `Editando ${seller.nome}`;
    els.cancelSellerEditButton.classList.remove("hidden");
    els.sellerNameInput.focus();
}

function resetSellerForm() {
    els.sellerForm.reset();
    els.sellerIdInput.value = "";
    els.sellerActiveInput.checked = true;
    els.sellerFormMode.textContent = "Aparecem no PDV pra marcar quem vendeu e calcular comissão";
    els.cancelSellerEditButton.classList.add("hidden");
}

async function submitSeller(event) {
    event.preventDefault();
    const id = els.sellerIdInput.value;
    try {
        await api(id ? `/vendedores/${id}` : "/vendedores", {
            method: id ? "PUT" : "POST",
            body: JSON.stringify({
                nome: els.sellerNameInput.value.trim(),
                comissaoPercentual: Number(els.sellerCommissionInput.value || 0),
                ativo: els.sellerActiveInput.checked
            })
        });
        resetSellerForm();
        showToast("Vendedora salva.");
        await refreshScoped(["sellers"]);
    } catch (error) {
        showToast(error.message);
    }
}

// ---------- Caixa ----------

function renderCashRegister() {
    if (!els.cashSessionBody) {
        return;
    }

    const cash = state.cash;
    els.cashOpenForm.classList.toggle("hidden", Boolean(cash));
    els.cashMoveForm.classList.toggle("hidden", !cash);
    els.cashCloseForm.classList.toggle("hidden", !cash);
    els.cashSessionBadge.className = `badge ${cash ? "badge-ok" : "badge-muted"}`;
    els.cashSessionBadge.textContent = cash ? "Aberto" : "Fechado";

    if (!cash) {
        els.cashMetricExpected.textContent = currency.format(0);
        els.cashMetricSold.textContent = currency.format(0);
        els.cashMetricCount.textContent = "0";
        els.cashMetricCredit.textContent = currency.format(0);
        els.cashSessionNote.textContent = "Nenhum caixa aberto";
        els.cashSessionBody.innerHTML = `<div class="empty-state">Abra o caixa com o troco da gaveta pra começar o dia. No fim do expediente, conte o dinheiro e feche: o sistema mostra se sobrou ou faltou.</div>`;
    } else {
        const session = cash.sessao;
        els.cashMetricExpected.textContent = currency.format(cash.dinheiroEsperado);
        els.cashMetricSold.textContent = currency.format(cash.totalVendas);
        els.cashMetricCount.textContent = cash.quantidadeVendas;
        els.cashMetricCredit.textContent = currency.format(cash.recebimentosFiadoDinheiro + cash.recebimentosFiadoOutros);
        els.cashSessionNote.textContent = `Aberto por ${session.abertoPor} em ${formatDate(session.abertoEm)}`;
        els.cashSessionBody.innerHTML = `
            <h3 class="cash-subtitle">Dinheiro na gaveta</h3>
            <div class="payment-summary">
                ${cashLine("Troco inicial", session.valorInicial)}
                ${cashLine("+ Vendas em dinheiro", cash.vendasDinheiro)}
                ${cashLine("+ Fiado recebido em dinheiro", cash.recebimentosFiadoDinheiro)}
                ${cashLine("+ Reforços", cash.reforcos)}
                ${cashLine("− Sangrias (retiradas)", cash.sangrias)}
                <div class="payment-item is-total">
                    <span>= Deve ter na gaveta</span>
                    <strong>${currency.format(cash.dinheiroEsperado)}</strong>
                </div>
            </div>

            <h3 class="cash-subtitle">Vendas por pagamento</h3>
            <div class="payment-summary">
                ${cash.porPagamento.length
                    ? cash.porPagamento.map((item) => `
                        <div class="payment-item">
                            <span>${formatPayment(item.formaPagamento)} · ${item.quantidade} venda${item.quantidade === 1 ? "" : "s"}</span>
                            <strong>${currency.format(item.total)}</strong>
                        </div>
                    `).join("")
                    : `<div class="empty-state">Nenhuma venda desde a abertura.</div>`}
                ${cash.recebimentosFiadoOutros ? cashLine("Fiado recebido em Pix/cartão", cash.recebimentosFiadoOutros) : ""}
                ${cash.cancelamentos ? `<div class="payment-item"><span>Vendas canceladas</span><strong>${cash.cancelamentos}</strong></div>` : ""}
            </div>

            ${session.movimentos.length ? `
                <h3 class="cash-subtitle">Retiradas e reforços</h3>
                <div class="compact-list">
                    ${session.movimentos.slice().reverse().map((move) => `
                        <div class="list-item">
                            <div>
                                <strong>${move.tipo === "Sangria" ? "Sangria" : "Reforço"} · ${currency.format(move.valor)}</strong>
                                <span>${escapeHtml(move.usuario)} · ${formatDate(move.criadoEm)}${move.motivo ? ` · ${escapeHtml(move.motivo)}` : ""}</span>
                            </div>
                        </div>
                    `).join("")}
                </div>
            ` : ""}
        `;
    }

    renderCashClosePreview();

    els.cashHistoryTable.innerHTML = state.cashHistory.length
        ? state.cashHistory.map((item) => `
            <tr>
                <td>${formatDate(item.sessao.abertoEm)}<br><span class="panel-note">${escapeHtml(item.sessao.abertoPor)}</span></td>
                <td>${formatDate(item.sessao.fechadoEm)}<br><span class="panel-note">${escapeHtml(item.sessao.fechadoPor || "")}</span></td>
                <td>${currency.format(item.totalVendas)}<br><span class="panel-note">${item.quantidadeVendas} vendas</span></td>
                <td>${currency.format(item.dinheiroEsperado)}</td>
                <td>${currency.format(item.sessao.valorContado || 0)}</td>
                <td>${formatCashDiff(item.diferenca)}${item.sessao.observacaoFechamento ? `<br><span class="panel-note">${escapeHtml(item.sessao.observacaoFechamento)}</span>` : ""}</td>
            </tr>
        `).join("")
        : `<tr><td colspan="6"><div class="empty-state">Nenhum caixa fechado ainda.</div></td></tr>`;
}

function cashLine(label, value) {
    return `
        <div class="payment-item">
            <span>${label}</span>
            <strong>${currency.format(value || 0)}</strong>
        </div>
    `;
}

function formatCashDiff(diff) {
    const value = Number(diff || 0);
    if (Math.abs(value) < 0.005) {
        return '<span class="badge badge-ok">Bateu</span>';
    }

    return value > 0
        ? `<span class="badge badge-info">Sobrou ${currency.format(value)}</span>`
        : `<span class="badge badge-danger">Faltou ${currency.format(Math.abs(value))}</span>`;
}

function renderCashClosePreview() {
    if (!els.cashClosePreview) {
        return;
    }

    if (!state.cash || els.cashCloseValue.value === "") {
        els.cashClosePreview.innerHTML = state.cash
            ? `<span class="panel-note">Esperado na gaveta: <strong>${currency.format(state.cash.dinheiroEsperado)}</strong></span>`
            : "";
        return;
    }

    const diff = Number(els.cashCloseValue.value) - Number(state.cash.dinheiroEsperado);
    els.cashClosePreview.innerHTML = `
        <span class="panel-note">Esperado ${currency.format(state.cash.dinheiroEsperado)}</span>
        ${formatCashDiff(diff)}
    `;
}

async function submitCashOpen(event) {
    event.preventDefault();
    try {
        await api("/caixa/abrir", {
            method: "POST",
            body: JSON.stringify({
                valorInicial: Number(els.cashOpenValue.value || 0),
                observacao: emptyToNull(els.cashOpenNote.value)
            })
        });
        els.cashOpenForm.reset();
        showToast("Caixa aberto.");
        await refreshScoped(["cash"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function submitCashMove(event) {
    event.preventDefault();
    const tipo = document.querySelector("input[name='cashMoveType']:checked")?.value || "Sangria";
    try {
        await api("/caixa/movimento", {
            method: "POST",
            body: JSON.stringify({
                tipo,
                valor: Number(els.cashMoveValue.value || 0),
                motivo: emptyToNull(els.cashMoveReason.value)
            })
        });
        els.cashMoveValue.value = "";
        els.cashMoveReason.value = "";
        showToast(tipo === "Sangria" ? "Sangria registrada." : "Reforço registrado.");
        await refreshScoped(["cash"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function submitCashClose(event) {
    event.preventDefault();
    const counted = Number(els.cashCloseValue.value || 0);
    const diff = counted - Number(state.cash?.dinheiroEsperado || 0);
    const diffText = Math.abs(diff) < 0.005
        ? "O dinheiro bateu."
        : diff > 0 ? `Sobrou ${currency.format(diff)}.` : `Faltou ${currency.format(Math.abs(diff))}.`;
    if (!window.confirm(`Fechar o caixa com ${currency.format(counted)} na gaveta? ${diffText}`)) {
        return;
    }

    try {
        await api("/caixa/fechar", {
            method: "POST",
            body: JSON.stringify({
                valorContado: counted,
                observacao: emptyToNull(els.cashCloseNote.value)
            })
        });
        els.cashCloseForm.reset();
        showToast(`Caixa fechado. ${diffText}`);
        await refreshScoped(["cash", "cashHistory"]);
    } catch (error) {
        showToast(error.message);
    }
}

// ---------- Fiado ----------

function renderCredit() {
    if (!els.creditTable) {
        return;
    }

    const all = state.credit || [];
    const open = all.filter((item) => item.saldo > 0.004);
    const late = open.filter((item) => item.diasAtraso > 0);
    els.creditMetricOpen.textContent = currency.format(open.reduce((sum, item) => sum + item.saldo, 0));
    els.creditMetricCustomers.textContent = open.length;
    els.creditMetricLate.textContent = currency.format(late.reduce((sum, item) => sum + item.saldo, 0));
    els.creditMetricPaid.textContent = currency.format(all.reduce((sum, item) => sum + item.totalPago, 0));

    const filter = els.creditFilter.value;
    const term = normalize(els.creditSearch.value);
    const rows = (filter === "late" ? late : filter === "open" ? open : all)
        .filter((item) => !term || normalize(`${item.nome} ${item.telefone || ""}`).includes(term));

    els.creditCount.textContent = `${rows.length} ${rows.length === 1 ? "cliente" : "clientes"}`;
    els.creditTable.innerHTML = rows.length
        ? rows.map((item) => {
            const cobranca = `Oi ${firstName(item.nome)}, tudo bem? Aqui é da Nana Modas. Passando pra lembrar do seu saldo de ${currency.format(item.saldo)} com a gente${item.vencimentoEm ? ` (vencimento ${formatShortDate(item.vencimentoEm)})` : ""}. Qualquer dúvida é só chamar!`;
            return `
                <tr class="${item.clienteId === state.creditSelectedId ? "is-selected" : ""}">
                    <td>
                        <strong>${escapeHtml(item.nome)}</strong>
                        ${item.telefone ? `<br><span class="panel-note">${escapeHtml(item.telefone)}</span>` : ""}
                    </td>
                    <td>
                        <strong>${currency.format(item.saldo)}</strong>
                        ${item.saldo < -0.004 ? '<br><span class="badge badge-info">Crédito da cliente</span>' : ""}
                    </td>
                    <td>
                        ${item.saldo > 0.004 && item.vencimentoEm ? formatShortDate(item.vencimentoEm) : '<span class="panel-note">-</span>'}
                        ${item.saldo > 0.004 && item.diasAtraso > 0 ? `<br><span class="badge badge-danger">${item.diasAtraso} dia${item.diasAtraso === 1 ? "" : "s"} em atraso</span>` : ""}
                        ${item.saldo <= 0.004 ? '<br><span class="badge badge-ok">Quitado</span>' : ""}
                    </td>
                    <td>${item.ultimaCompraEm ? formatShortDate(item.ultimaCompraEm) : "-"}</td>
                    <td>
                        <div class="table-actions">
                            <button class="button button-secondary" type="button" data-credit-open="${item.clienteId}">Extrato</button>
                            ${item.saldo > 0.004 ? whatsappButton(item.telefone, cobranca, "Cobrar") : ""}
                        </div>
                    </td>
                </tr>
            `;
        }).join("")
        : `<tr><td colspan="5"><div class="empty-state">${filter === "late" ? "Nenhuma cliente em atraso." : "Nenhuma cliente com fiado."}</div></td></tr>`;

    if (state.creditSelectedId && !all.some((item) => item.clienteId === state.creditSelectedId)) {
        state.creditSelectedId = null;
        state.creditStatement = null;
    }

    renderCreditStatement();
}

async function loadCreditStatement(clienteId) {
    try {
        state.creditSelectedId = clienteId;
        state.creditStatement = await api(`/fiado/${clienteId}`);
        renderCredit();
        els.creditStatementTitle.scrollIntoView({ behavior: "smooth", block: "nearest" });
    } catch (error) {
        showToast(error.message);
    }
}

function renderCreditStatement() {
    const statement = state.creditStatement;
    if (!statement) {
        els.creditStatementTitle.textContent = "Extrato";
        els.creditStatementNote.textContent = "Escolha uma cliente na lista";
        els.creditPaymentForm.classList.add("hidden");
        els.creditStatementList.innerHTML = "";
        return;
    }

    const resumo = statement.resumo;
    els.creditStatementTitle.textContent = resumo.nome;
    els.creditStatementNote.textContent = resumo.saldo > 0.004
        ? `Deve ${currency.format(resumo.saldo)}${resumo.vencimentoEm ? ` · vence ${formatShortDate(resumo.vencimentoEm)}` : ""}`
        : resumo.saldo < -0.004 ? `Tem crédito de ${currency.format(Math.abs(resumo.saldo))}` : "Tudo pago";
    els.creditPaymentForm.classList.toggle("hidden", resumo.saldo <= 0.004);
    els.creditPaymentValue.max = resumo.saldo > 0 ? resumo.saldo.toFixed(2) : "";

    els.creditStatementList.innerHTML = statement.lancamentos.length
        ? statement.lancamentos.map((item) => {
            const isPurchase = item.tipo === "Compra";
            return `
                <div class="list-item credit-entry">
                    <div>
                        <strong>${isPurchase ? "Compra" : `Pagamento · ${formatPayment(item.formaPagamento)}`} ${isPurchase ? "+" : "−"}${currency.format(Math.abs(item.valor))}</strong>
                        <span>${formatDate(item.data)} · ${escapeHtml(item.descricao)}${item.usuario ? ` · ${escapeHtml(item.usuario)}` : ""}</span>
                        <span>Saldo depois: ${currency.format(item.saldoApos)}${isPurchase && item.vencimentoEm ? ` · vence ${formatShortDate(item.vencimentoEm)}` : ""}</span>
                    </div>
                    <div class="table-actions">
                        ${isPurchase && item.vendaId ? `<button class="button button-ghost" type="button" data-credit-sale="${item.vendaId}">Ver</button>` : ""}
                        ${!isPurchase && isAdminUser() ? `<button class="button button-ghost" type="button" data-credit-reverse="${item.id}">Estornar</button>` : ""}
                    </div>
                </div>
            `;
        }).join("")
        : `<div class="empty-state">Sem lançamentos.</div>`;
}

async function submitCreditPayment(event) {
    event.preventDefault();
    const clienteId = state.creditSelectedId;
    if (!clienteId) {
        return;
    }

    try {
        state.creditStatement = await api(`/fiado/${clienteId}/recebimentos`, {
            method: "POST",
            body: JSON.stringify({
                valor: Number(els.creditPaymentValue.value || 0),
                formaPagamento: els.creditPaymentMethod.value,
                observacao: emptyToNull(els.creditPaymentNote.value)
            })
        });
        els.creditPaymentForm.reset();
        showToast("Pagamento registrado.");
        await refreshScoped(["credit", "cash", "customersSimple"]);
    } catch (error) {
        showToast(error.message);
    }
}

async function reverseCreditPayment(id) {
    if (!window.confirm("Estornar esse pagamento? O valor volta a ficar em aberto.")) {
        return;
    }

    try {
        state.creditStatement = await api(`/fiado/recebimentos/${id}`, { method: "DELETE" });
        showToast("Pagamento estornado.");
        await refreshScoped(["credit", "cash", "customersSimple"]);
    } catch (error) {
        showToast(error.message);
    }
}

// ---------- Clientes: aniversário, sumidas, edição ----------

function getBirthdayInfo(dateValue) {
    const [year, month, day] = dateValue.split("-").map(Number);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    let next = new Date(today.getFullYear(), month - 1, day);
    if (next < today) {
        next = new Date(today.getFullYear() + 1, month - 1, day);
    }

    return {
        daysUntil: Math.round((next - today) / 86400000),
        age: next.getFullYear() - year
    };
}

function renderCustomerCardExtras(customer) {
    const chips = [];
    if (customer.dataNascimento) {
        chips.push(`<span class="badge badge-muted">Aniversário ${formatBirthday(customer.dataNascimento)}</span>`);
    }

    if (Number(customer.saldoFiado) > 0.004) {
        chips.push(`<span class="badge badge-warn">Fiado ${currency.format(customer.saldoFiado)}</span>`);
    }

    const message = `Oi ${firstName(customer.nome)}, tudo bem? Aqui é da Nana Modas!`;
    return `
        <div class="customer-card-actions">
            <div>${chips.join(" ")}</div>
            <div class="table-actions">
                ${whatsappButton(customer.telefone, message)}
                <button class="button button-ghost" type="button" data-customer-edit="${customer.id}">Editar</button>
            </div>
        </div>
    `;
}

function renderCustomerRelationship() {
    if (!els.customerBirthdayList) {
        return;
    }

    const birthdays = state.customers
        .filter((customer) => customer.dataNascimento)
        .map((customer) => ({ customer, ...getBirthdayInfo(customer.dataNascimento) }))
        .filter((item) => item.daysUntil <= 30)
        .sort((a, b) => a.daysUntil - b.daysUntil);

    els.customerBirthdayList.innerHTML = birthdays.length
        ? birthdays.map(({ customer, daysUntil, age }) => {
            const message = `Feliz aniversário, ${firstName(customer.nome)}! A Nana Modas preparou um presente pra você: passa aqui na loja essa semana pra ganhar um desconto especial.`;
            return `
                <div class="list-item">
                    <div>
                        <strong>${escapeHtml(customer.nome)}</strong>
                        <span>${formatBirthday(customer.dataNascimento)} · ${daysUntil === 0 ? "hoje" : `em ${daysUntil} dia${daysUntil === 1 ? "" : "s"}`} · faz ${age} anos</span>
                    </div>
                    <div class="table-actions">
                        ${daysUntil === 0 ? '<span class="badge badge-info">Hoje</span>' : ""}
                        ${whatsappButton(customer.telefone, message, "Parabéns")}
                    </div>
                </div>
            `;
        }).join("")
        : `<div class="empty-state">Nenhum aniversário nos próximos 30 dias. Cadastre a data no cliente pra aparecer aqui.</div>`;

    const inactive = state.customers
        .filter((customer) => customer.ultimaCompraEm && daysBetween(customer.ultimaCompraEm) > INACTIVE_CUSTOMER_DAYS)
        .map((customer) => ({ customer, days: daysBetween(customer.ultimaCompraEm) }))
        .sort((a, b) => b.customer.totalGasto - a.customer.totalGasto)
        .slice(0, 30);

    els.customerInactiveList.innerHTML = inactive.length
        ? inactive.map(({ customer, days }) => {
            const message = `Oi ${firstName(customer.nome)}, tudo bem? Aqui é da Nana Modas! Chegou coisa nova na loja e lembrei de você. Quer que eu te mande umas fotos?`;
            return `
                <div class="list-item">
                    <div>
                        <strong>${escapeHtml(customer.nome)}</strong>
                        <span>Última compra há ${days} dias · já gastou ${currency.format(customer.totalGasto || 0)}</span>
                    </div>
                    ${whatsappButton(customer.telefone, message, "Chamar")}
                </div>
            `;
        }).join("")
        : `<div class="empty-state">Nenhuma cliente sumida. Bom sinal!</div>`;
}

function editCustomer(id) {
    const customer = state.customers.find((item) => item.id === id);
    if (!customer) {
        return;
    }

    els.customerIdInput.value = customer.id;
    els.customerNameInput.value = customer.nome;
    els.customerPhoneInput.value = customer.telefone || "";
    els.customerEmailInput.value = customer.email || "";
    els.customerBirthInput.value = customer.dataNascimento || "";
    els.customerFormTitle.textContent = "Editar cliente";
    els.customerSubmitButton.textContent = "Salvar cliente";
    els.cancelCustomerEditButton.classList.remove("hidden");
    els.customerNameInput.focus();
    els.customerNameInput.scrollIntoView({ behavior: "smooth", block: "center" });
}

function resetCustomerForm() {
    els.customerForm.reset();
    els.customerIdInput.value = "";
    els.customerFormTitle.textContent = "Novo cliente";
    els.customerSubmitButton.textContent = "Adicionar cliente";
    els.cancelCustomerEditButton.classList.add("hidden");
}

// ---------- Contagem de estoque ----------

function readStockCounts() {
    const saved = readStorage(STOCK_COUNT_STORAGE_KEY, {});
    return saved && typeof saved === "object" ? saved : {};
}

function writeStockCounts(counts) {
    writeStorage(STOCK_COUNT_STORAGE_KEY, counts);
}

function stockCountKey(productId, variation) {
    return [productId, variation?.tamanho || "", variation?.cor || "", variation?.modelo || ""].join("|");
}

function getStockCountLines() {
    const categories = new Map(state.categories.map((category) => [category.id, category]));
    return state.products
        .filter((product) => product.ativo)
        .flatMap((product) => {
            const category = categories.get(product.categoriaId);
            const categoryIds = [product.categoriaId, category?.categoriaPaiId].filter(Boolean);
            if (product.variacoesEstoque?.length) {
                return product.variacoesEstoque.map((variation) => ({
                    key: stockCountKey(product.id, variation),
                    product,
                    categoryIds,
                    variation,
                    variationLabel: formatPdvVariation(variation),
                    code: variation.sku || product.sku || "",
                    system: Number(variation.quantidade || 0)
                }));
            }

            return [{
                key: stockCountKey(product.id, null),
                product,
                categoryIds,
                variation: null,
                variationLabel: "",
                code: product.sku || "",
                system: Number(product.quantidadeEmEstoque || 0)
            }];
        })
        .sort((a, b) => a.product.nome.localeCompare(b.product.nome, "pt-BR") || a.variationLabel.localeCompare(b.variationLabel, "pt-BR"));
}

function renderStockCount() {
    if (!els.stockCountTable) {
        return;
    }

    const counts = readStockCounts();
    const countedTotal = Object.keys(counts).length;
    els.stockCountToggle.textContent = els.stockCountBody.classList.contains("hidden")
        ? (countedTotal ? `Continuar contagem (${countedTotal})` : "Começar contagem")
        : "Esconder";

    const selectedCategory = els.stockCountCategory.value;
    els.stockCountCategory.innerHTML = [`<option value="">Todas as categorias</option>`]
        .concat(state.categories.map((category) => `<option value="${category.id}">${escapeHtml(category.nome)}</option>`))
        .join("");
    els.stockCountCategory.value = state.categories.some((category) => category.id === selectedCategory) ? selectedCategory : "";

    if (els.stockCountBody.classList.contains("hidden")) {
        return;
    }

    const term = normalize(els.stockCountSearch.value);
    const onlyDiff = els.stockCountOnlyDiff.checked;
    const lines = getStockCountLines().filter((line) => {
        if (els.stockCountCategory.value && !line.categoryIds.includes(els.stockCountCategory.value)) {
            return false;
        }

        if (term && !normalize(`${line.product.nome} ${line.variationLabel} ${line.code}`).includes(term)) {
            return false;
        }

        return !onlyDiff || (line.key in counts && Number(counts[line.key]) !== line.system);
    });

    els.stockCountTable.innerHTML = lines.length
        ? lines.map((line) => {
            const counted = line.key in counts ? counts[line.key] : "";
            return `
                <tr>
                    <td><strong>${escapeHtml(line.product.nome)}</strong></td>
                    <td>${line.variationLabel ? escapeHtml(line.variationLabel) : '<span class="panel-note">-</span>'}</td>
                    <td><span class="panel-note">${escapeHtml(line.code || "-")}</span></td>
                    <td>${line.system}</td>
                    <td><input class="stock-count-input" type="number" min="0" step="1" inputmode="numeric" value="${counted}" data-count-key="${escapeHtml(line.key)}" aria-label="Contado ${escapeHtml(line.product.nome)}"></td>
                    <td data-count-diff="${escapeHtml(line.key)}">${formatStockCountDiff(counted, line.system)}</td>
                </tr>
            `;
        }).join("")
        : `<tr><td colspan="6"><div class="empty-state">Nenhum produto nesse filtro.</div></td></tr>`;

    renderStockCountSummary();
}

function formatStockCountDiff(counted, system) {
    if (counted === "" || counted === undefined || counted === null) {
        return '<span class="panel-note">-</span>';
    }

    const diff = Number(counted) - system;
    if (diff === 0) {
        return '<span class="badge badge-ok">Ok</span>';
    }

    return diff > 0
        ? `<span class="badge badge-info">+${diff}</span>`
        : `<span class="badge badge-danger">${diff}</span>`;
}

function renderStockCountSummary() {
    const counts = readStockCounts();
    const lines = getStockCountLines().filter((line) => line.key in counts);
    const withDiff = lines.filter((line) => Number(counts[line.key]) !== line.system);
    const plus = withDiff.reduce((sum, line) => sum + Math.max(0, Number(counts[line.key]) - line.system), 0);
    const minus = withDiff.reduce((sum, line) => sum + Math.max(0, line.system - Number(counts[line.key])), 0);
    els.stockCountSummary.textContent = lines.length
        ? `${lines.length} linha${lines.length === 1 ? "" : "s"} contada${lines.length === 1 ? "" : "s"} · ${withDiff.length} com diferença (+${plus} / -${minus} peças)`
        : "Nenhuma linha contada.";
    els.stockCountApply.disabled = withDiff.length === 0;
}

function updateStockCountValue(key, value) {
    const counts = readStockCounts();
    if (value === "" || Number(value) < 0 || !Number.isFinite(Number(value))) {
        delete counts[key];
    } else {
        counts[key] = Math.floor(Number(value));
    }

    writeStockCounts(counts);
    const line = getStockCountLines().find((item) => item.key === key);
    const diffCell = els.stockCountTable.querySelector(`[data-count-diff="${CSS.escape(key)}"]`);
    if (line && diffCell) {
        diffCell.innerHTML = formatStockCountDiff(key in counts ? counts[key] : "", line.system);
    }

    renderStockCountSummary();
}

function scanStockCountCode(rawCode) {
    const code = normalize(String(rawCode || "").trim());
    if (!code) {
        return;
    }

    const line = getStockCountLines().find((item) => item.code && normalize(item.code) === code);
    if (!line) {
        showToast(`Código ${rawCode} não encontrado.`);
        return;
    }

    const counts = readStockCounts();
    counts[line.key] = Number(counts[line.key] || 0) + 1;
    writeStockCounts(counts);
    showToast(`${line.product.nome}${line.variationLabel ? ` (${line.variationLabel})` : ""}: ${counts[line.key]} contada${counts[line.key] === 1 ? "" : "s"}`);
    renderStockCount();
    els.stockCountScan.focus();
}

async function applyStockCount() {
    const counts = readStockCounts();
    const lines = getStockCountLines().filter((line) => line.key in counts);
    const withDiff = lines.filter((line) => Number(counts[line.key]) !== line.system);
    if (!withDiff.length) {
        showToast("Nenhuma diferença pra ajustar.");
        return;
    }

    if (!window.confirm(`Ajustar o estoque de ${withDiff.length} item(ns) pelo que foi contado? Linhas não contadas não mudam.`)) {
        return;
    }

    try {
        const result = await api("/estoque/contagem", {
            method: "POST",
            body: JSON.stringify({
                observacao: `Contagem ${new Date().toLocaleDateString("pt-BR")}`,
                itens: lines.map((line) => ({
                    produtoId: line.product.id,
                    tamanho: line.variation?.tamanho || null,
                    cor: line.variation?.cor || null,
                    modelo: line.variation?.modelo || null,
                    quantidadeContada: Number(counts[line.key])
                }))
            })
        });
        writeStockCounts({});
        showToast(`Estoque ajustado: ${result.itensAjustados} item(ns), +${result.pecasSobrando} / -${result.pecasFaltando} peças.`);
        await refreshScoped(["products", "movements"]);
    } catch (error) {
        showToast(error.message);
    }
}
