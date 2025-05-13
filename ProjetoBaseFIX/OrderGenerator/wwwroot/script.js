let websocketConnection = null;

function enviaOrdem(tipo) {
    let papel = document.getElementById("papel").value;
    let preco = parseFloat(document.getElementById("preco").value.replace(/\./g, '').replace(',', '.'));
    let quantidade = parseInt(document.getElementById("quantidade").value, 10);

    if (papel == '') {
        showAlert("Preencha o papel", "warning");
        return;
    }


    if (isNaN(preco) || preco < 0.01 || preco > 1000) {
        showAlert("Preço deve ser entre 0,01 e 1.000,00.", "warning");
        return;
    }
    if (isNaN(quantidade) || quantidade < 1 || quantidade > 100000 || quantidade % 1 != 0) {
        showAlert("Quantidade deve ser entre 1 e 100.000 e deve ser numero inteiro.", "warning");
        return;
    }

    if (websocketConnection !== null && websocketConnection.readyState === websocketConnection.OPEN) {
        websocketConnection.send(JSON.stringify({
            Symbol: papel.toUpperCase(),
            OrderQty: parseInt(quantidade),
            Price: parseFloat(preco),
            Side: tipo
        }));
    } else {
        showAlert("Você está sem conexão com a contraparte", "error");
    }
}

function formataValor(valor) {
    let value = valor.replace(/\D/g, '');
    value = value.padStart(3, '0');
    let floatValue = (parseInt(value, 10) / 100);
    floatValue = Math.round(floatValue * 100) / 100;
    let formatted = floatValue.toFixed(2).replace('.', ',');
    return formatted.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
}

function calculaValor() {
    let papel = document.getElementById("papel").value;

    try {
        if (papel != '') {
            let preco = parseFloat(document.getElementById("preco").value.replace(/\./g, '').replace(',', '.'));
            let quantidade = parseInt(document.getElementById("quantidade").value, 10);

            if (!isNaN(preco) && !isNaN(quantidade)) {
                document.getElementById("valor").innerText = formataValor((quantidade * preco).toFixed(2));
                return;
            }
        }
    } catch (e) { }
    document.getElementById("valor").innerText = "0.00";
}

function showAlert(message, type, duration = 5_000) {
    let alertContainer = document.getElementById("alert-container");

    let alert = document.createElement("div");
    alert.innerText = message;
    alert.className = "alert";


    switch (type) {
        case "success":
            alert.style.backgroundColor = "#4CAF50";
            break;
        case "warning":
            alert.style.backgroundColor = "#efba18";
            break;
        case "error":
            alert.style.backgroundColor = "#F44336";
            break;
        default:
            alert.style.backgroundColor = "#333";
    }

    alertContainer.appendChild(alert);

    setTimeout(() => {
        alert.style.opacity = "0";
        setTimeout(() => alert.remove(), 300);
    }, duration);
}

document.addEventListener('DOMContentLoaded', function () {
    setInterval(() => {
        if (websocketConnection === null || websocketConnection.readyState !== websocketConnection.OPEN) {
            websocketConnection = new WebSocket("ws://localhost:8081/ws");
            websocketConnection.onopen = () => {
                console.log("conectado");
            };
            websocketConnection.onclose = (e) => {
                console.error("desconectado", e);
            }

            websocketConnection.onerror = (e) => {
                console.error("Erro no webSocket", e);
            }

            websocketConnection.onmessage = (messageReceived) => {
                const message = JSON.parse(messageReceived.data);

                if (message.IsAccepted) {
                    showAlert(message.Message, "success");
                } else {
                    showAlert("Ordem rejeitada, motivo:" + message.Message, "warning");
                }
            }
        }
    }, 1000);

    document.getElementById('preco').addEventListener('input', function (e) {
        let value = e.target.value.replace(/\D/g, '');
        e.target.value = formataValor(value);
    });
});