// Connect to a Dataverse instance using interactive login

using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

var list = new List<DesktopFlow>();

var serviceClient = new ServiceClient(connectionOptions: new ConnectionOptions
{
    LoginPrompt = Microsoft.PowerPlatform.Dataverse.Client.Auth.PromptBehavior.Auto,
    AuthenticationType = Microsoft.PowerPlatform.Dataverse.Client.AuthenticationType.OAuth,
    ServiceUri = new Uri(""),
    RedirectUri = new Uri("http://localhost")
}, false, null);

serviceClient.Connect();

Entity entity = new("workflow");

var query = new QueryExpression
{
    ColumnSet = new ColumnSet("workflowid", "name", "clientdata"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression
            {
                AttributeName = "category",
                Operator = ConditionOperator.Equal,
                Values = { 6 }
            }
        }
    },
    PageInfo = new PagingInfo
    {
        Count = 10,
        ReturnTotalRecordCount = true,
        PageNumber = 1
    },
    EntityName = entity.LogicalName
};

var results = await serviceClient.RetrieveMultipleAsync(query);


do
{
    foreach (var recod in results.Entities)
    {
        list.Add(new DesktopFlow
        {
            Id = recod.Id.ToString(),
            Name = recod.GetAttributeValue<string>("name"),
            Size = System.Text.Encoding.Unicode.GetByteCount(recod.GetAttributeValue<string>("clientdata"))
        });
    }

    if (results.MoreRecords)
    {
        query.PageInfo.PagingCookie = results.PagingCookie;
        query.PageInfo.PageNumber++;
        results = await serviceClient.RetrieveMultipleAsync(query);
    }
    else
    {
        break;
    }
} while (true);


list = list.OrderBy(x => x.Size).ToList();

foreach (var item in list)
{
    Console.WriteLine($"DesktopFlow: {item.Name} with id: {item.Id} Size: {BytesToString(item.Size)}");
}

static String BytesToString(long byteCount)
{
    string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
    if (byteCount == 0)
        return "0" + suf[0];
    long bytes = Math.Abs(byteCount);
    int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
    double num = Math.Round(bytes / Math.Pow(1024, place), 1);
    return (Math.Sign(byteCount) * num).ToString() + suf[place];
}