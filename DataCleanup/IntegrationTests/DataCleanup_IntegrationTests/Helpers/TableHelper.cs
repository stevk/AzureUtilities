using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCleanup_IntegrationTests.Helpers
{
    public static class TableHelper
    {
        public static CloudTableClient CreateCloudTableClient(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            return storageAccount.CreateCloudTableClient();
        }

        public static async Task<List<CloudTable>> GetTableListAsync(CloudTableClient tableClient)
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
