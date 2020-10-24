using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.Shared
{
    public class StorageQueueHelper
    {
        private static readonly string s_storageConnectionString = Environment.GetEnvironmentVariable("StorageAccountConnectionString") ?? throw new ArgumentException();

        public static async Task<QueueClient> CreateQueue(string queueName)
        {
            QueueClient queue = new QueueClient(s_storageConnectionString, queueName);
            await queue.CreateIfNotExistsAsync();
            return queue;
        }

        public static QueueClient GetQueueReference(string queueName)
        {
            return new QueueClient(s_storageConnectionString, queueName);
        }

        public async Task InsertMessageToQueue(string queueName, byte[] payload)
        {
            var message = Convert.ToBase64String(payload);
            await AddMessage(queueName, message);
        }

        public static async Task AddMessage(string queueName, string message)
        {
            QueueClient queue = await CreateQueue(queueName);
            await queue.SendMessageAsync(message);
        }

        public static async Task<QueueMessage[]> GetMessageFromQueue(QueueClient queue, TimeSpan visibilityTimeout = default)
        {
            if (visibilityTimeout == default)
            {
                return await queue.ReceiveMessagesAsync(1);
            }
            else
            {
                return await queue.ReceiveMessagesAsync(1, visibilityTimeout);
            }
        }

        public static async Task UpdateMessageFromQueue(QueueClient queue, QueueMessage message, TimeSpan visibilityTimeout)
        {
            await queue.UpdateMessageAsync(message.MessageId, message.PopReceipt, message.MessageText, visibilityTimeout);
        }

        public static async Task<bool> PeekMessageFromQueueWithRetry(QueueClient queue)
        {
            var queueMessageExistsFunc = new Func<Task<bool>>(async () =>
            {
                Response<PeekedMessage[]> peekResult = null;
                peekResult = await queue.PeekMessagesAsync();
                int statuscode = peekResult.GetRawResponse().Status;
                if (statuscode >= 300)
                {
                    throw new Exception($"Non-recoverable statuscode returned: {statuscode}");
                }

                return peekResult.Value.Length > 0;
            });

            return await CheckWithRetry(queueMessageExistsFunc, "Failed due to timeout waiting for a Message to appear in the Queue.");
        }

        public static async Task<bool> CheckIfQueueExistsWithRetry(QueueClient queue)
        {
            var queueExistsFunc = new Func<Task<bool>>(async () => await queue.ExistsAsync());

            return await CheckWithRetry(queueExistsFunc, "Failed due to timeout waiting for a Queue to be created.");
        }

        internal static async Task<bool> CheckWithRetry(Func<Task<bool>> check, string timeoutMessage)
        {
            bool ready = false;
            int delay = 300;
            int retriesRemaining = 15;

            while (!ready && retriesRemaining > 0)
            {
                ready = await check.Invoke();

                if (ready)
                {
                    return ready;
                }

                await Task.Delay(delay);
                if (delay < 15000)
                {
                    delay *= 2;
                }
                retriesRemaining--;
            }

            throw new TimeoutException(timeoutMessage);
        }
    }
}
