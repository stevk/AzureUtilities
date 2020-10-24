using Microsoft.Azure.Management.EventGrid;
using Microsoft.Azure.Management.EventGrid.Models;
using Microsoft.Rest.Azure;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.Interfaces
{
    public interface IEventGridManager
    {
        Task<EventGridManagementClient> GetEventGridManagementClient(
            string azureSubscriptionId,
            string azureServicePrincipalClientId,
            string azureServicePrincipalClientKey,
            string azureAuthenticationAuthority,
            string azureManagemernResourceUrl);

        Task DeleteDomainTopic(
            string resourceGroupName,
            string domainName,
            string domainTopicName,
            EventGridManagementClient eventGridManagementClient);

        Task<AzureOperationResponse<IPage<DomainTopic>>> GetDomainTopics(
        string resourceGroupName,
        string domainName,
        EventGridManagementClient eventGridManagementClient);

        Task<AzureOperationResponse<IPage<DomainTopic>>> GetDomainTopics(
            string nextPageLink,
            EventGridManagementClient eventGridManagementClient);
    }
}
