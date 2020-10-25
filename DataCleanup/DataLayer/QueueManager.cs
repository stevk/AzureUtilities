using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using AzureUtilities.DataCleanup.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
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

        public Task CreateIfNotExistsAsync(QueueClient client)
        {
            return client.CreateIfNotExistsAsync();
        }

        public Task SendMessageAsync(QueueClient client, string message)
        {
            return client.SendMessageAsync(Base64Encode(message));
        }

        private static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
