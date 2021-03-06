namespace ImageProducer.Settings
{
    /// <summary>
    /// Contains configurable settings for the ImageProducer project.
    /// </summary>
    public class ConfigSettings
    {
        public const string UPLOADEDIMAGES_CONTAINERNAME = "uploadedimages";

        public const string JOBS_TABLENAME = "imageconversionjobs";

        public const string IMAGEJOBS_PARTITIONKEY = "imageconversions";

        public const string IMAGEJOBS_QUEUE_NAME = "imagestoprocessqueue";

    }
}
