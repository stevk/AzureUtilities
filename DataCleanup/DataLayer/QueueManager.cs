using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using AzureUtilities.DataCleanup.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.DataLayer
{
    public class QueueManager : IQueueManager
    {
        public QueueServiceClient CreateQueueServiceClient(string storageConnectionString)
        {
            return new QueueServiceClient(storageConnectionString);
        }

        public QueueClient CreateQueueClient(string storageConnectionString, string queueName)
        {
            return new QueueClient(storageConnectionString, queueName);
        }

        public async Task<List<string>> GetQueues(QueueServiceClient queueServiceClient)
        {
            IAsyncEnumerable<Page<QueueItem>> queuesAsyncEnumerable = queueServiceClient.GetQueuesAsync().AsPages();
            var result = new List<string>();
            await foreach (var page in queuesAsyncEnumerable)
            {
                foreach (var queueItem in page.Values)
                {
                    result.Add(queueItem.Name);
                }
            }
            return result;
        }

        public Task DeleteQueue(QueueServiceClient queueServiceClient, string queueName)
        {
            return queueServiceClient.DeleteQueueAsync(queueName);
        }
    }
}
