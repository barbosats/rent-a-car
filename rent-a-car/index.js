const express = require('express');
const cors = require('cors');
const { DefaultAzureCredential } = require('@azure/identity');
const { ServiceBusClient } = require('@azure/service-bus');
require('dotenv').config();
const app = express();
app.use(cors());
app.use(express.json());

app.post('/api/locacao', async (req, res) => {
    const { nome, email } = req.body;
    const connectionString = "Endpoint=sb://seu-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;Shared"
    const veiculo = {
        modelo: "Golf",
        ano: 2025,
        tempoAluguel: "1 semana",
    };
    const mensagem = {
        nome,
        email,
        modelo,
        ano,
        tempoAluguel,
        data: new Date().toISOString(),
    };

    try{
    const credential = new DefaultAzureCredential();
    const serviceBusConnectionString = connectionString;
    const queueName = "fila-locacao-auto";
    const sbClient = new ServiceBusClient(serviceBusConnectionString);
    const sender = sbClient.createSender(queueName);
    const message = {
        body: mensagem,
        contentType: 'application/json',
        label: "locacao"
    };  
    await sender.sendMessages(message);
    await sender.close();
    await sbClient.close();

    res.status(201).json({ message: 'Locação de Veículo enviada para fila com sucesso' });



    }catch (error){
      console.log(error);
      return res.status(500).json({ error: 'Erro ao enviar mensagem' });
    }
    });

    app.listen(3001,() => {
        console.log('Servidor rodando na porta 3001');
    });

    