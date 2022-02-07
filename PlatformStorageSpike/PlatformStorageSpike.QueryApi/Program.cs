var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

app.Use((c, next) =>
{
    c.Response.Headers.Add("X-Particular-Version", "4.21.4");
    return next();
});
app.MapControllers();

app.MapGet("/api",
    () =>
    {
        var baseUrl = "https://localhost:7012";

        return new RootUrls
        {
            EndpointsUrl = baseUrl + "endpoints",
            KnownEndpointsUrl = "/endpoints/known", // relative URI to allow proxying
            SagasUrl = baseUrl + "sagas",
            ErrorsUrl = baseUrl + "errors/{?page,per_page,direction,sort}",
            EndpointsErrorUrl = baseUrl + "endpoints/{name}/errors/{?page,per_page,direction,sort}",
            MessageSearchUrl =
                baseUrl + "messages/search/{keyword}/{?page,per_page,direction,sort}",
            EndpointsMessageSearchUrl =
                baseUrl +
                "endpoints/{name}/messages/search/{keyword}/{?page,per_page,direction,sort}",
            EndpointsMessagesUrl =
                baseUrl + "endpoints/{name}/messages/{?page,per_page,direction,sort}",
            Name = "ServiceControlStorageSpike",
            Description = "The management backend for the Particular Service Platform",
            LicenseStatus = "valid",
            LicenseDetails = baseUrl + "license",
            Configuration = baseUrl + "configuration",
            EventLogItems = baseUrl + "eventlogitems",
            ArchivedGroupsUrl = baseUrl + "errors/groups/{classifier?}",
            GetArchiveGroup = baseUrl + "archive/groups/id/{groupId}",
        };

    });

app.Run();

public class RootUrls
{
    public string Description { get; set; }
    public string EndpointsErrorUrl { get; set; }
    public string KnownEndpointsUrl { get; set; }
    public string EndpointsMessageSearchUrl { get; set; }
    public string EndpointsMessagesUrl { get; set; }
    public string EndpointsUrl { get; set; }
    public string ErrorsUrl { get; set; }
    public string Configuration { get; set; }
    public string MessageSearchUrl { get; set; }
    public string LicenseStatus { get; set; }
    public string LicenseDetails { get; set; }
    public string Name { get; set; }
    public string SagasUrl { get; set; }
    public string EventLogItems { get; set; }
    public string ArchivedGroupsUrl { get; set; }
    public string GetArchiveGroup { get; set; }
}