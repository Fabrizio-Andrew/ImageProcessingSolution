using System.Collections;
using System.Threading.Tasks;

namespace ImageProducer.Jobs
{
    public interface IJobTable
    {
        Task<JobEntity> RetrieveJobEntity(string jobId);
        Task<bool> UpdateJobEntity(JobEntity jobEntity);
        Task UpdateJobEntityStatus(string jobId, int status, string message, string imageResult);
        Task InsertOrReplaceJobEntity(string jobId, int status, string message, string imageSource, int imageConversionMode);
        Task<ArrayList> RetrieveAllJobs();
    }
}
