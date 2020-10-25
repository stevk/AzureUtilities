using Azure.Storage.Queues;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.Interfaces
{
    public interface IQueueManager
    {
        QueueServiceClient CreateQueueServiceClient(string storageConnectionString);

        QueueClient CreateQueueClient(string storageConnectionString, string queueName);

        Task<List<string>> GetQueues(QueueServiceClient queueServiceClient);

        Task DeleteQueue(QueueServiceClient queueServiceClient, string queueName);

        Task CreateIfNotExistsAsync(QueueClient client);

        Task SendMessageAsync(QueueClient client, string message);
    }
}
