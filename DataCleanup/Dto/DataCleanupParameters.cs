using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureUtilities.DataCleanup.Dto
{
    public class DataCleanupParameters
    {
        [JsonPropertyName("storageConnectionString")]
        [Required]
        public string StorageConnectionString { get; set; }

        [JsonPropertyName("subscriptionId")]
        [Required]
        public string SubscriptionId { get; set; }

        [JsonPropertyName("resourceGroupName")]
        [Required]
        public string ResourceGroupName { get; set; }

        [JsonPropertyName("eventGridName")]
        [Required]
        public string EventGridName { get; set; }

        [JsonPropertyName("servicePrincipalClientId")]
        [Required]
        public string ServicePrincipalClientId { get; set; }

        [JsonPropertyName("servicePrincipalClientKey")]
        [Required]
        public string ServicePrincipalClientKey { get; set; }

        [JsonPropertyName("servicePrincipalTenantId")]
        [Required]
        public string ServicePrincipalTenantId { get; set; }

        [JsonPropertyName("domainTopicName")]
        public string DomainTopicName { get; set; }

        [JsonPropertyName("domainTopicnNextPage")]
        public string DomainTopicNextpage { get; set; }
    }
}
