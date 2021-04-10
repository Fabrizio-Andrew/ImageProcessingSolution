using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImageProducer.Repositories
{
    public interface IStorageRepository
    {
        Task<(MemoryStream fileStream, string contentType)> GetFileAsync(string containerName, string fileName);
        Task<List<string>> GetListOfBlobs(string containerName);
        Task UploadFile(string containerName, string fileName, Stream fileStream, string contentType);
    }
}
