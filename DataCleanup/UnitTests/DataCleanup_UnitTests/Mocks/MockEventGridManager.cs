using Azure;
using AzureUtilities.DataCleanup.Interfaces;
using Microsoft.Azure.Management.EventGrid;
using Microsoft.Rest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataCleanup_UnitTests.Mocks
{
    public class MockEventGridManager : IEventGridManager
    {
        public bool ThrowExceptionOnGetClient = false;
        public bool ThrowExceptionOnGetTopics = false;
        public bool ThrowExceptionOnDelete = false;
        public string ExceptionMessage = "Expected Exception Thrown";
        public int TopicNameIndex = 0;
        public List<string> DeletedDomainTopics = new List<string>();
        public List<List<string>> ReturnDomainTopicNames = new List<List<string>>()
        {
            new List<string>()
            {
                "domaintopic1",
                "domaintopic2",
                "domaintopic3"
            }
        };

        public Task DeleteDomainTopic(
            EventGridManagementClient eventGridManagementClient,
            string resourceGroupName,
            string domainName,
            string domainTopicName)
        {
            if (ThrowExceptionOnDelete)
            {
                throw new RequestFailedException(ExceptionMessage);
            }

            DeletedDomainTopics.Add(domainTopicName);

            return Task.CompletedTask;
        }

        public Task<(List<string> domainTopicNames, string nextPageLink)> GetDomainTopics(
            EventGridManagementClient eventGridManagementClient,
            string resourceGroupName,
            string domainName,
            string nextPageLink)
        {
            if (ThrowExceptionOnGetTopics)
            {
                throw new RequestFailedException(ExceptionMessage);
            }

            TopicNameIndex++;
            string returnNextPageLink = null;
            if (ReturnDomainTopicNames.Count > TopicNameIndex)
            {
                returnNextPageLink = "Nextpage";
            }


            return Task.FromResult((ReturnDomainTopicNames[TopicNameIndex - 1], returnNextPageLink));
        }

        public Task<EventGridManagementClient> GetEventGridManagementClient(
            string azureSubscriptionId,
            string azureServicePrincipalClientId,
            string azureServicePrincipalClientKey,
            string azureAuthenticationAuthority,
            string azureManagemernResourceUrl)
        {
            if (ThrowExceptionOnGetClient)
            {
                throw new RequestFailedException(ExceptionMessage);
            }

            return Task.FromResult(new EventGridManagementClient(new TokenCredentials("token")));
        }
    }
}
