using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PlatformStorageSpike.Ingestor
{
    internal class Program
    {
        // reguired env-variables: PlatformSpike_BlobContainerConnectionString => the storage account to store blobs
        //example: 10 SqlAzureAuditStore|AzureTableAuditStore {other storage specific options}
        static async Task Main(string[] args)
        {
            var numberOfConversations = int.Parse(args[0]);

            var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("PlatformSpike_BlobContainerConnectionString"));
            var blobContainerClient = blobClient.GetBlobContainerClient("platform-spike-storage");
            await blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            //Tomek and Andreas wanted to start with Audits we will need to tweak this for Errors
            var indexStore = (IAuditIndexStore)Activator.CreateInstance(Type.GetType(args[1], true));
            var isConfirmed = true; //This would be false when transport tx mode > receive only

            await indexStore!.Initalize(args);
            
            var dataGenerator = new TestDataGenerator(numberOfConversations, 10);

            foreach (var testData in dataGenerator.GetTestData())
            {
                await StoreMessageBodyHeaderAndMetaInBlob(blobContainerClient, testData, isConfirmed);
                await indexStore.IndexMetadata(testData.ProcessingMetadata);
            }
        }

        static async Task StoreMessageBodyHeaderAndMetaInBlob(BlobContainerClient blobContainerClient, TestData testData, bool isConfirmed)
        {
            await StoreBodyAndHeaders(blobContainerClient, testData.Body, testData.Headers);

            await StoreProcessingMetadata(blobContainerClient, testData.ProcessingMetadata, isConfirmed);

            if (!isConfirmed)
            {
                await MarkProcessingMetadataAsConfirmed(blobContainerClient, testData.Headers[MetadataKeys.ProcessingId]);
            }
        }

        static Task MarkProcessingMetadataAsConfirmed(BlobContainerClient blobContainerClient, string processingAttemptId)
        {
            throw new NotImplementedException();
        }

        static Task StoreBodyAndHeaders(BlobContainerClient blobContainerClient, ReadOnlyMemory<byte> body, IDictionary<string, string> headers)
        {
            var blob = blobContainerClient.GetBlobClient(headers[MetadataKeys.MessageId]);

            var options = new BlobUploadOptions
            {
                Metadata = headers.ToDictionary(kv => kv.Key.Replace('.', '_'), kv => kv.Value)
            };

            //NOTE: store headers in a separate blob if size is above 8KB

            return blob.UploadAsync(
                BinaryData.FromBytes(body),
                options,
                CancellationToken.None);
        }

        static Task StoreProcessingMetadata(BlobContainerClient blobContainerClient, IDictionary<string, string> processingMetadata, bool isConfirmed)
        {
            var blob = blobContainerClient.GetBlobClient(processingMetadata[MetadataKeys.ProcessingId]);  //TODO: do we need a prefix?

            var options = new BlobUploadOptions
            {
                Metadata = new Dictionary<string, string> { { "IsConfirmed", isConfirmed.ToString() } }
            };

            return blob.UploadAsync(
                BinaryData.FromBytes(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(processingMetadata)),
                options,
                CancellationToken.None);
        }
    }

    class TestData
    {
        public byte[] Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string,string> ProcessingMetadata { get; set; }
    }
}
