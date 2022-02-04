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
            //HINT: Body and headers represent immutable information that do not change once defined. 
            //      Metadata represents information that is specific to a concrete message handling.
            var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("PlatformSpike_BlobContainerConnectionString"));

            var blobContainerClient = blobClient.GetBlobContainerClient("platform-spike-storage");
            await blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);


            var messageIdHeaderName = "NServiceBus.MessageId";

            var body = new ReadOnlyMemory<byte>(new byte[5*1024]); //KB message body
            var headers = new Dictionary<string, string>
            {
                {messageIdHeaderName, "7d6bce1d-829b-4fba-abc6-ab2900b53718"},
                {"NServiceBus.ContentType", "text/xml"},
                {"NServiceBus.ConversationId", "b613bb02-a97e-4298-a9e9-ab2900b53723"},
                {"NServiceBus.CorrelationId", "7d6bce1d-829b-4fba-abc6-ab2900b53718"},
                {"NServiceBus.EnclosedMessageTypes", "Core7.Headers.Writers.MyNamespace.MessageToSend, MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"},
                {"NServiceBus.MessageIntent", "Send"},
                {"NServiceBus.OriginatingEndpoint", "HeaderWriterAuditV7"},
                {"NServiceBus.OriginatingMachine", "MACHINENAME"},
                {"NServiceBus.ReplyToAddress", "HeaderWriterAuditV7"},
                {"NServiceBus.TimeSent", "2019-12-20 10:59:47:141171 Z"},
                {"NServiceBus.Version", "7.2.0"},
            };

            var messageId = headers[messageIdHeaderName];

            await StoreBodyAndHeaders(blobContainerClient, messageId, body, headers);

            var processedAt = DateTimeOffset.UtcNow.ToString();
            var processingEndpoint = "MyProcessingEndpoint";
            var processingAttemptId = $"{messageId}-{processingEndpoint}-{processedAt}";

            var processingMetadata = new Dictionary<string, string> 
            {
                { "ProcessingAttemptId", processingAttemptId },
                { "ProcessedAt", processedAt },
                { "ProcessingEndpoint", processingEndpoint}
            };


            var isConfirmed = false; // isConfirmed == TxLevel < AtomicSendsWithReceive

            await StoreProcessingMetadata(blobContainerClient, processingAttemptId, processingMetadata, isConfirmed);

            await MarkProcessingMetadataAsConfirmed(blobContainerClient, processingAttemptId);

        }

        static Task MarkProcessingMetadataAsConfirmed(BlobContainerClient blobContainerClient, string processingAttemptId)
        {
            throw new NotImplementedException();
        }

        static Task StoreBodyAndHeaders(BlobContainerClient blobContainerClient, string messageId, ReadOnlyMemory<byte> body, IDictionary<string, string> headers)
        {
            //codify the update commit header on the blob
            var blob = blobContainerClient.GetBlobClient(messageId);

            var options = new BlobUploadOptions
            {
                Metadata = headers
            };

            //TODO: store headers in a separate blob if size is above 8KB

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
