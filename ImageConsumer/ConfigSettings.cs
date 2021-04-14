namespace ImageConsumer.Settings
{
    public class ConfigSettings
    {
        public const string UPLOADEDIMAGES_CONTAINERNAME = "uploadedimages";

        public const string JOBS_TABLENAME = "imageconversionjobs";

        public const string IMAGEJOBS_PARTITIONKEY = "imageconversions";

        public const string IMAGEJOBS_QUEUE_NAME = "imagestoprocessqueue";

        public const string CONVERTED_IMAGES_CONTAINERNAME = "convertedimages";
    }
}
