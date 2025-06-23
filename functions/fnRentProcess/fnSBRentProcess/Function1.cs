using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using fnSBRentProcess.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fnSBRentProcess
{
    public class ProcessaLocacao
    {
        private readonly ILogger<ProcessaLocacao> _logger;
        private readonly IConfiguration _config;
        public ProcessaLocacao(ILogger<ProcessaLocacao> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [Function(nameof(ProcessaLocacao))]
        public async Task Run(
            [ServiceBusTrigger("fila-locacao-auto", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {

            _logger.LogInformation("Message ID: {id}", message.MessageId);
            var body = message.Body.ToString();
            _logger.LogInformation("Message Body: {body}", body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);
            _logger.LogInformation($"Mensagem recebida: {message}");

            RentModel locacao = null;
            try
            {
                locacao = JsonSerializer.Deserialize<RentModel>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (locacao is null)
                {
                    _logger.LogWarning("Mensagem mal formatada.");
                    await messageActions.DeadLetterMessageAsync(message, null, "Formato Inv�lido");
                    return;
                }

                var connectionString = _config["SqlConnectionString"];

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var comando = new SqlCommand(@"INSERT INTO Locacao (Nome, Email, Modelo, Ano, TempoAluguel, Data)
                                                            VALUES (@nome, @email, @modelo, @ano, @tempo, @data)", connection);

                comando.Parameters.AddWithValue("@nome", locacao.nome);
                comando.Parameters.AddWithValue("@email", locacao.email);
                comando.Parameters.AddWithValue("@modelo", locacao.modelo);
                comando.Parameters.AddWithValue("@ano", locacao.ano);
                comando.Parameters.AddWithValue("@tempo", locacao.tempoAluguel);
                comando.Parameters.AddWithValue("@data", locacao.data);


                
                var ServiceBusConnetion = _configuration.GetSection("ServiceBusConnection").ValueToString();
                sendMessageToPay(serviceBusConnection, serviceBusQueue);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                connection.Close();

                // Complete the message
                await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar a mensagem: {message}", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, null, "Formato Inv�lido");
                return;
            }
        }
        private void sendMessageToPay(string serviceBusConnection, string serviceBusQueue)
        {

            ServiceBusClient serviceBusClient = new ServiceBusClient(serviceBusConnection);
            ServiceBusSender serviceBusSender = serviceBusClient.CreateSender(serviceBusQueue);
            ServiceBusMessage message = new ServiceBusMessage(JsonSerializer.Serialize(rentModel));
            message.ContentType = "application/json";
            message.ApplicationProperties.Add("Tipo", "Pagamento");
            message.ApplicationProperties.Add("Nome", rentModel.nome);
            message.ApplicationProperties.Add("Email", rentModel.email);
            message.ApplicationProperties.Add("Modelo", rentModel.modelo);
            message.ApplicationProperties.Add("Ano", rentModel.ano);    
            message.ApplicationProperties.Add("TempoAluguel", rentModel.tempoAluguel);
            message.ApplicationProperties.Add("Data", rentModel.data);
            
            serviceBusSender.SendMessageAsync(message).Wait();
            serviceBusSender.DisposeAsync();
            _logger.LogInformation("Enviando mensagem para o Service Bus Queue de pagamento...");
            // Aqui voc�� pode usar o ServiceBusClient e ServiceBusSender para enviar a mensagem.
        }
    }
}

