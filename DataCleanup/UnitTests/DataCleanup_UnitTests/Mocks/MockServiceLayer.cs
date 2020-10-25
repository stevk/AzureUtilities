using AzureUtilities.DataCleanup.Dto;
using AzureUtilities.DataCleanup.Interfaces;
using System.Threading.Tasks;

namespace DataCleanup_UnitTests.Mocks
{
    public class MockServiceLayer : IServiceLayer
    {
        public bool DeleteDomainTopicCalled = false;
        public bool DeleteQueuesCalled = false;
        public bool DeleteTablesCalled = false;
        public bool PopulateDomainTopicQueueCalled = false;
        public DataCleanupParameters LastParametersPassed;

        public Task DeleteDomainTopic(DataCleanupParameters parameters)
        {
            DeleteDomainTopicCalled = true;
            LastParametersPassed = parameters;
            return Task.CompletedTask;
        }

        public Task DeleteQueues(DataCleanupParameters parameters)
        {
            DeleteQueuesCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteTables(DataCleanupParameters parameters)
        {
            DeleteTablesCalled = true;
            return Task.CompletedTask;
        }

        public Task PopulateDomainTopicQueue(DataCleanupParameters parameters)
        {
            PopulateDomainTopicQueueCalled = true;
            LastParametersPassed = parameters;
            return Task.CompletedTask;
        }
    }
}
