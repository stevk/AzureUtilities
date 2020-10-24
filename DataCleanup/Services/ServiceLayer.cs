using Azure.Storage.Queues;
using AzureUtilities.DataCleanup.Dto;
using AzureUtilities.DataCleanup.Interfaces;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.EventGrid;
using Microsoft.Azure.Management.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.Services
{
    public class ServiceLayer : IServiceLayer
    {
        private readonly ILogger _log;
        private readonly IEventGridManager _eventGridManager;
        private readonly IQueueManager _queueManager;
        private readonly ITableManager _tableManager;

        private const string QueueNameHoldingDomainTopicsToDelete = "domaintopicstodelete";
        private const string QueueNameHoldingDomainTopicsToList = "domaintopicstolist";

        public ServiceLayer(
            IEventGridManager eventGridManager,
            IQueueManager queueManager,
            ITableManager tableManager,
            ILogger<ServiceLayer> log)
        {
            _eventGridManager = eventGridManager;
            _queueManager = queueManager;
            _tableManager = tableManager;
            _log = log;
        }

        public async Task DeleteQueues(DataCleanupParameters parameters)
        {
            _log.LogDebug("Queue deletion starting.");

            try
            {
                QueueServiceClient queueServiceClient = _queueManager.CreateQueueServiceClient(parameters.StorageConnectionString);
                List<string> queues = await _queueManager.GetQueues(queueServiceClient);

                var queueDeleteTasks = new List<Task>();
                if (queues != null)
                {
                    foreach (var queue in queues)
                    {
                        queueDeleteTasks.Add(_queueManager.DeleteQueue(queueServiceClient, queue));
                    }
                    await Task.WhenAll(queueDeleteTasks);
                }

                _log.LogDebug($"Queue deletion completed! Removed {queueDeleteTasks.Count} queues.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception encountered in DeleteQueues method.");
                throw;
            }

            return;
        }

        public async Task DeleteTables(DataCleanupParameters parameters)
        {
            _log.LogDebug("Table deletion starting.");

            try
            {
                CloudTableClient tableClient = _tableManager.CreateCloudTableClient(parameters.StorageConnectionString);
                List<CloudTable> cloudTableList = await _tableManager.GetTableListAsync(tableClient);

                var tableDeleteTasks = new List<Task>();
                if (cloudTableList != null)
                {
                    foreach (CloudTable table in cloudTableList)
                    {
                        tableDeleteTasks.Add(_tableManager.DeleteIfExists(table));
                    }
                    await Task.WhenAll(tableDeleteTasks);
                }

                _log.LogDebug($"Table deletion completed! Removed {tableDeleteTasks.Count} tables.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception encountered in DeleteTables method.");
                throw;
            }

            return;
        }

        public async Task DeleteDomainTopic(DataCleanupParameters parameters)
        {
            EventGridManagementClient eventGridManagementClient = await _eventGridManager.GetEventGridManagementClient(
                parameters.SubscriptionId,
                parameters.ServicePrincipalClientId,
                parameters.ServicePrincipalClientKey,
                string.Concat(@"https://login.windows.net/", parameters.ServicePrincipalTenantId),
                @"https://management.azure.com/");

            _log.LogDebug($"Deleting domain topic {parameters.DomainTopicName}");

            try
            {
                await _eventGridManager.DeleteDomainTopic(
                        parameters.ResourceGroupName,
                        parameters.EventGridName,
                        parameters.DomainTopicName,
                        eventGridManagementClient);

                _log.LogDebug($"Domain topic deletion completed! {parameters.DomainTopicName}");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception encountered in DeleteDomainTopic method.");
                throw;
            }

            return;
        }

        public async Task PopulateDomainTopicQueue(DataCleanupParameters parameters)
        {
            EventGridManagementClient eventGridManagementClient = await _eventGridManager.GetEventGridManagementClient(
                parameters.SubscriptionId,
                parameters.ServicePrincipalClientId,
                parameters.ServicePrincipalClientKey,
                string.Concat(@"https://login.windows.net/", parameters.ServicePrincipalTenantId),
                @"https://management.azure.com/");

            try
            {
                string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? throw new ArgumentException();
                QueueClient queuePendingDelete = _queueManager.CreateQueueClient(storageConnectionString, QueueNameHoldingDomainTopicsToDelete);
                await queuePendingDelete.CreateIfNotExistsAsync();

                AzureOperationResponse<IPage<DomainTopic>> lastResult;
                if (parameters.DomainTopicNextpage == null)
                {
                    _log.LogDebug("Getting first page of domain topics.");
                    lastResult = await _eventGridManager.GetDomainTopics(
                        parameters.ResourceGroupName,
                        parameters.EventGridName,
                        eventGridManagementClient);
                }
                else
                {
                    _log.LogDebug("Getting next page of domain topics.");
                    lastResult = await _eventGridManager.GetDomainTopics(
                        parameters.DomainTopicNextpage,
                        eventGridManagementClient);
                }

                var queueAddTasks = new List<Task>();
                foreach (DomainTopic domainTopic in lastResult.Body)
                {
                    parameters.DomainTopicName = domainTopic.Name;
                    string deleteMessage = JsonSerializer.Serialize(parameters);
                    queueAddTasks.Add(queuePendingDelete.SendMessageAsync(Base64Encode(deleteMessage)));

                    _log.LogDebug($"Found {domainTopic.Name}");
                }

                _log.LogDebug($"{queueAddTasks.Count} domain topics being added to queue for deletion.");

                // If there is another page of domain topics, then add a task to the queue to parse it.
                if (lastResult.Body.NextPageLink != null)
                {
                    QueueClient queuePendingList = _queueManager.CreateQueueClient(storageConnectionString, QueueNameHoldingDomainTopicsToList);
                    await queuePendingList.CreateIfNotExistsAsync();
                    parameters.DomainTopicNextpage = lastResult.Body.NextPageLink;
                    parameters.DomainTopicName = null;
                    string nextPageParameters = JsonSerializer.Serialize(parameters);
                    queueAddTasks.Add(queuePendingList.SendMessageAsync(Base64Encode(nextPageParameters)));

                    _log.LogDebug($"Added next domain topic page to queue.");
                }

                await Task.WhenAll(queueAddTasks);

                _log.LogDebug($"Finished processing page of domain topics.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception encountered in PopulateDomainTopicQueue method.");
                throw;
            }

            return;
        }

        private static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
