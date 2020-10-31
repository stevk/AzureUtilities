using DataCleanup_IntegrationTests.Dto;
using DataCleanup_IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DataCleanup_IntegrationTests
{
    public class DeleteTables_Tests : IClassFixture<ClientFixture>
    {
        // Setup for runing intergration tests locally by default. 
        // Values can be provided in the release pipeline for testing against the deployed function app.
        // There is only one storage account when running locally, so these tests need to be designed to handle that.
        private readonly string _baseEndpoint = Environment.GetEnvironmentVariable("BaseEndpoint") ??
            "http://localhost:7071/api/";
        private readonly string StorageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString") ??
            "UseDevelopmentStorage=true";
        private readonly string _UrlDeleteTables;

        private readonly ClientFixture _clientFixture;

        public DeleteTables_Tests(ClientFixture clientFixture)
        {
            _clientFixture = clientFixture;
            _UrlDeleteTables = $"{_baseEndpoint}DeleteTables";
        }

        private async Task<CloudTableClient> SetupTestTable()
        {
            // Create and verify the existance of a table. Pre-existing tables are ok to ignore.
            CloudTableClient tableClient = TableHelper.CreateCloudTableClient(StorageConnectionString);
            CloudTable table = tableClient.GetTableReference("testtable");
            await table.CreateIfNotExistsAsync();
            List<CloudTable> tableList = await TableHelper.GetTableListAsync(tableClient);
            tableList.Should().HaveCountGreaterOrEqualTo(1);
            return tableClient;
        }

        [Fact]
        public async Task DataCleanup_DeleteTables_ShouldWork()
        {
            // Setup
            CloudTableClient tableClient = await SetupTestTable();

            var parameters = new DataCleanupParameters()
            {
                StorageConnectionString = StorageConnectionString
            };
            var content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json");

            // Act
            HttpResponseMessage response = await _clientFixture.GetClient().PostAsync(_UrlDeleteTables, content);

            // Verify
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            IEnumerable<CloudTable> tableResult = tableClient.ListTables();
            tableResult.Should().HaveCount(0);
        }

        [Fact]
        public async Task DataCleanup_DeleteTables_NoTablesExist_ShouldWork()
        {
            // Setup
            CloudTableClient tableClient =TableHelper.CreateCloudTableClient(StorageConnectionString);

            var parameters = new DataCleanupParameters()
            {
                StorageConnectionString = StorageConnectionString
            };
            var content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json");

            // - Removes pre-existing tables
            IEnumerable<CloudTable> tableResult1 = tableClient.ListTables();
            if (tableResult1.Count() != 0)
            {
                HttpResponseMessage response1 = await _clientFixture.GetClient().PostAsync(_UrlDeleteTables, content);
                response1.StatusCode.Should().Be(HttpStatusCode.OK);
                tableResult1 = tableClient.ListTables();
                tableResult1.Should().HaveCount(0);
            }

            // Act
            HttpResponseMessage response2 = await _clientFixture.GetClient().PostAsync(_UrlDeleteTables, content);

            // Verify
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            IEnumerable<CloudTable> tableResult2 = tableClient.ListTables();
            tableResult2.Should().HaveCount(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task DataCleanup_DeleteTables_BadStorageConnectionString_ReturnsBadRequest(string connectionString)
        {
            // Setup
            CloudTableClient tableClient = await SetupTestTable();

            var parameters = new DataCleanupParameters()
            {
                StorageConnectionString = connectionString
            };
            var content = new StringContent(JsonSerializer.Serialize(parameters), Encoding.UTF8, "application/json");

            // Act
            HttpResponseMessage response = await _clientFixture.GetClient().PostAsync(_UrlDeleteTables, content);

            // Verify
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            IEnumerable<CloudTable> tableResult = tableClient.ListTables();
            tableResult.Should().HaveCountGreaterOrEqualTo(1);
        }
    }
}
