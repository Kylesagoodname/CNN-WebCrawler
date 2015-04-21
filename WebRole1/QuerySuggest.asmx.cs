using Crawler;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using Table;

namespace WebRole1
{
    /// <summary>
    /// Summary description for QuerySuggest
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
     [System.Web.Script.Services.ScriptService]
    public class QuerySuggest : System.Web.Services.WebService
    {
        private static Node root;
        private static Trie titles;
        private static int search;

        private Dictionary<string, List<String>> cache;

        [WebMethod]
        public String[] parseHtml(String url)
        {
            String hi = "";
            try
            {
                HtmlWeb htmlDoc = new HtmlWeb();
                HtmlDocument rootDocument = htmlDoc.Load(url);
                HtmlAgilityPack.HtmlNode bodyNode;
                String[] nodeReturns = new String[2];
                bodyNode = rootDocument.DocumentNode.SelectSingleNode("//title");
                String title = bodyNode.InnerHtml;
                String date = "";
                if (!url.Contains("bleacherreport.com")) {
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
                //errors.Add(url + " " + except);
                String[] err = { "Error: Exception", "Error: Exception" };
                return err;
            }
        
        }

        [WebMethod]
        public List<String> searchTitle(String input)
        {
            if (cache == null)
            {
                cache = new Dictionary<string, List<string>>();
            }
            
            input = input.Trim();
            input = input.ToLower();
            if (cache.Count >= 100)
            {
                cache.Clear();
            }

            if (cache.ContainsKey(input))
            {
                return cache[input];
            }

            String[] keywords = input.Split(' ');

            List<String> results = new List<String>();
            
            //return new JavaScriptSerializer().Serialize(input);

            Crawling crawler = new Crawling();
            CloudTable table = crawler.getTable();
            List<TableEntry> resultWords = new List<TableEntry>();
            foreach (String word in keywords)
            {
                TableQuery<TableEntry> query = new TableQuery<TableEntry>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word));

                foreach (TableEntry entry in table.ExecuteQuery(query))
                {
                    resultWords.Add(entry);
                }
            }

            List<String> orderedWords = linqQuery(resultWords);
            if (!cache.ContainsKey(input))
            {
                cache.Add(input, orderedWords);
            }
            return orderedWords;
        }

        [WebMethod]
        public List<string> linqQuery(List<TableEntry> urlList)
        {
            var results = urlList.Select(x => new Tuple<string, string, string>(x.plainUrl, x.title, x.date))
                .GroupBy(x => x.Item1)
                .Select(x => new Tuple<string, string, string, int>(x.Key, x.ToList().First().Item2, x.ToList().First().Item3, x.ToList().Count))
                .OrderByDescending(x => x.Item4)
                .ThenByDescending(x => x.Item3)
                .Take(10);
            List<string> matches = new List<string>();
            foreach (Tuple<string, string, string, int> result in results)
            {
                if (result.Item2.StartsWith(","))
                {
                    result.Item2.Remove(0, 1);
                }
                matches.Add(result.Item2);
                
            }
            return matches;
        }

        [WebMethod]
        public String suggest(String input)
        {
            search++;
            input = input.ToLower();
            input = input.Trim();
            ArrayList output = new ArrayList { "", "", "", "", "", "", "", "", "", "", "" };

            if (input.Replace(" ", "") != "")
            {
                output = titles.traverse(root, input, search);
            }
            return new JavaScriptSerializer().Serialize(output);
        }

        [WebMethod]
        public String build()
        {
            var filePath = System.IO.Path.GetTempPath() + "\\wiki.txt";
            System.IO.StreamReader File = new System.IO.StreamReader(filePath);
            String[] phrases = System.IO.File.ReadAllLines(filePath);     
            titles = new Trie();

            root = titles.build(phrases);

            return "Successfully built trie";
            //return "successfully built trie with " + getAvailableBytes() + " bytes free";
        }

        [WebMethod]
        public void download()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings
            ["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("main");
            var filePath = System.IO.Path.GetTempPath() + "\\wiki.txt";

            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {

                        CloudBlockBlob blob = (CloudBlockBlob)item;

                        using (var fileStream = System.IO.File.OpenWrite(filePath))
                        {
                            blob.DownloadToStream(fileStream);
                        }
                    }
                }
            }
            Console.WriteLine("Completed");
            Console.ReadLine();
        }
    } 
}
