using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Xml;
using Crawler;
using Table;
//using WorkerRole1;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class admin : System.Web.Services.WebService
    {
        public static List<String> badLinks;

        


        [WebMethod]
        public String grabStats(String input)
        {

            Crawling crawler = new Crawling();
            CloudTable table = crawler.getTable();

            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntry>("Statistics", "StatisticsRow");

            try
            {
                TableResult retrievedResult = table.Execute(retrieveOperation);
                String queueCount = ((TableEntry)retrievedResult.Result).plainUrl;
                String indexNumErrors = ((TableEntry)retrievedResult.Result).date;
                
                String output = queueCount + " | " + indexNumErrors;
                return new JavaScriptSerializer().Serialize(output);
            }
            catch
            {
                return new JavaScriptSerializer().Serialize("Error loading stats");
            }
        }

        [WebMethod]
        public String getRecentAdds(String input)
        {

            Crawling crawler = new Crawling();
            CloudTable table = crawler.getTable();

            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntry>("LastAdded", "LastAddedRow");

            try
            {
                TableResult retrievedResult = table.Execute(retrieveOperation);
                String addResults = ((TableEntry)retrievedResult.Result).plainUrl;

                String output = addResults.Replace('>', '/');
                return new JavaScriptSerializer().Serialize(output);
            }
            catch
            {
                return new JavaScriptSerializer().Serialize("Error loading stats");
            }
        }

        [WebMethod]
        public String getRecentErrors(String input)
        {

            Crawling crawler = new Crawling();
            CloudTable table = crawler.getTable();

            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntry>("Errors", "ErrorRow");

            try
            {
                TableResult retrievedResult = table.Execute(retrieveOperation);
                String addResults = ((TableEntry)retrievedResult.Result).plainUrl;

                String output = addResults.Replace('>', '/');
                return new JavaScriptSerializer().Serialize(output);
            }
            catch
            {
                return new JavaScriptSerializer().Serialize("");
            }
        }


        //Adds a command to the command queue
        [WebMethod]
        public String addCommand(String input)
        {
            Crawling crawler = new Crawling();
            CloudQueue commandsQueue = crawler.getCommands();
            String command = input;
            CloudQueueMessage message = new CloudQueueMessage(command);
            commandsQueue.AddMessage(message);

            return input + " command received";
            
        }

        [WebMethod]
        public String getPageTitle(String input)
        {

            input = input.Replace('/', '>');
            Crawling crawler = new Crawling();
            CloudTable table = crawler.getTable();
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntry>("cnn", input);

            try
            {
                TableResult retrievedResult = table.Execute(retrieveOperation);
                return new JavaScriptSerializer().Serialize(((TableEntry)retrievedResult.Result).plainUrl);
            }
            catch
            {
                return new JavaScriptSerializer().Serialize("Learn to spell correctly.");
            }

        }


    }
}
