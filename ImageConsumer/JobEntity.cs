using Microsoft.WindowsAzure.Storage.Table;

namespace ImageConsumer.Jobs
{
    public class JobEntity : TableEntity
    {
        public int imageConversionMode { get; set; }

        public int status { get; set; }

        public string statusDescription { get; set; }

        public string imageSource { get; set; }

        public string imageResult { get; set; }
    }
}