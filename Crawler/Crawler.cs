using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Crawler
{
    public class Crawling
    {
        public CloudQueue getQueue()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("urls");
            queue.CreateIfNotExists();
            return queue;
        }


        public CloudTable getTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
              ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("websites");
            table.CreateIfNotExists();
            return table;
        }

        public CloudQueue getCommands()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("commands");
            queue.CreateIfNotExists();
            return queue;
        }
    }

    //public class TableEntry : TableEntity
    //{


    //    public TableEntry(String website, String url, String title, String date)
    //    {
    //        this.PartitionKey = website;
    //        this.RowKey = url;
    //    }

    //    public TableEntry() { }

    //    public String title { get; set; }
    //    public String date { get; set; }

    //}
}
