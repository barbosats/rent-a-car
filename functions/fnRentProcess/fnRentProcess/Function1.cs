using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fnRentProcess
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
        public void Run([QueueTrigger("fila-locacao-auto", Connection = "ServiceBusConnection")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");
        }
    }
}
