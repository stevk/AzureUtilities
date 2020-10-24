using AzureUtilities.DataCleanup.Interfaces;
using Microsoft.Azure.Management.EventGrid;
using Microsoft.Azure.Management.EventGrid.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.DataLayer
{
    public class EventGridManager : IEventGridManager
    {
        public async Task<EventGridManagementClient> GetEventGridManagementClient(
            string azureSubscriptionId,
            string azureServicePrincipalClientId,
            string azureServicePrincipalClientKey,
            string azureAuthenticationAuthority,
            string azureManagemernResourceUrl)
        {
            var cc = new ClientCredential(azureServicePrincipalClientId, azureServicePrincipalClientKey);
            var context = new AuthenticationContext(azureAuthenticationAuthority);

            AuthenticationResult result = await context.AcquireTokenAsync(azureManagemernResourceUrl, cc).ConfigureAwait(false);

            var credential = new TokenCredentials(result.AccessToken);

            var eventGridManagementClient = new EventGridManagementClient(credential)
            {
                SubscriptionId = azureSubscriptionId,
                LongRunningOperationRetryTimeout = 50
            };

            return eventGridManagementClient;
        }

        public async Task DeleteDomainTopic(
            string resourceGroupName,
            string domainName,
            string domainTopicName,
            EventGridManagementClient eventGridManagementClient)
        {
            await eventGridManagementClient.DomainTopics.DeleteWithHttpMessagesAsync(resourceGroupName, domainName, domainTopicName);
            return;
        }

        public async Task<AzureOperationResponse<IPage<DomainTopic>>> GetDomainTopics(
        string resourceGroupName,
        string domainName,
        EventGridManagementClient eventGridManagementClient)
        {
            return await eventGridManagementClient.DomainTopics.ListByDomainWithHttpMessagesAsync(resourceGroupName, domainName);
        }

        public async Task<AzureOperationResponse<IPage<DomainTopic>>> GetDomainTopics(
            string nextPageLink,
            EventGridManagementClient eventGridManagementClient)
        {
            return await eventGridManagementClient.DomainTopics.ListByDomainNextWithHttpMessagesAsync(nextPageLink);
        }
    }
}
