using AzureUtilities.DataCleanup.Interfaces;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCleanup_UnitTests.Mocks
{
    public class MockTableManager : ITableManager
    {
        public CloudTableClient CreateCloudTableClient(string storageConnectionString)
        {
            throw new NotImplementedException();
        }

        public Task DeleteIfExists(CloudTable table)
        {
            throw new NotImplementedException();
        }

        public Task<List<CloudTable>> GetTableListAsync(CloudTableClient tableClient)
        {
            throw new NotImplementedException();
        }
    }
}
