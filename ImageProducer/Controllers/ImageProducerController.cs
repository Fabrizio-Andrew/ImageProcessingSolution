using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageProducer.Repositories;
using ImageProducer.DataTransferObjects;
using ClassLibrary.ConfigSettings;

namespace ImageProducer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageProducerController : ControllerBase
    {

        private readonly IStorageRepository _storageRepository;

        public ImageProducerController(IStorageRepository storageRepository)
        {
            _storageRepository = storageRepository;
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
        public async Task<IActionResult> UploadFile(IFormFile formFile)
        {

            // Create a unique ID for the uploaded blob
            string blobName = $"{Guid.NewGuid()}-{formFile.FileName}";

            // Create the blob with contents of the message provided
            using Stream stream = formFile.OpenReadStream();
            await _storageRepository.UploadFile(ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME, blobName, stream, formFile.ContentType);

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
    }
}
