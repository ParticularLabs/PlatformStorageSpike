using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlatformStorageSpike.Ingestor
{
    internal interface IIndexStore
    {
        Task Initalize(string[] args);
        Task IndexMetadata(IDictionary<string, string> processingAttempt);
    }
}