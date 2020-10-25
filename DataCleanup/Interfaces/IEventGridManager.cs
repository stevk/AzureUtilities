using Microsoft.Azure.Management.EventGrid;
using System.Collections.Generic;
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
            EventGridManagementClient eventGridManagementClient,
            string resourceGroupName,
            string domainName,
            string domainTopicName);

        Task<(List<string> domainTopicNames, string nextPageLink)> GetDomainTopics(
            EventGridManagementClient eventGridManagementClient,
            string resourceGroupName,
            string domainName,
            string nextPageLink = null);
    }
}
