using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageProcessor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using ImageConsumer.Jobs;
using ImageConsumer.Settings;

namespace ImageConsumer
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        [NoAutomaticTrigger]
        public async Task ProcessJobsOnDemand(TextWriter log)
        {
            log.WriteLine("Job Started");
            try
            {

                // Retrieve storage account from connection string.
                log.WriteLine("Accessing Storage Account...");
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);

                // Get the Queue client
                CloudQueue queue = GetCloudQueue(storageAccount);

                // Create the table client
                var tableClient = storageAccount.CreateCloudTableClient();

                // Create the CloudTable object for the "jobs" table
                CloudTable table = tableClient.GetTableReference(ConfigSettings.JOBS_TABLENAME);

                // Create a blob client
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Create or retrieve a reference to the uploaded images container
                log.WriteLine("Setting up containers...");
                CloudBlobContainer uploadedImagesContainer = blobClient.GetContainerReference(ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME);
                await uploadedImagesContainer.CreateIfNotExistsAsync();

                // Create or retrieve a reference to the converted images container
                CloudBlobContainer convertedImagesContainer = blobClient.GetContainerReference(ConfigSettings.CONVERTED_IMAGES_CONTAINERNAME);
                await convertedImagesContainer.CreateIfNotExistsAsync();


                // Get the first message from queue

                CloudQueueMessage message = await queue.GetMessageAsync();
                while (message != null)
                {

                    // Retrieve the table entry for the job
                    TableOperation retrieveOperation = TableOperation.Retrieve<JobEntity>(ConfigSettings.IMAGEJOBS_PARTITIONKEY, message.AsString);
                    TableResult retrievedResult = table.ExecuteAsync(retrieveOperation).ConfigureAwait(false).GetAwaiter().GetResult();

                    JobEntity job = retrievedResult.Result as JobEntity;

                    // Retrieve the blob name from the imageSource url string
                    string[] urlSplit = job.imageSource.Split('/');
                    string blobName = urlSplit[3];

                    // Convert the image represented in the retrieved result
                    await ConvertAndStoreImage(storageAccount, blobName, job.imageConversionMode, message.AsString, job.imageSource);

                    // Get the next message from queue
                    message = await queue.GetMessageAsync();
                }
            }
            catch (Exception ex)
            {
                log.WriteLine(ex);
            }
        }


        /// <summary>
        /// Updates the job entity status.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="status">The status.</param>
        /// <param name="message">The message.</param>
        /// <param name="imageResult">The url string for the converted/failed image.</param>
        public static async Task UpdateJobEntityStatus(CloudStorageAccount storageAccount, string jobId, int status, string message, string imageResult)
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
                //jobEntityToReplace.imageResult = imageResult;

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


        /// <summary>
        /// Converts and stores the image.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="uploadedImage"></param>
        /// <param name="convertedImagesContainer"></param>
        /// <param name="blobName"></param>
        /// <param name="failedImagesContainer"></param>
        /// <param name="jobId"></param>
        /// <param name="imageSource"></param>
        public static async Task ConvertAndStoreImage(CloudStorageAccount storageAccount, string blobName, int imageConversionMode, string jobId, string imageSource)
        {
            string convertedBlobName = $"{Guid.NewGuid()}-{blobName}";

            try
            {
                // Update Job Status - about to convert image
                await UpdateJobEntityStatus(storageAccount, jobId, 2, "Processing blob.", imageSource);

                //(MemoryStream memoryStream, string contentType) = await _storageRepository.GetFileAsync(ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME, id);

                // Get the Blob Container Client
                BlobServiceClient blobServiceClient = new BlobServiceClient(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME);

                // Get the Blob Client for the specified blob
                BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
                BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();

                // Set up blob stream
                using (MemoryStream inStream = new MemoryStream())
                {
                    await blobDownloadInfo.Content.CopyToAsync(inStream);
                    inStream.Position = 0;

                    // Convert the image based on the imageConversionMode
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                        {

                            switch (imageConversionMode)
                            {
                                case 1:
                                    {
                                        imageFactory.Load(inStream)
                                                    .Filter(ImageProcessor.Imaging.Filters.Photo.MatrixFilters.GreyScale)
                                                    .Save(outStream);
                                        break;
                                    }
                                case 2:
                                    {
                                        imageFactory.Load(inStream)
                                                    .Filter(ImageProcessor.Imaging.Filters.Photo.MatrixFilters.Sepia)
                                                    .Save(outStream);
                                        break;
                                    }
                                case 3:
                                    {
                                        imageFactory.Load(inStream)
                                                    .Filter(ImageProcessor.Imaging.Filters.Photo.MatrixFilters.Comic)
                                                    .Save(outStream);
                                        break;
                                    }
                            }

                            outStream.Seek(0, SeekOrigin.Begin);

                            // Create a blob client
                            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                            // Create or retrieve a reference to the converted images container
                            CloudBlobContainer convertedImagesContainer = cloudBlobClient.GetContainerReference(ConfigSettings.CONVERTED_IMAGES_CONTAINERNAME);
                            bool created = await convertedImagesContainer.CreateIfNotExistsAsync();

                            CloudBlockBlob convertedBlockBlob = convertedImagesContainer.GetBlockBlobReference(convertedBlobName);

                            //convertedBlockBlob.Metadata.Add(ConfigSettings.JOBID_METADATA_NAME, jobId);

                            // Upload the converted blob to the converted images container
                            convertedBlockBlob.Properties.ContentType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                            await convertedBlockBlob.UploadFromStreamAsync(outStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //await StoreFailedImage(log, uploadedImage, blobName, failedImagesContainer, convertedBlobName: convertedBlobName, jobId: jobId);
            }
        }
    }
}