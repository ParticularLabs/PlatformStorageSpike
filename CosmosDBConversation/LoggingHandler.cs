using Microsoft.Azure.Cosmos;

class LoggingHandler : RequestHandler
{
    private static long totalRequestCharges;

    public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken = default)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var requestCharge = response.Headers["x-ms-request-charge"];
        await Console.Error.WriteLineAsync($"Charged RUs:{requestCharge} for {request.Method.Method} {request.RequestUri} IsBatch:{request.Headers["x-ms-cosmos-is-batch-request"]}");
        
        var requestChargeConverted = Convert.ToInt64(Convert.ToDouble(requestCharge));
        var incrementedValue = Interlocked.Add(ref totalRequestCharges, requestChargeConverted);

        await Console.Error.WriteLineAsync($"Total charged RUs: {incrementedValue}");

        if ((int)response.StatusCode == 429)
        {
            await Console.Error.WriteLineAsync("Request throttled.");
        }

        return response;
    }
}