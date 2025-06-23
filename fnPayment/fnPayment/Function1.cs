using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fnPayment
{
    public class Payment
    {
        private readonly ILogger<Payment> _logger;
        private readonly IConfiguration _configuration;
        private readonly string[] StatusList = { "Aprovado", "Reprovado", "Em análise" };
        private readonly Random random = new Random();

        public Payment(ILogger<Payment> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function(nameof(Payment))]
        [CosmosDBOutput("%CosmosDb%", "%CosmosContainer%", Connection = "CosmosDBConnection", CreateIfNotExists = true)]
        public async Task<object?> Run(
            [ServiceBusTrigger("payment-queue", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body.ToString());
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            PaymentModel payment = null;

            try
            {
                payment = JsonSerializer.Deserialize<PaymentModel>(message.Body.ToString(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payment == null)
                {
                    await messageActions.DeadLetterMessageAsync(message, null, "The message could not be deserialized.");
                    return null;
                }

                int index = random.Next(StatusList.Length);
                string status = StatusList[index];
                payment.Status = status;

                if (status == "Aprovado")
                {
                    payment.DataAprovacao = DateTime.Now;
                    await SendToNotificationQueue(payment);
                }
                else
                {
                    payment.DataAprovacao = null;
                }

                return payment;
            }
            catch (Exception ex)
            {
                await messageActions.DeadLetterMessageAsync(message, null, $"Erro: {ex.Message}");
                return null;
            }
        }

        private async Task SendToNotificationQueue(PaymentModel payment)
        {
            var connectionString = _configuration.GetValue<string>("ServiceBusConnection");
            var queueName = _configuration.GetValue<string>("ServiceBus");

            var serviceBusClient = new ServiceBusClient(connectionString);
            var sender = serviceBusClient.CreateSender(queueName);

            var message = new ServiceBusMessage(JsonSerializer.Serialize(payment))
            {
                ContentType = "application/json"
            };

            message.ApplicationProperties["idPayment"] = payment.IdPayment;
            message.ApplicationProperties["type"] = "notification";
            message.ApplicationProperties["message"] = "Pagamento aprovado com sucesso";

            try
            {
                await sender.SendMessageAsync(message);
                _logger.LogInformation("Message sent to notification queue: {id}", payment.IdPayment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to notification queue");
            }
            finally
            {
                await sender.DisposeAsync();
                await serviceBusClient.DisposeAsync();
            }
        }
    }

    public class PaymentModel
    {
        public string IdPayment { get; set; }
        public string Status { get; set; }
        public DateTime? DataAprovacao { get; set; }
    }
}
