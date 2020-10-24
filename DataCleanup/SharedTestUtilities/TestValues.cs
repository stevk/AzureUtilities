using System.Collections.Generic;
using System.Text.Json;
using AzureUtilities.DataCleanup.Dto;

namespace AzureUtilities.DataCleanup.Shared
{
    public static class TestValues
    {
        // This connection string will parse, but is not valid.
        private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789012345678901234567890123==;EndpointSuffix=core.windows.net";

        public static DataCleanupParameters GetDataCleanupParameters(string nextPage = null)
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
                DomainTopicName = "DomainTopicName",
                DomainTopicNextpage = nextPage
            };
        }

        public static string GetQueueMessageData(string nextPage = null)
        {
            return JsonSerializer.Serialize(GetDataCleanupParameters(nextPage), JsonOptions);
        }

        internal static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static IEnumerable<object[]> GetTestValues()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { " " };
            yield return new object[] { null };
        }
    }
}