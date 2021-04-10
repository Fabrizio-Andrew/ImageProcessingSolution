using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageProducer.DataTransferObjects;

namespace ImageProducer.Repositories
{
    public interface IStorageRepository
    {
        Task<(MemoryStream fileStream, string contentType)> GetFileAsync(string containerName, string fileName);
        Task<List<BlobId>> GetListOfBlobs();
        Task UploadFile(string containerName, string fileName, Stream fileStream, string contentType);
    }
}
