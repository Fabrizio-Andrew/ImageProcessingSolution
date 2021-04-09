namespace ImageProducer.Settings
{
    public interface IStorageAccountSettings
    {
        /// <summary>
        /// Defines the standard name for a storage account connection string
        /// </summary>
        public string StorageAccountConnectionString { get; }        
    }
}