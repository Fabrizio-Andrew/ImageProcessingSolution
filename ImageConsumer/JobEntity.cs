using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageConsumer.Jobs
{
    public class JobEntity : TableEntity
    {
        public int imageConversionMode { get; set; }

        public int status { get; set; }

        public string statusDescription { get; set; }

        public string imageSource { get; set; }

        //public string imageResult { get; set; }
    }
}