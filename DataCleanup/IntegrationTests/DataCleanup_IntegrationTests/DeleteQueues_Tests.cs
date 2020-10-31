using Azure.Storage.Queues;
using DataCleanup_IntegrationTests.Dto;
using DataCleanup_IntegrationTests.Helpers;
using FluentAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DataCleanup_IntegrationTests
{
    public class DeleteQueues_Tests : IClassFixture<ClientFixture>
    {
        // Setup for runing intergration tests locally by default. 
        // Values can be provided in the release pipeline for testing against the deployed function app.
        // There is only one storage account when running locally, so these tests need to be designed to handle that.
        private readonly string _baseEndpoint = Environment.GetEnvironmentVariable("BaseEndpoint") ??
            "http://localhost:7071/api/";
        private readonly string StorageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString") ??
            "UseDevelopmentStorage=true";
        private readonly string _UrlDeleteQueues;

        private readonly ClientFixture _clientFixture;

        public DeleteQueues_Tests(ClientFixture clientFixture)
        {
            _clientFixture = clientFixture;
            _UrlDeleteQueues = $"{_baseEndpoint}DeleteQueues";
        }

        private async Task<QueueClient> SetupTestQueue()
        {
            // Create and verify the existance of a queue. Pre-existing queues are ok to ignore.
            // Note that deleting domains does generate queues, so those tests can be negatively impacted if run at the same time.
            var queueHelper = new QueueHelper(StorageConnectionString);
            QueueClient queueClient = await queueHelper.CreateQueue("testqueue");
            await QueueHelper.CheckIfQueueExistsWithRetry(queueClient);
            return queueClient;
        }

        [Fact]
        public async Task DataCleanup_DeleteQueues_ShouldWork()
        {
            // Setup
            QueueClient queueClient = await SetupTestQueue();
            var queueHelper = new QueueHelper(StorageConnectionString);
            (await queueHelper.GetQueues()).Should().HaveCountGreaterOrEqualTo(1);

            var parameters = new DataCleanupParameters()
            {
                StorageConnectionString = StorageConnectionString
            };
            var content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json");

            // Act
            HttpResponseMessage response = await _clientFixture.GetClient().PostAsync(_UrlDeleteQueues, content);

            // Verify
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            bool queueExists = await queueClient.ExistsAsync();
            queueExists.Should().BeFalse();
            (await queueHelper.GetQueues()).Should().HaveCount(0);
        }

        [Fact]
        public async Task DataCleanup_DeleteQueues_NoQueuesExist_ShouldWork()
        {
            // Setup
            var parameters = new DataCleanupParameters()
            {
                StorageConnectionString = StorageConnectionString
            };
            var content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json");

            // - Removes pre-existing tables
            HttpResponseMessage response = await _clientFixture.GetClient().PostAsync(_UrlDeleteQueues, content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var queueHelper = new QueueHelper(StorageConnectionString);
            (await queueHelper.GetQueues()).Should().HaveCount(0, "Did not expect queues to exist after first delete");

            // Act
            HttpResponseMessage response2 = await _clientFixture.GetClient().PostAsync(_UrlDeleteQueues, content);

            // Verify
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            (await queueHelper.GetQueues()).Should().HaveCount(0, "Did not expect queues to exist after second delete");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task DataCleanup_DeleteQueues_BadStorageConnectionString_ReturnsBadRequest(string connectionString)
        {
            // Setup
            QueueClient queueClient = await SetupTestQueue();

            var parameters = new DataCleanupParameters()
            {
                StorageConnectionString = connectionString
            };
            var content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json");

            // Act
            HttpResponseMessage response = await _clientFixture.GetClient().PostAsync(_UrlDeleteQueues, content);

            // Verify
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            bool queueExists = await queueClient.ExistsAsync();
            queueExists.Should().BeTrue();
        }
    }
}
