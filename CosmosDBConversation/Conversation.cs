using Newtonsoft.Json;

public class Conversation
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; init; }

    public Message[] Messages { get; set; }
}

public class Message {
    public string MessageId { get; init; }
    public string? RelatedTo { get; init; }
    public string ProcessingEndpoint { get; init; }
    public string OriginatingEndpoint { get; init; }

    public string MessageTypeName { get; init; }
}