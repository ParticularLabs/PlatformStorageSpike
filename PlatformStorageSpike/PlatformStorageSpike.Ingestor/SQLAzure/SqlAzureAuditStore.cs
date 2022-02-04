using PlatformStorageSpike.Ingestor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public class SqlAzureAuditStore : IAuditIndexStore
{
    public Task IndexMetadata(IDictionary<string, string> processingAttempt)
    {

        throw new NotImplementedException();
    }

    public async Task Initalize(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PlatformSpike_AzureSQLConnectionString");

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();



        }
    }
}

