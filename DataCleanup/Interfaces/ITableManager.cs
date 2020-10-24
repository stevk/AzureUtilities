using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.Interfaces
{
    public interface ITableManager
    {
        CloudTableClient CreateCloudTableClient(string storageConnectionString);

        Task DeleteIfExists(CloudTable table);

        Task<List<CloudTable>> GetTableListAsync(CloudTableClient tableClient);
    }
}
