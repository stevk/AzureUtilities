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
    public class Functions
    {
        private readonly IServiceLayer _serviceLayer;

        private const string QueueNameToDelete = "domaintopicstodelete";
        private const string QueueNameToList = "domaintopicstolist";

        public Functions(IServiceLayer serviceLayer)
        {
            _serviceLayer = serviceLayer;

        }

        [FunctionName("InitializeCleanup")]
        public async Task<IActionResult> InitializeCleanup(
            [HttpTrigger(AuthorizationLevel.Function, "post")] DataCleanupParameters parameters)
        {
            Task queueDeleteTask = _serviceLayer.DeleteQueues(parameters);
            Task tableDeleteTask = DeleteTables(parameters);
            Task domainTopicQueryTask = _serviceLayer.PopulateDomainTopicQueue(parameters);

            await Task.WhenAll(queueDeleteTask, tableDeleteTask, domainTopicQueryTask);

            return new OkResult();
        }

        [FunctionName("DeleteQueues")]
        public async Task<IActionResult> DeleteQueues(
            [HttpTrigger(AuthorizationLevel.Function, "post")] DataCleanupParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.StorageConnectionString))
            {
                return new BadRequestResult();
            }

            await _serviceLayer.DeleteQueues(parameters);

            return new OkResult();
        }

        [FunctionName("DeleteTables")]
        public async Task<IActionResult> DeleteTables(
            [HttpTrigger(AuthorizationLevel.Function, "post")] DataCleanupParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.StorageConnectionString))
            {
                return new BadRequestResult();
            }
            
            await _serviceLayer.DeleteTables(parameters);

            return new OkResult();
        }

        [FunctionName("DomainTopicList")]
        [StorageAccount("AzureWebJobsStorage")]
        public async Task DomainTopicList([QueueTrigger(QueueNameToList)] string queueMessage)
        {
            DataCleanupParameters parameters = JsonSerializer.Deserialize<DataCleanupParameters>(queueMessage);

            await _serviceLayer.PopulateDomainTopicQueue(parameters);
        }

        [FunctionName("DomainTopicCleanup")]
        [StorageAccount("AzureWebJobsStorage")]
        public async Task DomainTopicCleanup([QueueTrigger(QueueNameToDelete)] string queueMessage)
        {
            DataCleanupParameters parameters = JsonSerializer.Deserialize<DataCleanupParameters>(queueMessage);

            if (string.IsNullOrWhiteSpace(parameters.DomainTopicName))
            {
                throw new ArgumentException($"{nameof(parameters.DomainTopicName)} value is invalid.");
            }

            await _serviceLayer.DeleteDomainTopic(parameters);
        }
    }
}
