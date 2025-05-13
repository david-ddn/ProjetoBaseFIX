# Projeto FIX Base
Projeto de exemplo de conectividade entre FIX Server e Client , sendo que o client tambem possui um servidor Websocket, por onde a interface Web se comunica para o envio de ordens.

A solução é constituída por 2 projetos principais:
1. OrderAccumulator: O serviço roda como um FIX Server(Acceptor) e processa as ordens e faz as validações, como limite operacional e dados do ativo.
2. OrderGenerator: Serviço realiza uma conectividade com o server através do FIX Client (Initiator), tambem levantando um WebSocket para a conectividade entre o client web e o servidor.

- A página web possui um formulário com os seguintes campos, com os quais será criada uma nova ordem (NewOrderSingle): 
1. Símbolo: escolhido entre PETR4, VALE3 ou VIIA4. 
2. Lado: escolhido entre Compra ou Venda. 
3. Quantidade: valor positivo inteiro menor que 100.000. 
4. Preço: valor positivo decimal múltiplo de 0.01 e menor que 1.000. 

- O fluxo completo deve serguir os segintes pontos:
1. No resultado para o client, apresentar a resposta da requisição. 
2. O OrderAccumulator recebe as ordens e calcula a exposição financeira por símbolo: Exposição financeira = somatório de (preço * quantidade executada) de cada ordem de compra - somatório de (preço * quantidade executada) de venda. Ou seja, as ordens de compra aumentam a exposição e as de venda diminuem a exposição.
3. O OrderAccumulator terá um limite interno constante, por símbolo, de R$ 100.000.000 (cem milhões), isso significa que qualquer ordem que venha a ultrapassar em valor absoluto o limite de exposição, deve ser respondida com uma rejeição, caso a ordem seja aceita, o OrderAccumulator deve responder com um ExecutionReport tendo ExecType = New e a ordem deve ser considerada no cálculo de exposição, caso a ordem seja rejeitada, o OrderAccumulator deve responder com um ExecutionReport tendo ExecType = Rejected e a ordem não deve ser considerada no cálculo de exposição.
