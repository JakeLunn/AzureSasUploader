namespace AzureSasUploader
{
    public static class Constants
    {
        public static AzureSasUploaderOptions DefaultOptions => new AzureSasUploaderOptions
        {
            ParallelUploadThreads = 4,
            ChunkSizeInBytes = 1024 * 1024 * 6 //6MB
        };
    }
}
