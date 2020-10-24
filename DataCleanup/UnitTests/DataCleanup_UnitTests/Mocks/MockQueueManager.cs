using Azure;
using Azure.Storage.Queues;
using AzureUtilities.DataCleanup.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCleanup_UnitTests.Mocks
{
    public class MockQueueManager : IQueueManager
    {
        public bool ThrowException = false;
        public string ExceptionMessage = "Expected Exception Thrown";
        public List<string> ReturnQueueList = new List<string>()
        {
            "queue1",
            "queue2",
            "queue3"
        };
        public List<string> DeletedQueues = new List<string>();

        public QueueClient CreateQueueClient(string storageConnectionString, string queueName)
        {
            throw new NotImplementedException();
        }

        public QueueServiceClient CreateQueueServiceClient(string storageConnectionString)
        {
            if (ThrowException)
            {
                throw new RequestFailedException(ExceptionMessage);
            }

            return new QueueServiceClient(storageConnectionString);
        }

        public Task DeleteQueue(QueueServiceClient queueServiceClient, string queueName)
        {
            if (ThrowException)
            {
                throw new RequestFailedException(ExceptionMessage);
            }

            DeletedQueues.Add(queueName);

            return Task.CompletedTask;
        }

        public Task<List<string>> GetQueues(QueueServiceClient queueServiceClient)
        {
            if (ThrowException)
            {
                throw new RequestFailedException(ExceptionMessage);
            }

            return Task.FromResult(ReturnQueueList);
        }
    }
}
