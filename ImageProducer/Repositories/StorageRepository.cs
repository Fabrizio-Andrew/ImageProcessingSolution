using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageProducer.Exceptions;
using ImageProducer.Settings;
using ImageProducer.DataTransferObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using SolutionSettings.ConfigSettings;

namespace ImageProducer.Repositories
{
    public class StorageRepository : IStorageRepository
    {
        private BlobContainerClient _blobContainerClient;
        private BlobServiceClient _blobServiceClient;
        private IStorageAccountSettings _storageAccountSettings;

        // I don't think I need Picture Settings at all.///////////////////////////////////////
        //private IPictureSettings _pictureSettings;

        private bool IsInitialized { get; set; }

        /// <summary>
        /// Initializes this instance for use, this is not thread safe
        /// </summary>
        /// <returns>A task</returns>
        /// <remarks>This method is not thread safe</remarks>
        private void Initialize(string containerName)
        {
            if (!IsInitialized)
            {
                _blobServiceClient = new BlobServiceClient(_storageAccountSettings.StorageAccountConnectionString);

                // This needs to change to the input parameter containerName
                _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                
                _blobContainerClient.CreateIfNotExists(publicAccessType: PublicAccessType.None);

                IsInitialized = true;
            }
        }

        /// <summary>
        /// The blob container client
        /// </summary>
        private BlobContainerClient GetBlobContainerClient(string containerName)
        {
            if (!IsInitialized)
            {
                Initialize(containerName);
            }
            return _blobContainerClient;
        }

        public StorageRepository(IStorageAccountSettings storageAccountSettings)
        {
            _storageAccountSettings = storageAccountSettings;
        }

        /// <summary>
        /// Uploads file to blob storage
        /// </summary>
        /// <param name="fileName">The filename of the file to upload which will be used as the blobId</param>
        /// <param name="fileStream">The correspnding fileStream associated with the fileName</param>
        /// <param name="contentType">The content type of the blob to upload</param>
        public async Task UploadFile(string containerName, string fileName, Stream fileStream, string contentType)
        {
            // Sets up the blob client
            BlobClient blobClient = GetBlobClient(containerName, fileName);


            // Get the Cloud Storage Account and set up the CloudBlobClient (used for setting container permissions)
            CloudStorageAccount Account = CloudStorageAccount.Parse(_storageAccountSettings.StorageAccountConnectionString);
            CloudBlobClient cloudBlobClient = Account.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            CloudBlobContainer container = cloudBlobClient.GetContainerReference(containerName);

            // Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync();

            if (containerName.ToLower().Contains("public"))
            {

                // Set permissions on the blob container to ALLOW public access
                await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Container });
            }
            else
            {
                // Set permissions on the blob container to PREVENT public access (private container)
                await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Off });
            }

            // Sets the content type and Uploads the blob
            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders() { ContentType = contentType });

            // Sets the content type
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders() { ContentType = contentType });
        }

        /// <summary>
        /// Gets the file from the blob storage
        /// </summary>
        /// <param name="fileName">The id of the blob to download</param>
        /// <returns>A memory stream, which must be disposed by the caller, that contains the downloaded blob</returns>
        public async Task<(MemoryStream fileStream, string contentType)> GetFileAsync(string containerName, string fileName)
        {
            BlobClient blobClient = GetBlobClient(containerName, fileName);
            using BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();

            
            // Caller is expected to dispose of the memory stream
            MemoryStream memoryStream = new MemoryStream();
            await blobDownloadInfo.Content.CopyToAsync(memoryStream);

            // Reset the stream to the beginning so readers don't have to
            memoryStream.Position = 0;
            return (memoryStream, blobDownloadInfo.ContentType);
        }

        /// <summary>
        /// Returns all of the blob names in a container
        /// </summary>
        /// <returns>All of the blob names in a container</returns>
        /// <remarks>This does not scale, for scalability usitlize the pagaing functionaltiy
        /// to page through the blobs in t</remarks>
        public async Task<List<BlobId>> GetListOfBlobs()
        {
            BlobContainerClient blobContainerClient = GetBlobContainerClient(ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME);
            var blobs = blobContainerClient.GetBlobsAsync();

            List<BlobId> blobNames = new List<BlobId>();

            await foreach (var blobPage in blobs.AsPages())
            {
                foreach (var blobItem in blobPage.Values)
                {
                    var blobject = new BlobId();
                    blobject.id = blobItem.Name;
                    blobNames.Add(blobject);
                }
            }
            return blobNames;
        }

        /// <summary>
        /// Gets the blob client associated with the blob specified in the fileName - only to set up the blob within the SDK.
        /// </summary>
        /// <param name="containerName">The name of the container where the specified file is located.</param>
        /// <param name="fileName">The file name which is the blob id</param>
        /// <returns>The corresponding BlobClient for the fileName, blob ID specified</returns>
        private BlobClient GetBlobClient(string containerName, string fileName)
        {
            BlobContainerClient blobContainerClient = GetBlobContainerClient(containerName);

            return blobContainerClient.GetBlobClient(fileName);
        }
    }
}
