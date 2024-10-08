﻿// Connect to a Dataverse instance using interactive login

using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.PowerPlatform.Dataverse.Client.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.CommandLine;
using System.Text;


internal class Program
{
    private static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand();
        rootCommand.Description = "This tool will list all desktop flows in a Dataverse environment and sort them by size.";

        var cServiceUri = new Option<string>("--service-uri", "The service uri.");
        var maxSize = new Option<int>("--min-size", () => 0, "The minimum size to be displayed.");
        var path = new Option<string>("--path", () => "desktopflow.csv", "The path to save the output file.");
        var pageSize = new Option<int>("--page-size", () => 10, "The page size to retrieve records.");

        var command = new Command("list", "List all desktop flows in a Dataverse environment and sort them by size.")
{
    maxSize,
    cServiceUri,
    path,
    pageSize
};

        command.SetHandler((string serviceUri, int maxSize, string path, int pageSize) =>
        {
            ListDesktopFlows(serviceUri, maxSize, path, pageSize).Wait();
        }, cServiceUri, maxSize, path, pageSize);

        rootCommand.Add(command);

        await rootCommand.InvokeAsync(args);

        async Task ListDesktopFlows(string serviceUri, int maxSize, string path, int pageSize)
        {
            var list = new List<DesktopFlow>();

            var serviceClient = new ServiceClient(connectionOptions: new ConnectionOptions
            {
                LoginPrompt = Microsoft.PowerPlatform.Dataverse.Client.Auth.PromptBehavior.Auto,
                AuthenticationType = Microsoft.PowerPlatform.Dataverse.Client.AuthenticationType.OAuth,
                ServiceUri = new Uri(serviceUri),
                RedirectUri = new Uri("http://localhost")
            }, false, null);

            serviceClient.Connect();

            Entity entity = new("workflow");

            var query = new QueryExpression
            {
                ColumnSet = new ColumnSet("workflowid", "name", "clientdata", "modifiedon"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression
                {
                    AttributeName = "category",
                    Operator = ConditionOperator.Equal,
                    Values = { 6 }
                },
            }
                },
                PageInfo = new PagingInfo
                {
                    Count = pageSize,
                    ReturnTotalRecordCount = true,
                    PageNumber = 1
                },
                EntityName = entity.LogicalName
            };
            LinkEntity link = query.AddLink("systemuser", "ownerid", "systemuserid", JoinOperator.Inner);
            link.Columns.AddColumns("fullname");
            link.Columns.AddColumn("internalemailaddress");
            link.EntityAlias = "owner";

            var results = await serviceClient.RetrieveMultipleAsync(query);
            using var file = File.Open(path, FileMode.Create, FileAccess.Write);
            Console.WriteLine(file.Name);
            await file.WriteAsync(Encoding.UTF8.GetBytes("Name,Size,Owner,ModifiedOn\n"));

            do
            {
                var tempList = new List<DesktopFlow>();
                foreach (var recod in results.Entities)
                {
                    int size = Encoding.Unicode.GetByteCount(recod.GetAttributeValue<string>("clientdata"));

                    recod["clientdata"] = null;

                    GC.Collect();

                    if (size < maxSize)
                    {
                        continue;
                    }

                    var dektopFlow = new DesktopFlow
                    {
                        Id = recod.Id.ToString(),
                        Name = recod.GetAttributeValue<string>("name"),
                        Size = size,
                        OwnerName = recod.GetAttributeValue<AliasedValue>("owner.internalemailaddress").Value.ToString(),
                        ModifiedOn = recod.GetAttributeValue<DateTime>("modifiedon"),
                    };

                    list.Add(dektopFlow);
                    tempList.Add(dektopFlow);
                }

                Console.WriteLine($"Retrieved currently {list.Count} desktop flows from the environment.");

                foreach (var item in tempList)
                {
                    await file.WriteAsync(Encoding.UTF8.GetBytes($"{item.Name},{item.Size},{item.OwnerName},{item.ModifiedOn}\n"));
                }

                await file.FlushAsync();

                if (results.MoreRecords)
                {
                    Console.WriteLine("Getting more records...");
                    query.PageInfo.PagingCookie = results.PagingCookie;
                    query.PageInfo.PageNumber++;
                    results = await serviceClient.RetrieveMultipleAsync(query);
                }
                else
                {
                    break;
                }
            } while (true);


            list = list.OrderByDescending(x => x.Size).ToList();

            Console.WriteLine($"Display desktop flows by size on the environment {serviceClient.OrganizationDetail.FriendlyName}");

            foreach (var item in list)
            {
                Console.WriteLine($"DesktopFlow: {item.Name} with id: {item.Id}, Size: {BytesToString(item.Size)}, Owner: {item.OwnerName}, Last Modified date: {item.ModifiedOn}");
            }
        }



        static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}