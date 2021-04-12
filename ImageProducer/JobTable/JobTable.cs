using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using ImageProducer.Jobs;
using ImageProducer.Settings;

namespace ImageProducer.Jobs
{
    public class JobTable : IJobTable
    {
        private CloudTableClient _tableClient;
        private CloudTable _table;
        private string _partitionKey;
        private ILogger _log;

        public JobTable(IStorageAccountSettings storageAccountSettings)
        {

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountSettings.StorageAccountConnectionString);

            // Create the table client.
            _tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "jobentity" table.
            _table = _tableClient.GetTableReference(ConfigSettings.JOBS_TABLENAME);

            _table.CreateIfNotExistsAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            _partitionKey = "imageconversions";
        }

        /// <summary>
        /// Retrieves the job entity.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <returns>JobEntity.</returns>
        public async Task<JobEntity> RetrieveJobEntity(string jobId)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<JobEntity>(_partitionKey, jobId);
            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            return retrievedResult.Result as JobEntity;
        }

        /// <summary>
        /// Updates the job entity.
        /// </summary>
        /// <param name="jobEntity">The job entity.</param>
        public async Task<bool> UpdateJobEntity(JobEntity jobEntity)
        {
            TableOperation replaceOperation = TableOperation.Replace(jobEntity);
            TableResult result = await _table.ExecuteAsync(replaceOperation);

            if (result.HttpStatusCode >199 && result.HttpStatusCode < 300)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the job entity status.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="status">The status.</param>
        /// <param name="message">The message.</param>
        /// <param name="imageResult">The url string for the converted/failed image.</param>
        public async Task UpdateJobEntityStatus(string jobId, int status, string message, string imageResult)
        {
            JobEntity jobEntityToReplace = await RetrieveJobEntity(jobId);
            if (jobEntityToReplace != null)
            {
                jobEntityToReplace.status = status;
                jobEntityToReplace.statusDescription = message;
                jobEntityToReplace.imageResult = imageResult;
                await UpdateJobEntity(jobEntityToReplace);
            }
        }

        /// <summary>
        /// Inserts the or replace job entity.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="status">The status.</param>
        /// <param name="message">The message.</param>
        /// <param name="imageSource">The url string for the uploaded image.</param>
        /// <param name="imageConversionMode">The type of conversion to be processed.</param>
        public async Task InsertOrReplaceJobEntity(string jobId, int status, string message, string imageSource, int imageConversionMode)
        {
            // Map parameters to JobEntity attributes
            JobEntity jobEntityToInsertOrReplace = new JobEntity();
            jobEntityToInsertOrReplace.RowKey = jobId;
            jobEntityToInsertOrReplace.imageConversionMode = imageConversionMode;
            jobEntityToInsertOrReplace.status = status;
            jobEntityToInsertOrReplace.statusDescription = message;
            jobEntityToInsertOrReplace.imageSource = imageSource;
            jobEntityToInsertOrReplace.PartitionKey = _partitionKey;

            TableOperation insertReplaceOperation = TableOperation.InsertOrReplace(jobEntityToInsertOrReplace);
            TableResult result = await _table.ExecuteAsync(insertReplaceOperation);
        }
    }
}