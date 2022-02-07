using PlatformStorageSpike.Ingestor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public class SqlAzureAuditStore : IAuditIndexStore
{
    public async Task IndexMetadata(IDictionary<string, string> processingMetadata)
    {
        var connectionString = Environment.GetEnvironmentVariable("PlatformSpike_AzureSQLConnectionString");

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using (var transaction = connection.BeginTransaction())
            {
                await InsertMessage(transaction, processingMetadata);
                await InsertConversation(transaction, processingMetadata);

                transaction.Commit();
            }
        }
    }

    static async Task InsertMessage(SqlTransaction transaction, IDictionary<string, string> metadata)
    {
        var command = new SqlCommand(StoreMessageText, transaction.Connection, transaction);

        command.Parameters.Add(new SqlParameter
        {
            ParameterName = "@messageId",
            DbType = DbType.String,
            Value = metadata[MetadataKeys.MessageId],
            Direction = ParameterDirection.Input,
        });

        command.Parameters.Add(new SqlParameter
        {
            ParameterName = "@messageType",
            DbType = DbType.String,
            Value = metadata[MetadataKeys.EnclosedMessages],
            Direction = ParameterDirection.Input,
        });

        command.Parameters.Add(new SqlParameter
        {
            ParameterName = "@processingTimeMs",
            DbType = DbType.Int32,
            Value = TimeSpan.Parse(metadata[MetadataKeys.ProcessingTime]).TotalMilliseconds,
            Direction = ParameterDirection.Input,
        });

        command.Parameters.Add(new SqlParameter
        {
            ParameterName = "@timeSent",
            DbType = DbType.DateTime,
            Value = DateTime.Parse(metadata[MetadataKeys.TimeSent], DateTimeFormatInfo.InvariantInfo),
            Direction = ParameterDirection.Input,
        });

        command.Parameters.Add(new SqlParameter
        {
            ParameterName = "@status",
            DbType = DbType.String,
            Value = metadata[MetadataKeys.ProcessingStatus],
            Direction = ParameterDirection.Input,
        });

        await command.ExecuteNonQueryAsync();
    }

    static async Task InsertConversation(SqlTransaction transaction, IDictionary<string, string> metadata)
    {
        var conversationIdParameter = new SqlParameter
        {
            ParameterName = "@conversationId", // Defining Name  
            SqlDbType = SqlDbType.UniqueIdentifier, // Defining DataType  
            Direction = ParameterDirection.Input,
            Value = Guid.Parse(metadata[MetadataKeys.ConversationId]) // Setting the direction  
        };

        var processingIdParameter = new SqlParameter
        {
            ParameterName = "@processingId", // Defining Name  
            SqlDbType = SqlDbType.UniqueIdentifier, // Defining DataType  
            Direction = ParameterDirection.Input,
            Value = Guid.Parse(metadata[MetadataKeys.ProcessingId]) // Setting the direction  
        };

        var command = new SqlCommand(StoreConversationText, transaction.Connection, transaction);

        command.Parameters.Add(conversationIdParameter);
        command.Parameters.Add(processingIdParameter);

        await command.ExecuteNonQueryAsync();
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

    const string StoreConversationText = @"
INSERT INTO [dbo].[Conversations]
           ([ConversationId]
           ,[MessageProcessingId])
     VALUES
           (@conversationId,
            @processingId)";

    private const string StoreMessageText = @"
INSERT INTO [dbo].[Messages]
           ([MessageId]
           ,[MessageType]
           ,[ProcessingTimeMs]
           ,[TimeSent]
           ,[Status])
     VALUES
           (@messageId
           ,@messageType
           ,@processingTimeMs
           ,@TimeSent
           ,@Status)";

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
END

IF (NOT EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_SCHEMA = 'dbo' 
                 AND  TABLE_NAME = 'Messages'))
BEGIN
  CREATE TABLE [dbo].[Messages](
	[MessageId] [nvarchar](50) NULL,
	[MessageType] [nvarchar](500) NULL,
	[ProcessingTimeMs] [int] NULL,
	[TimeSent] [datetime] NULL,
	[Status] [int] NULL
) ON [PRIMARY]
END
";
}

