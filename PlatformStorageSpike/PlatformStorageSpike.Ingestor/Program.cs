using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PlatformStorageSpike.Ingestor
{
    internal class Program
    {
        static async Task  Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("PlatformSpike_BlobContainerConnectionString"));

            var blobContainerClient = blobClient.GetBlobContainerClient("platform-spike-storage");
            await blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = blobContainerClient.GetBlobClient("test");

            var options = new BlobUploadOptions
            {
                Metadata = new Dictionary<string, string>()
                {
                    { "ContentType", "content-type" },
                    { "BodySize", "123"}
                }
            };

            await blob.UploadAsync(
                BinaryData.FromBytes(new byte[0]),
                options,
                CancellationToken.None
            ).ConfigureAwait(false);
        }
    }
}
