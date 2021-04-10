using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageProducer.Repositories;
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
            // Create the blob with contents of the message provided
            using Stream stream = formFile.OpenReadStream();
            await _storageRepository.UploadFile(ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME, formFile.FileName, stream, formFile.ContentType);

            // TO-DO: Set "uploadedimages" to a setting somewhere
            return CreatedAtRoute("GetFileByIdRoute", new { containerName = ConfigSettings.UPLOADEDIMAGES_CONTAINERNAME, fileName = formFile.FileName }, null);
        }
    }
}
