using AzureUtilities.DataCleanup.Interfaces;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.DataLayer
{
    public class TableManager : ITableManager
    {
        public CloudTableClient CreateCloudTableClient(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            return storageAccount.CreateCloudTableClient();
        }

        public Task DeleteIfExists(CloudTable table)
        {
            return table.DeleteIfExistsAsync();
        }

        public async Task<List<CloudTable>> GetTableListAsync(CloudTableClient tableClient)
        {
            TableContinuationToken token = null;
            var cloudTableList = new List<CloudTable>();

            do
            {
                TableResultSegment segment = await tableClient.ListTablesSegmentedAsync(token);
                token = segment.ContinuationToken;
                cloudTableList.AddRange(segment.Results);
            }
            while (token != null);

            return cloudTableList;
        }
    }
}
