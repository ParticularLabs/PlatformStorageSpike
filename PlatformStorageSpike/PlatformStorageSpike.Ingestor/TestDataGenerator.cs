using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using Microsoft.VisualBasic;
using PlatformStorageSpike.Ingestor.SQLAzure;

namespace PlatformStorageSpike.Ingestor;

internal class TestDataGenerator
{
    int numberOfConversations;
    int numberOfMessagesPerConvesation;

    public TestDataGenerator(int numberOfConversations, int numberOfMessagesPerConvesation)
    {
        this.numberOfConversations = numberOfConversations;
        this.numberOfMessagesPerConvesation = numberOfMessagesPerConvesation;
    }

    public IEnumerable<TestData> GetTestData()
    {
        var conversationIds = Enumerable.Range(0, numberOfConversations).Select(_ => Guid.NewGuid());

        foreach (var conversationId in conversationIds)
        {
            for (int i = 0; i < numberOfMessagesPerConvesation; i++)
            {
                var messageId = Guid.NewGuid().ToString();
        
                var data = new TestData
                {
                    Body = new byte[5 * 1024],
                    Headers = CreateHeaders(messageId, conversationId.ToString()),
                    ProcessingMetadata = CreateProcessingMetadata(messageId, conversationId.ToString())
                };

                yield return data;
            }
        }
    }

    private static Dictionary<string, string> CreateProcessingMetadata(string messageId, string conversationId)
    {
        var processedAt = DateTimeOffset.UtcNow.ToString();
        var processingEndpoint = "MyProcessingEndpoint";
        var processingId = DeterministicGuid.MakeId(messageId, processingEndpoint, processedAt).ToString();

        return new Dictionary<string, string>
        {
            {MetadataKeys.MessageId, messageId},
            {MetadataKeys.ProcessingId, processingId},
            {MetadataKeys.ConversationId, conversationId},
            {"ProcessedAt", processedAt},
            {"ProcessingEndpoint", processingEndpoint}
        };
    }

    private static Dictionary<string, string> CreateHeaders(string messageId, string conversationId)
    {
        return new Dictionary<string, string>
        {
            {MetadataKeys.MessageId, messageId},
            {MetadataKeys.ConversationId, conversationId},
            {"NServiceBus.ContentType", "text/xml"},
            {"NServiceBus.CorrelationId", "7d6bce1d-829b-4fba-abc6-ab2900b53718"},
            {
                "NServiceBus.EnclosedMessageTypes",
                "Core7.Headers.Writers.MyNamespace.MessageToSend, MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
            },
            {"NServiceBus.MessageIntent", "Send"},
            {"NServiceBus.OriginatingEndpoint", "HeaderWriterAuditV7"},
            {"NServiceBus.OriginatingMachine", "MACHINENAME"},
            {"NServiceBus.ReplyToAddress", "HeaderWriterAuditV7"},
            {"NServiceBus.TimeSent", "2019-12-20 10:59:47:141171 Z"},
            {"NServiceBus.Version", "7.2.0"},
        };
    }
}