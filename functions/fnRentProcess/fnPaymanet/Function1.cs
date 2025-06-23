using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
namespace fnPaymanet
{
    public class Paymanet
    {
        private readonly ILogger<Paymanet> _logger;
        private readonly IConfiguration _configuration;
        private readonly string[] StatusList = { "Aprovado", "Reprovado", "Em análise" };
        private readonly Random random = new Random();
        public Paymanet(ILogger<Paymanet> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function(nameof(Paymanet))]
        [CosmosDBOutput("%CosmosDb%", "%CosmosContainerOut%", Connection = "CosmosDBConnection", CreateIfNotExists = true)]
        public async Task<object?> Run(
            [ServiceBusTrigger("payment-queue", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            PaymanetModel paymanet = null;
            try
            {
                paymanet = JsonSerializer.Deserialize<PaymanetModel>(message.Body.ToString(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (paymanet == null)
                {
                    await messageActions.DeadLetterMessageAsync(message, null, "Mensagem mal formatada");
                }
                int index = random.Next(StatusList.Length);
                string status = StatusList[index];
                paymanet.status = status;

                if(status == "Aprovado")
                {
                    paymanet.dataAprovacao = DateTime.UtcNow;
                    await sendToNotificationQueue(paymanet);
                }
                
                return paymanet;
                
            }
            catch (Exception)
            {
                await messageActions.DeadLetterMessageAsync(message, null, "Erro ao processar pagamento.");
                return null;
            }

            finally
            {
                await messageActions.CompleteMessageAsync(message);
            }
        }

        private async Task sendToNotificationQueue(PaymanetModel paymanet)
        {
            var connection = _configuration.GetSection("ServiceBusConnection").Value.ToString();
            var queue = _configuration.GetSection("NotificationQueue").Value.ToString();

            // Enviar mensagem para a fila de notificação
            var serviceBusClient = new ServiceBusClient(connection);
            var sender = serviceBusClient.CreateSender(queue);
            var message = new ServiceBusMessage(JsonSerializer.Serialize(paymanet));
            message.ApplicationProperties["type"] = "notification";
            message.ApplicationProperties["message"] = "Pagamento aprovado com sucesso";
            message.ApplicationProperties["timestamp"] = DateTime.UtcNow.ToString("o");
            await sender.SendMessageAsync(message);
        }
    }
}
