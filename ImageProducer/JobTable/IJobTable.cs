using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageProducer.Jobs;
using Microsoft.AspNetCore.Mvc;


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
