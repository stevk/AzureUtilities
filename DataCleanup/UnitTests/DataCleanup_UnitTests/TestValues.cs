using AzureUtilities.DataCleanup.Dto;
using System.Text.Json;

namespace AzureUtilities.DataCleanup.Shared
{
    public static class TestValues
    {
        // This connection string will parse, but is not valid.
        private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789012345678901234567890123==;EndpointSuffix=core.windows.net";

        public static DataCleanupParameters GetDataCleanupParameters(string domainTopicName = null, string nextPage = null)
        {
            return new DataCleanupParameters()
            {
                StorageConnectionString = ConnectionString,
                SubscriptionId = "SubscriptionId",
                ResourceGroupName = "ResourceGroupName",
                EventGridName = "EventGridName",
                ServicePrincipalClientId = "ServicePrincipalClientId",
                ServicePrincipalClientKey = "ServicePrincipalClientKey",
                ServicePrincipalTenantId = "ServicePrincipalTenantId",
                DomainTopicName = domainTopicName,
                DomainTopicNextpage = nextPage
            };
        }

        public static string GetQueueMessageData(string domainTopicName = null, string nextPage = null)
        {
            return JsonSerializer.Serialize(GetDataCleanupParameters(domainTopicName, nextPage), JsonOptions);
        }

        internal static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
}
