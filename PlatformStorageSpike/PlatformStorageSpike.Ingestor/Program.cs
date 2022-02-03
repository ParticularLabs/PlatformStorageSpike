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

            var messageIdHeaderName = "NServiceBus.MessageId";

            var body = new byte[5*1024]; //KB message body
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

            //Data that we or customer associate with a message that failed during processing 
            var metadata = @"";


            //codify the update commit header on the blob
            var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("PlatformSpike_BlobContainerConnectionString"));

            var blobContainerClient = blobClient.GetBlobContainerClient("platform-spike-storage");
            await blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = blobContainerClient.GetBlobClient(headers[messageIdHeaderName]);

            var options = new BlobUploadOptions
            {
                Metadata = headers
            };

            await blob.UploadAsync(
                BinaryData.FromBytes(body),
                options,
                CancellationToken.None
            ).ConfigureAwait(false);
        }
    }
}
