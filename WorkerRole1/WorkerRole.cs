using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using Crawler;
using System.Xml;

using Microsoft.WindowsAzure.Storage.Table;
using HtmlAgilityPack;
using Table;
using System.Text.RegularExpressions;
using System.IO;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private static HashSet<String> duplicates;
        private static List<String> disallows;
        private static List<String> errors;
        private static CloudQueue queue;
        private static CloudTable table;
        private static List<String> lastAdded;
        private PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
        private int count;
        private int indexNum;
        private int errorNum;
        private string state;



        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole2 is running");

            try
            {
                duplicates = new HashSet<string>();
                disallows = new List<String>();
                errors = new List<String>();
                lastAdded = new List<String>();
                Crawling crawler = new Crawling();
                queue = crawler.getQueue();
                table = crawler.getTable();
                CloudQueue commands = crawler.getCommands();
                Boolean onstart = true;
                state = "Crawling";
                String command = "Start";
                //this.RunAsync(this.cancellationTokenSource.Token).Wait();
                while (true)
                {
                    //Check for next command
                    command = checkCommands(commands, command);

                    if (command == "Clear")
                    {
                        table.DeleteIfExists();
                        count = 0;
                        indexNum = 0;

                        duplicates.Clear();
                        lastAdded.Clear();
                        errors.Clear();
                        
                    }
                    else if (command == "Stop")
                    {
                        state = "Idle";
                        insertStats();
                        while (checkCommands(commands, command) != "Start")
                        {
                            Thread.Sleep(1000);
                        }
                    }

                    //Crawl sitemaps and add to queue
                    if (onstart)
                    {

                        String urlBleacher = "http://bleacherreport.com/robots.txt";
                        string url = "http://www.cnn.com/robots.txt";

                        List<String> xmlLinks = readSiteMaps(urlBleacher, "Bleacher");
                        xmlLinks.AddRange(readSiteMaps(url, "Cnn"));
                        

                        List<String> allResults = new List<String>();
                        List<String> results = new List<String>();
                        List<String> resultsBleacher = new List<String>();

                        foreach (String xmlLink in xmlLinks)
                        {
                            results = helper(xmlLink);
                            allResults.AddRange(results);
                        }

                        foreach (String link in allResults)
                        {
                            CloudQueueMessage cloudMessage = new CloudQueueMessage(link);
                            queue.AddMessageAsync(cloudMessage);
                        }
                        onstart = false;
                    }

                    //Continuously crawl urls while worker role is running
                    crawl(queue);
                }

            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }


        private String checkCommands(CloudQueue commands, String command)
        {
            if (commands.PeekMessage() != null)
            {
                CloudQueueMessage message = commands.GetMessage();
                command = message.AsString;
                commands.DeleteMessage(message);
            }
            return command;
        }

        private void crawl(CloudQueue queue)
        {
            //Grab next url to process
            state = "Crawling";
            CloudQueueMessage popMessage = queue.GetMessage();
            String parentLink = popMessage.AsString;

            Boolean validLink = true;
            
            //web.Load(rootUrl.Value, Encoding.GetEncoding("iso-8859-9"));

            try
            {
                HtmlWeb htmlDoc = new HtmlWeb();
                HtmlDocument doc = htmlDoc.Load(parentLink);
                foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                {
                    HtmlAttribute att = link.Attributes["href"];
                    String childLink = att.Value;

                    //If link is a relative url, append http:
                    if (childLink.StartsWith("//"))
                    {
                        childLink = "http:" + childLink;
                    }
                    else if (childLink.StartsWith("/"))
                    {
                        if (parentLink.StartsWith("http://bleacherreport.com"))
                        {
                            childLink = "http://bleacherreport.com" + childLink;
                        }
                        else if (parentLink.StartsWith("http://cnn.com"))
                        {
                            childLink = "http://cnn.com" + childLink;
                        }
                    }

                    //Check for duplicates and disallows
                    if (!duplicates.Contains(childLink) && !disallows.Contains(childLink) && childLink.Contains("cnn.com"))
                    {
                        duplicates.Add(childLink);
                        CloudQueueMessage cloudMessage = new CloudQueueMessage(childLink);
                        queue.AddMessageAsync(cloudMessage);
                    }
                }
            }
            catch (Exception e)
            {
                //add link to exceptions, delete from queue
                if (errors.Count == 10)
                {
                    errors.RemoveAt(0);
                }
                errors.Add("URL: " + parentLink + "  Exception: " + e.Message);
                errorNum++;
                validLink = false;
            }

            if (validLink)
            {
                count++;
                addToTable(popMessage);
            }
        }

        private void addToTable(CloudQueueMessage popMessage)
        {
            //Add each item in the queue to the table
            String poppedMessage = popMessage.AsString;
            String[] nodeVals = parseHtml(poppedMessage);
            String title = nodeVals[0].ToLower();
            String date = nodeVals[1];


            if (!title.Contains("Error: Exception"))
            {
                Regex rgx = new Regex("[^a-zA-Z ]");
                title = rgx.Replace(title, " ");

                String[] keywords = title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                String combinedTitle = "";
                //Combine keywords into a trimmed title
                foreach (String word in keywords)
                {
                    combinedTitle += word;
                    combinedTitle += " ";
                }
                title = combinedTitle.Trim();

                //Inserts each of the keywords into the table as a separate row
                foreach (String word in keywords)
                {

                    TableEntry addKeyword = new TableEntry(word, poppedMessage.Replace('/', '>'), null, null, null);
                    addKeyword.title = title;
                    addKeyword.date = date;
                    addKeyword.plainUrl = poppedMessage;
                    TableOperation insertKeyword = TableOperation.InsertOrReplace(addKeyword);
                    table.Execute(insertKeyword);
                }

                indexNum++;

               
                //Add url to last 10 added 
                if (lastAdded.Count == 10)
                {
                    lastAdded.RemoveAt(0);
                }
                lastAdded.Add(poppedMessage.Replace('/', '>'));
            }

            if (count >= 5)
            {
                insertStats();

                //Build last 10 added table operation
                String lastAddedString = "";
                foreach (String addedUrl in lastAdded)
                {
                    lastAddedString = addedUrl + " | " + lastAddedString;
                }

                TableEntry lastAddEntry = new TableEntry("LastAdded", "LastAddedRow", null, null, null);
                lastAddEntry.plainUrl = lastAddedString;
                TableOperation insertAdded = TableOperation.InsertOrReplace(lastAddEntry);
                table.ExecuteAsync(insertAdded);

                //Insert errors               
                String errorString = "";
                foreach (String error in errors)
                {
                    errorString = error + " | " + errorString;
                }



                if (errors.Count > 0)
                {
                    TableEntry err = new TableEntry("Errors", "ErrorRow", null, null, null);
                    err.plainUrl = errorString;
                    TableOperation insertError = TableOperation.InsertOrReplace(err);
                    table.ExecuteAsync(insertError);
                }

                count = 0;
            }
            queue.DeleteMessageAsync(popMessage);
        }

        private void insertStats()
        {
            //Build stats table operation

            queue.FetchAttributes();
            String statsLine = "Queue Count: " + queue.ApproximateMessageCount + " | State: " + state + " | CPU: "
                                + this.theCPUCounter.NextValue() + " | RAM: " + this.theMemCounter.NextValue();


            Table.TableEntry stats = new TableEntry("Statistics", "StatisticsRow", null, null, null);
            stats.plainUrl = statsLine;
            stats.date = "Urls Crawled: " + (duplicates.Count + errors.Count) + " | Index Size: " + indexNum + " | Number of Errors: " + errorNum;

            TableOperation insertStats = TableOperation.InsertOrReplace(stats);
            table.ExecuteAsync(insertStats);
        }
        private List<String> readSiteMaps(String url, String domain)
        {

            //Potential issue
            WebClient wClient = new WebClient();
            Stream data = wClient.OpenRead(url);
            StreamReader read = new StreamReader(data);
            List<String> lines = new List<string>();
            String line;

            //Reads lines in from robots.txt
            while ((line = read.ReadLine()) != null)
            {
                lines.Add(line);
            }

            List<String> xmlLinks = new List<string>();


            //Grab the sitemaps for each of links in robots.txt
            //Grab the disallows in robots.txt 
            foreach (String phrase in lines)
            {

                String[] sections = phrase.Split(' ');
                //Grab disallowed links
                if (sections[0] == "Disallow:")
                {
                    disallows.Add(sections[1]);
                }
                //Grab valid xml links
                else if (!sections[0].Contains("User"))
                {
                    if (domain == "Cnn")
                    {
                        xmlLinks.Add(sections[1]);
                    }
                    else if (domain == "Bleacher" && sections[1].Contains("nba"))
                    {
                        xmlLinks.Add(sections[1]);
                    }

                }
            }
            return xmlLinks;
        }

        public String[] parseHtml(String url)
        {
            HtmlWeb htmlDoc = new HtmlWeb();
            try
            {
                HtmlDocument rootDocument = htmlDoc.Load(url);
                HtmlAgilityPack.HtmlNode bodyNode;
                String[] nodeReturns = new String[2];
                bodyNode = rootDocument.DocumentNode.SelectSingleNode("//title");
                String title = bodyNode.InnerHtml;
                String date = "";

                if (!url.Contains("bleacherreport.com"))
                {
                    HtmlNode dateNode = rootDocument.DocumentNode.SelectSingleNode("//meta[@itemprop='datePublished']");
                    date = dateNode.GetAttributeValue("content", "not found");
                }
                else
                {
                    date = "No date found";
                }

                String[] nodeVals = { title, date };
                return nodeVals;
            }
            catch (Exception e)
            {
                String except = "" + e;
                errors.Add(url + " " + except);
                String[] err = { "Error: Exception", "Error: Exception" };
                return err;
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();


            Trace.TraceInformation("WorkerRole2 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole2 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole2 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }


        public List<String> helper(String xmlLink)
        {

            //Open xml doc
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlLink);
            //List <String> sumUrls --> this list is the sum of all of the urls found at this level
            List<String> sumUrls = new List<String>();
            //foreach of the links in this doc {

            foreach (XmlNode node in doc.DocumentElement)
            {
                //Grab inner html
                String innerLink = node["loc"].InnerText;
                //If link ends with xml and if link is 2015
                if (innerLink.EndsWith(".xml") && innerLink.Contains("2015"))
                {
                    //resultUrls = helper (new xml doc)
                    List<String> resultUrls = helper(innerLink);
                    //sumUrls = sumUrls.append(resultUrls)
                    sumUrls.AddRange(resultUrls);

                    //TODO: Not all final url links have 2015 in them
                }
                else if (!innerLink.EndsWith(".xml"))
                {
                    //add to sumUrls
                    sumUrls.Add(innerLink);
                }
            }
            //return sumUrls  
            return sumUrls;
        }


    }
}
