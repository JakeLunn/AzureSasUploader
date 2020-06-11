namespace AzureSasUploader
{
    public class AzureSasUploaderOptions
    {
        /// <summary>
        /// Number of parallel threads to utilize while executing an Upload operation.
        /// </summary>
        public int ParallelUploadThreads { get; set; }
        /// <summary>
        /// The size, in bytes, of each upload chunk.
        /// </summary>
        public long ChunkSizeInBytes { get; set; }
    }
}
