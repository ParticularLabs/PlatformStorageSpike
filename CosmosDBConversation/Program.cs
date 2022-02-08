// See https://aka.ms/new-console-template for more information
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

var connectionStringEnvironmentVariableName = "CosmosDBPersistence_ConnectionString";
var connectionString = Environment.GetEnvironmentVariable(connectionStringEnvironmentVariableName) ?? 
"AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

var builder = new CosmosClientBuilder(connectionString);
builder.AddCustomHandlers(new LoggingHandler());
var cosmosClient = builder.Build();

Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync("HierarchyDatabase");
Container container = await database.CreateContainerIfNotExistsAsync("Conversations", "/id");

var fristConversation = new Conversation {
    Id = "Conversation1",
    Messages = new Message[] {
        new Message {
            MessageId = "1",
            RelatedTo = null,
            MessageTypeName = "SubmitOrder",
            OriginatingEndpoint = "Store.ECommerce",
            ProcessingEndpoint = "Store.Sales"
        },
        new Message {
            MessageId = "2",
            RelatedTo = "1",
            MessageTypeName = "OrderPlaced",
            OriginatingEndpoint = "Store.Sales",
            ProcessingEndpoint = "Store.ECommerce"
        },
        new Message {
            MessageId = "3",
            RelatedTo = "1",
            MessageTypeName = "BuyersRemorseIsOver",
            OriginatingEndpoint = "Store.Sales",
            ProcessingEndpoint = "Store.Sales"
        },
        new Message {
            MessageId = "4",
            RelatedTo = "3",
            MessageTypeName = "OrderAccepted",
            OriginatingEndpoint = "Store.Sales",
            ProcessingEndpoint = "Store.CustomerRelations"
        },
        new Message {
            MessageId = "4",
            RelatedTo = "3",
            MessageTypeName = "OrderAccepted",
            OriginatingEndpoint = "Store.Sales",
            ProcessingEndpoint = "Store.ContentManagement"
        },
    }
};

await container.UpsertItemAsync(fristConversation);

var patchTask1 = container.PatchItemStreamAsync("Conversation1", new PartitionKey("Conversation1"), new List<PatchOperation> {
    PatchOperation.Add<Message>("/Messages/-", new Message {
            MessageId = "5",
            RelatedTo = "3",
            MessageTypeName = "OrderAccepted",
            OriginatingEndpoint = "Store.Sales",
            ProcessingEndpoint = "Store.Shipping"
        })
});

var patchTask2 = container.PatchItemStreamAsync("Conversation1", new PartitionKey("Conversation1"), new List<PatchOperation> {
    PatchOperation.Add<Message>("/Messages/-", new Message {
            MessageId = "6",
            RelatedTo = "3",
            MessageTypeName = "OrderAccepted",
            OriginatingEndpoint = "Store.Sales",
            ProcessingEndpoint = "Store.YZ"
        }),
    PatchOperation.Add<Message>("/Messages/-", new Message {
            MessageId = "7",
            RelatedTo = "3",
            MessageTypeName = "OrderAccepted",
            OriginatingEndpoint = "Store.Sales",
            ProcessingEndpoint = "Store.ZY"
        })        
});

var batchesOfTen = Enumerable.Range(8, 300).Select(index =>
    PatchOperation.Add<Message>("/Messages/-", new Message {
            MessageId = $"{index}",
            RelatedTo = "3",
            MessageTypeName = "OrderAccepted",
            OriginatingEndpoint = "Store.Sales",
            ProcessingEndpoint = $"Store.YZ{index}"
        })).Chunk(10);

static Task PatchConcurrently(Container container, IEnumerable<PatchOperation[]> batchesOfTen) {
    var patchTasks = new List<Task>();
    foreach(var batch in batchesOfTen) 
    {
        patchTasks.Add(container.PatchItemStreamAsync("Conversation1", new PartitionKey("Conversation1"), batch));
    }
    return Task.WhenAll(patchTasks);
};

await Task.WhenAll(PatchConcurrently(container, batchesOfTen), patchTask2, patchTask1);

Console.WriteLine("Reading the document");

Conversation conversation = await container.ReadItemAsync<Conversation>("Conversation1", new PartitionKey("Conversation1"));
Console.WriteLine(conversation.Messages.Length);