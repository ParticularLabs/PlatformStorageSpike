using PlatformStorageSpike.Ingestor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AzureTableStorage : IIndexStore
{
    public Task IndexMetadata(IDictionary<string, string> processingAttempt)
    {
        throw new NotImplementedException();
    }

    public Task Initalize(string[] args)
    {
        throw new NotImplementedException();
    }
}

