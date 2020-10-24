using AzureUtilities.DataCleanup.Interfaces;
using Microsoft.Azure.Management.EventGrid;
using Microsoft.Azure.Management.EventGrid.Models;
using Microsoft.Rest.Azure;
using System;
using System.Threading.Tasks;

namespace DataCleanup_UnitTests.Mocks
{
    public class MockEventGridManager : IEventGridManager
    {
        public Task DeleteDomainTopic(string resourceGroupName, string domainName, string domainTopicName, EventGridManagementClient eventGridManagementClient)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<IPage<DomainTopic>>> GetDomainTopics(string resourceGroupName, string domainName, EventGridManagementClient eventGridManagementClient)
        {
            throw new NotImplementedException();
        }

        public Task<AzureOperationResponse<IPage<DomainTopic>>> GetDomainTopics(string nextPageLink, EventGridManagementClient eventGridManagementClient)
        {
            throw new NotImplementedException();
        }

        public Task<EventGridManagementClient> GetEventGridManagementClient(string azureSubscriptionId, string azureServicePrincipalClientId, string azureServicePrincipalClientKey, string azureAuthenticationAuthority, string azureManagemernResourceUrl)
        {
            throw new NotImplementedException();
        }
    }
}
