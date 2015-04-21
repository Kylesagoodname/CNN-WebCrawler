using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Table
{
    public class TableEntry : TableEntity
    {


        public TableEntry(String keyword, String url, String plainUrl, String title, String date)
        {
            this.PartitionKey = keyword;
            this.RowKey = url;
        }

        public TableEntry() { }

        public String keyword { get; set; }
        public String title { get; set; }
        public String plainUrl { get; set; }
        public String date { get; set; }

    }
}