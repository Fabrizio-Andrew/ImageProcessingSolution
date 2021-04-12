﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using ImageProducer.Repositories;
using ImageProducer.DataTransferObjects;
using ImageProducer.Settings;
using ImageProducer.Jobs;
using System.Collections;


namespace ImageProducer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageProducerController : ControllerBase
    {

        private readonly IStorageRepository _storageRepository;
        private readonly IJobTable _jobTable;


        public ImageProducerController(IStorageRepository storageRepository, IStorageAccountSettings storageAccountSettings)
        {
            _storageRepository = storageRepository;
            _jobTable = new JobTable(storageAccountSettings);
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public IConfiguration Configuration { get; set; }

        [Route("api/v1/uploadedimages")]
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile fileData, [FromQuery] int imageConversionMode)
        {

            // Catch submission without file data
            if (fileData == null)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(5, null, "fileData", null));
            }

            // Catch submission without imageConversionMode specified via querystring
            if (imageConversionMode == null)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(5, null, "imageConversionMode", null));
            }

            // Create a unique ID for the uploaded blob
            string blobName = $"{Guid.NewGuid()}-{fileData.FileName}";

            // Create the blob with contents of the message provided
            using Stream stream = fileData.OpenReadStream();
            await _storageRepository.UploadFile(ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME, blobName, stream, fileData.ContentType);

            // Assign a GUID tot the job
            string jobId = Guid.NewGuid().ToString();

            // Set the URI for the blob in the jobs table
            var uri = $"api/v1/uploadedimages/ + {blobName}";

            // Create the table entity
            await _jobTable.InsertOrReplaceJobEntity(jobId, status: 1, message: "Blob received.", imageSource: uri, imageConversionMode);


            return CreatedAtRoute("GetFileByIdRoute", new { id = blobName }, null);
        }

        [Route("api/v1/uploadedimages/{id}", Name = "GetFileByIdRoute")]
        [HttpGet]
        public async Task<IActionResult> GetFileById([FromRoute] string id)
        {
            if (id != null)
            {
                try
                {
                    // Get the existing file
                    (MemoryStream memoryStream, string contentType) = await _storageRepository.GetFileAsync(ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME, id);
                    return File(memoryStream, contentType);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("BlobNotFound"))
                    {
                        return StatusCode((int)HttpStatusCode.NotFound, ErrorResponse.GenerateErrorResponse(4, null, "id", id));
                    }
                    return BadRequest();
                }
            }
            return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(5, null, "id", id));
        }

        [Route("api/v1/uploadedimages")]
        [HttpGet]
        public async Task<IActionResult> GetAllFiles()
        {
            try
            {
                // Get only the blobs within the specified container
                List<BlobId> blobList = await _storageRepository.GetListOfBlobs();
                return new ObjectResult(blobList.ToArray());
            }

            // Catch Azure Exceptions
            catch (Exception ex)
            {
                if (ex.Message.Contains("InvalidResourceName"))
                {
                    return StatusCode((int)HttpStatusCode.BadRequest, ErrorResponse.GenerateErrorResponse(null, ex.Message, "containerName", ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME));
                }
                return BadRequest();
            }
        }

        [Route("api/v1/jobs")]
        [HttpGet]
        public async Task<string> RetrieveAllJobs()
        {
            // Get list of jobs
            var results = await _jobTable.RetrieveAllJobs();

            // Make some pretty Json
            JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
            var formattedResults = System.Text.Json.JsonSerializer.Serialize(results, options);

            return formattedResults;
        }

        [Route("api/v1/jobs/{id}")]
        [HttpGet]
        public async Task<string> RetrieveJobById([FromRoute] string id)
        {
            // Get the job
            JobEntity entity = await _jobTable.RetrieveJobEntity(id);

            if (entity != null)
            {
                // Map relevant JobEntity attributes to JobResult class
                JobResult jobResult = new JobResult();
                jobResult.jobId = entity.RowKey;
                jobResult.imageConversionMode = entity.imageConversionMode;
                jobResult.status = entity.status;
                jobResult.statusDescription = entity.statusDescription;
                jobResult.imageSource = entity.imageSource;
                jobResult.imageResult = entity.imageResult;

                // Make some pretty Json
                JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
                var formattedResult = JsonSerializer.Serialize(jobResult, options);

                return formattedResult;
            }

            // Create error response
            ErrorResponse errorResponse = ErrorResponse.GenerateErrorResponse(4, null, "id", id);

            // Format error response
            JsonSerializerOptions errorOptions = new JsonSerializerOptions() { WriteIndented = true };
            var formattedError = JsonSerializer.Serialize(errorResponse, errorOptions);

            return formattedError;
        }
    }
}
