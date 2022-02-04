using PlatformStorageSpike.Ingestor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public class SqlAzureAuditStore : IAuditIndexStore
{
    public async Task IndexMetadata(IDictionary<string, string> processingMetadata)
    {
        var connectionString = Environment.GetEnvironmentVariable("PlatformSpike_AzureSQLConnectionString");

        var conversationId = processingMetadata[MetadataKeys.ConversationId];
        var processingId = processingMetadata[MetadataKeys.ProcessingId];

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using (var transaction = connection.BeginTransaction())
            {
                var conversationIdParameter = new SqlParameter
                {
                    ParameterName = "@conversationId", // Defining Name  
                    SqlDbType = SqlDbType.UniqueIdentifier, // Defining DataType  
                    Direction = ParameterDirection.Input,
                    Value = Guid.Parse(conversationId) // Setting the direction  
                };

                var processingIdParameter = new SqlParameter
                {
                    ParameterName = "@processingId", // Defining Name  
                    SqlDbType = SqlDbType.UniqueIdentifier, // Defining DataType  
                    Direction = ParameterDirection.Input,
                    Value = Guid.Parse(processingId) // Setting the direction  
                };

                var command = new SqlCommand(StoreConversationText, connection, transaction);
                
                command.Parameters.Add(conversationIdParameter);
                command.Parameters.Add(processingIdParameter);

                await command.ExecuteNonQueryAsync();

                transaction.Commit();
            }
        }
    }

    public async Task Initalize(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PlatformSpike_AzureSQLConnectionString");

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var command = new SqlCommand(InitializeText, connection);

            await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private const string StoreConversationText = @"
INSERT INTO [dbo].[Conversations]
           ([ConversationId]
           ,[MessageProcessingId])
     VALUES
           (@conversationId,
            @processingId)";


    const string InitializeText = @"
IF (NOT EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_SCHEMA = 'dbo' 
                 AND  TABLE_NAME = 'Conversations'))
BEGIN
    CREATE TABLE [dbo].[Conversations](
	[ConversationId] [uniqueidentifier] NOT NULL,
	[MessageProcessingId] [uniqueidentifier] NOT NULL
) ON [PRIMARY]
END";
}

