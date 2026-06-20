const form = document.querySelector("#loginForm");
const usuario = document.querySelector("#usuario");
const senha = document.querySelector("#senha");
const error = document.querySelector("#loginError");

form.addEventListener("submit", async (event) => {
    event.preventDefault();
    error.textContent = "";

    const button = form.querySelector("button");
    button.disabled = true;

    try {
        const response = await fetch("/auth/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                usuario: usuario.value.trim(),
                senha: senha.value
            })
        });

        if (!response.ok) {
            const payload = await response.json().catch(() => null);
            error.textContent = payload?.erro || "Usuário ou senha inválidos.";
            return;
        }

        window.location.href = "/";
    } catch {
        error.textContent = "Não foi possível entrar agora.";
    } finally {
        button.disabled = false;
    }
});
