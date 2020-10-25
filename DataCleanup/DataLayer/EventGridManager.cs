using AzureUtilities.DataCleanup.Interfaces;
using Microsoft.Azure.Management.EventGrid;
using Microsoft.Azure.Management.EventGrid.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using System.Collections.Generic;
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
            EventGridManagementClient eventGridManagementClient,
            string resourceGroupName,
            string domainName,
            string domainTopicName)
        {
            await eventGridManagementClient.DomainTopics.DeleteWithHttpMessagesAsync(resourceGroupName, domainName, domainTopicName);
            return;
        }

        public async Task<(List<string> domainTopicNames, string nextPageLink)> GetDomainTopics(
            EventGridManagementClient eventGridManagementClient,
            string resourceGroupName,
            string domainName,
            string nextPageLink = null)
        {
            AzureOperationResponse<IPage<DomainTopic>> result;
            if (string.IsNullOrWhiteSpace(nextPageLink))
            {
                result = await eventGridManagementClient.DomainTopics.ListByDomainWithHttpMessagesAsync(resourceGroupName, domainName);
            }
            else
            {
                result = await eventGridManagementClient.DomainTopics.ListByDomainNextWithHttpMessagesAsync(nextPageLink);
            }

            var domainTopcicNames = new List<string>();
            foreach (DomainTopic domainTopic in result.Body)
            {
                domainTopcicNames.Add(domainTopic.Name);
            }

            return (domainTopcicNames, result.Body.NextPageLink);
        }
    }
}
