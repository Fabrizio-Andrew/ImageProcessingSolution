using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using ImageConsumer.Jobs;
using ImageConsumer.Settings;

namespace ImageConsumer
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        [NoAutomaticTrigger]
        public static void ProcessJobsOnDemand(TextWriter log)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);

            CloudQueue queue = GetCloudQueue(storageAccount);

            //While loop - until no more messages
        }


        /// <summary>
        /// Updates the job entity status.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="status">The status.</param>
        /// <param name="message">The message.</param>
        /// <param name="imageResult">The url string for the converted/failed image.</param>
        public async Task UpdateJobEntityStatus(CloudStorageAccount storageAccount, string jobId, int status, string message, string imageResult)
        {

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the job table.
            CloudTable table = tableClient.GetTableReference(ConfigSettings.JOBS_TABLENAME);

            // Retrieve the Job entity
            TableOperation retrieveOperation = TableOperation.Retrieve<JobEntity>(ConfigSettings.IMAGEJOBS_PARTITIONKEY, jobId);
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            JobEntity jobEntityToReplace = retrievedResult.Result as JobEntity;

            if (jobEntityToReplace != null)
            {
                jobEntityToReplace.status = status;
                jobEntityToReplace.statusDescription = message;
                jobEntityToReplace.imageResult = imageResult;

                // Update the Job Entity
                TableOperation replaceOperation = TableOperation.Replace(jobEntityToReplace);
                TableResult result = await table.ExecuteAsync(replaceOperation);
            }
        }

        private static CloudQueue GetCloudQueue(CloudStorageAccount storageAccount)
        {

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            CloudQueue queue = queueClient.GetQueueReference(ConfigSettings.IMAGEJOBS_QUEUE_NAME);

            // Create the queue if it doesn't already exist.
            queue.CreateIfNotExists();

            return queue;

        }
    }
}
