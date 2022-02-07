using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ServiceControl.CompositeViews.Messages;
using ServiceControl.Contracts.Operations;

public class MessagesController : ControllerBase
{
    [HttpGet]
    [Route("/api/messages")]
    public List<MessagesView> Get(int per_page=10, int page=0, string direction = "asc", bool includeSystemMessages = false, string sort = "time_sent")
    {
        var connectionString = Environment.GetEnvironmentVariable("PlatformSpike_AzureSQLConnectionString");

        using (var connection = new SqlConnection(connectionString))
        {
            var sortColumn = new Dictionary<string, string>
            {
                {"time_sent", "TimeSent"},
                {"message_type", "MessageType"},
                {"processing_time", "ProcessingTimeMs"},
                {"id", "MessageId"}
            };
            var pageQuery = @$"SELECT * FROM Messages ORDER BY {sortColumn[sort]} {direction} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var messages = connection.Query<MessagesView>(
                pageQuery, 
                new
                {
                    Offset = (page-1)*per_page, 
                    PageSize = per_page,
                    Direction = direction
                }
                ).ToList();

            var totalMessages = connection.Query<int>("SELECT count(*) FROM Messages").First();

            Response.Headers.Add("Total-Count", totalMessages.ToString());

            return messages;
        }

        /*
        var messages = new List<MessagesView>
        {
            new (){ MessageId = "1", MessageType = "Some.Type", ProcessingTime = TimeSpan.FromSeconds(1), TimeSent = DateTime.Now, Status = MessageStatus.Successful },
            new (){ MessageId = "2", MessageType = "Some.Type", ProcessingTime = TimeSpan.FromSeconds(1), TimeSent = DateTime.Now, Status = MessageStatus.Successful },
            new (){ MessageId = "3", MessageType = "Some.Type", ProcessingTime = TimeSpan.FromSeconds(1), TimeSent = DateTime.Now, Status = MessageStatus.Successful },
            new (){ MessageId = "4", MessageType = "Some.Type", ProcessingTime = TimeSpan.FromSeconds(1), TimeSent = DateTime.Now, Status = MessageStatus.Successful }
        };
        */
    }
}