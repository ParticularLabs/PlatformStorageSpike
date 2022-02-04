using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using PlatformStorageSpike.Ingestor.SQLAzure;

namespace PlatformStorageSpike.Ingestor
{
    internal class Program
    {
        // reguired env-variables: PlatformSpike_BlobContainerConnectionString => the storage account to store blobs
        //example: 10 SqlAzureAuditStore|AzureTableAuditStore {other storage specific options}
        static async Task Main(string[] args)
        {
            var numTestMessages = int.Parse(args[0]);

            var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("PlatformSpike_BlobContainerConnectionString"));
            var blobContainerClient = blobClient.GetBlobContainerClient("platform-spike-storage");
            await blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            //Tomek and Andreas wanted to start with Audits we will need to tweak this for Errors
            var indexStore = (IAuditIndexStore)Activator.CreateInstance(Type.GetType(args[1], true));
            var isConfirmed = true; //This would be false when transport tx mode > receive only

            await indexStore.Initalize(args);
            for (var i = 0; i < numTestMessages; i++)
            {
                var processingAttempt = await StoreMessageBodyHeaderAndMetaInBlob(blobContainerClient, isConfirmed);
                await indexStore.IndexMetadata(processingAttempt);
            }
        }

        static async Task<IDictionary<string, string>> StoreMessageBodyHeaderAndMetaInBlob(BlobContainerClient blobContainerClient, bool isConfirmed)
        {
            var processedAt = DateTimeOffset.UtcNow.ToString();
            var messageId = "7d6bce1d-829b-4fba-abc6-ab2900b53718";
            var conversationId = "b613bb02-a97e-4298-a9e9-ab2900b53723";

            var body = new ReadOnlyMemory<byte>(new byte[5 * 1024]); //KB message body
            var headers = new Dictionary<string, string>
            {
                {MetadataKeys.MessageId, messageId},
                {MetadataKeys.ConversationId, conversationId},
                {"NServiceBus.ContentType", "text/xml"},
                {"NServiceBus.CorrelationId", "7d6bce1d-829b-4fba-abc6-ab2900b53718"},
                {"NServiceBus.EnclosedMessageTypes", "Core7.Headers.Writers.MyNamespace.MessageToSend, MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"},
                {"NServiceBus.MessageIntent", "Send"},
                {"NServiceBus.OriginatingEndpoint", "HeaderWriterAuditV7"},
                {"NServiceBus.OriginatingMachine", "MACHINENAME"},
                {"NServiceBus.ReplyToAddress", "HeaderWriterAuditV7"},
                {"NServiceBus.TimeSent", "2019-12-20 10:59:47:141171 Z"},
                {"NServiceBus.Version", "7.2.0"},
            };

            var processingEndpoint = "MyProcessingEndpoint";
            var processingId = DeterministicGuid.MakeId(messageId, processingEndpoint, processedAt).ToString();

            var processingMetadata = new Dictionary<string, string>
            {
                { MetadataKeys.MessageId, messageId},
                { MetadataKeys.ProcessingId, processingId },
                { MetadataKeys.ConversationId, conversationId},
                { "ProcessedAt", processedAt },
                { "ProcessingEndpoint", processingEndpoint}
            };

            await StoreBodyAndHeaders(blobContainerClient, messageId, body, headers);

            await StoreProcessingMetadata(blobContainerClient, processingId, processingMetadata, isConfirmed);

            if (!isConfirmed)
            {
                await MarkProcessingMetadataAsConfirmed(blobContainerClient, processingId);
            }

            return processingMetadata;

        }

        static Task MarkProcessingMetadataAsConfirmed(BlobContainerClient blobContainerClient, string processingAttemptId)
        {
            throw new NotImplementedException();
        }

        static Task StoreBodyAndHeaders(BlobContainerClient blobContainerClient, string messageId, ReadOnlyMemory<byte> body, IDictionary<string, string> headers)
        {
            var blob = blobContainerClient.GetBlobClient(messageId);

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

        static Task StoreProcessingMetadata(
            BlobContainerClient blobContainerClient,
            string processingMetadataId,
            IDictionary<string, string> processingMetadata,
            bool isConfirmed)
        {
            var blob = blobContainerClient.GetBlobClient(processingMetadataId);  //TODO: do we need a prefix?

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
}
