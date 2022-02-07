using Microsoft.AspNetCore.Mvc;
using ServiceControl.CompositeViews.Messages;
using ServiceControl.Contracts.Operations;

public class MessagesController : ControllerBase
{
    [HttpGet]
    [Route("/api/messages")]
    public List<MessagesView> Get()
    {
        var messages = new List<MessagesView>
        {
            new (){ MessageId = "1", MessageType = "Some.Type", ProcessingTime = TimeSpan.FromSeconds(1), TimeSent = DateTime.Now, Status = MessageStatus.Successful },
            new (){ MessageId = "2", MessageType = "Some.Type", ProcessingTime = TimeSpan.FromSeconds(1), TimeSent = DateTime.Now, Status = MessageStatus.Successful },
            new (){ MessageId = "3", MessageType = "Some.Type", ProcessingTime = TimeSpan.FromSeconds(1), TimeSent = DateTime.Now, Status = MessageStatus.Successful },
            new (){ MessageId = "4", MessageType = "Some.Type", ProcessingTime = TimeSpan.FromSeconds(1), TimeSent = DateTime.Now, Status = MessageStatus.Successful }
        };

        Response.Headers.Add("Total-Count", messages.Count.ToString());

        return messages;
    }
}