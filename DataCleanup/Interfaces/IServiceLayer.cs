using AzureUtilities.DataCleanup.Dto;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.Interfaces
{
    public interface IServiceLayer
    {
        Task DeleteQueues(DataCleanupParameters parameters);

        Task DeleteTables(DataCleanupParameters parameters);

        Task DeleteDomainTopic(DataCleanupParameters parameters);

        Task PopulateDomainTopicQueue(DataCleanupParameters parameters);
    }
}
