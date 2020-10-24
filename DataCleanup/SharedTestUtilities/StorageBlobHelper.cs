using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzureUtilities.DataCleanup.Shared
{
    public class StorageBlobHelper
    {
        private static readonly string s_storageConnectionString = Environment.GetEnvironmentVariable("StorageAccountConnectionString") ?? throw new ArgumentException();

        public static BlobContainerClient GetBlobContainer(string blobContainerName)
        {
            var serviceClient = new BlobServiceClient(s_storageConnectionString);
            return serviceClient.GetBlobContainerClient(blobContainerName);
        }

        public async Task<List<string>> GetBlobFileList(BlobContainerClient container)
        {
            var blobFileList = new List<string>();
            await foreach (BlobItem blobItem in container.GetBlobsAsync())
            {
                blobFileList.Add(blobItem.Name);
            }

            return blobFileList;
        }

        public async Task<string> GetBlobFileAsString(BlobContainerClient container, string blobName)
        {
            BlobClient client = container.GetBlobClient(blobName);
            BlobDownloadInfo download = await client.DownloadAsync();

            using var streamReader = new StreamReader(download.Content);
            string result = await streamReader.ReadToEndAsync();

            return result;
        }
    }
}
