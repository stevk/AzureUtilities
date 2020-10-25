using AzureUtilities.DataCleanup.Dto;
using AzureUtilities.DataCleanup.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup
{
    public class DataCleanup
    {
        private readonly IServiceLayer _serviceLayer;

        private const string QueueNameToDelete = "domaintopicstodelete";
        private const string QueueNameToList = "domaintopicstolist";

        public DataCleanup(IServiceLayer serviceLayer)
        {
            _serviceLayer = serviceLayer;

        }

        [FunctionName("InitializeCleanup")]
        public async Task<IActionResult> InitializeCleanup(
            [HttpTrigger(AuthorizationLevel.Function, "post")] DataCleanupParameters parameters)
        {
            Task queueDeleteTask = _serviceLayer.DeleteQueues(parameters);
            Task tableDeleteTask = _serviceLayer.DeleteTables(parameters);
            Task domainTopicQueryTask = _serviceLayer.PopulateDomainTopicQueue(parameters);

            await Task.WhenAll(queueDeleteTask, tableDeleteTask, domainTopicQueryTask);

            return new OkResult();
        }

        [FunctionName("DomainTopicList")]
        [StorageAccount("AzureWebJobsStorage")]
        public async Task DomainTopicList([QueueTrigger(QueueNameToList)] string queueMessage)
        {
            DataCleanupParameters parameters = JsonSerializer.Deserialize<DataCleanupParameters>(queueMessage);

            await _serviceLayer.PopulateDomainTopicQueue(parameters);

            return;
        }

        [FunctionName("DomainTopicCleanup")]
        [StorageAccount("AzureWebJobsStorage")]
        public async Task DomainTopicCleanup([QueueTrigger(QueueNameToDelete)] string queueMessage)
        {
            DataCleanupParameters parameters = JsonSerializer.Deserialize<DataCleanupParameters>(queueMessage);

            if (string.IsNullOrEmpty(parameters.DomainTopicName))
            {
                throw new ArgumentException("DomainTopicName value is null or empty.");
            }

            await _serviceLayer.DeleteDomainTopic(parameters);

            return;
        }
    }
}
