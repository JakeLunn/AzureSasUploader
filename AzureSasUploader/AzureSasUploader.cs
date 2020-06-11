using AzureSasUploader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureSasUploader
{
    /// <summary>
    /// A multi-threaded uploader for uploading a file to an Azure Storage SAS URI.
    /// </summary>
    public class AzureSasUploader
    {
        public event EventHandler<decimal> ProgressChanged;

        private int _currentChunks, _totalChunks;
        private readonly Uri _sasUri;
        private readonly AzureSasUploaderOptions _options;

        public decimal ProgressPercentage => Math.Round((decimal)_currentChunks / _totalChunks * 100, 2);
        public AzureSasUploaderOptions Options
        {
            get
            {
                if (_options == null) return Constants.DefaultOptions;
                return _options;
            }
        }

        #region Constructors
        public AzureSasUploader(string sasUri)
        {
            if (!Uri.TryCreate(sasUri, UriKind.Absolute, out var uri))
                throw new ArgumentException($"\"sasUri\" is not a valid URI or is not an Absolute URI");

            _sasUri = uri;
            Reset();
        }

        public AzureSasUploader(string sasUri, AzureSasUploaderOptions options)
        {
            if (!Uri.TryCreate(sasUri, UriKind.Absolute, out var uri))
                throw new ArgumentException($"\"sasUri\" is not a valid URI or is not an Absolute URI");

            _sasUri = uri;
            _options = options;
            Reset();
        }

        public AzureSasUploader(Uri sasUri)
        {
            if (!sasUri.IsAbsoluteUri)
                throw new ArgumentException($"\"sasUri\" is not an Absolute URI");

            _sasUri = sasUri;
            Reset();
        }

        public AzureSasUploader(Uri sasUri, AzureSasUploaderOptions options)
        {
            if (!sasUri.IsAbsoluteUri)
                throw new ArgumentException($"\"sasUri\" is not an Absolute URI");

            _sasUri = sasUri;
            _options = options;
            Reset();
        }
        #endregion Constructors

        private void Reset()
        {
            _currentChunks = 0;
            _totalChunks = 1; //Avoid divide by zero error
        }

        /// <summary>
        /// Upload a file to the Azure SAS Uri
        /// </summary>
        /// <param name="pathToFile">Full path to the file to upload</param>
        /// <returns></returns>
        public async Task UploadFileAsync(string pathToFile)
        {
            if (Directory.Exists(pathToFile))
                throw new ArgumentException($"File at path \"{pathToFile}\" is a directory. Directories can't be uploaded by this uploader. " +
                    $"Please compress the directory or upload each file individually");

            if (!File.Exists(pathToFile))
                throw new ArgumentException($"File at path \"{pathToFile}\" does not exist");

            using (var client = GetHttpClient())
            using (var reader = new BinaryReader(File.Open(pathToFile, FileMode.Open)))
            {
                var chunks = GetChunks(reader);
                _totalChunks = chunks.Count;

                Parallel.ForEach(chunks, new ParallelOptions { MaxDegreeOfParallelism = _options.ParallelUploadThreads }, chunk =>
                {
                    var iso = Encoding.GetEncoding("iso-8859-1");
                    var encodedBody = iso.GetString(chunk.Bytes);

                    var request = new HttpRequestMessage(HttpMethod.Put, $"{_sasUri}&comp=block&blockid={chunk.Id}")
                    {
                        Content = new StringContent(encodedBody, iso, null)
                    };

                    client.SendAsync(request)
                        .GetAwaiter()
                        .GetResult();

                    Interlocked.Increment(ref _currentChunks);
                    ProgressChanged?.Invoke(this, ProgressPercentage);
                });

                var blockList = GetBlockList(chunks.Select(c => c.Id).ToArray());

                var finalizeRequest = new HttpRequestMessage(HttpMethod.Put, $"{_sasUri}&comp=blocklist")
                {
                    Content = new StringContent(blockList, Encoding.UTF8, "application/xml")
                };

                client.DefaultRequestHeaders.Remove("x-ms-blob-type");
                var response = await client.SendAsync(finalizeRequest);
                response.EnsureSuccessStatusCode();
            }
        }

        private HttpClient GetHttpClient()
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("x-ms-blob-type", "BlockBlob");

            return client;
        }

        private string GetBlockList(string[] ids)
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?><BlockList>");
            foreach (var id in ids)
            {
                builder.Append($"<Latest>{id}</Latest>");
            }
            builder.Append("</BlockList>");

            return builder.ToString();
        }

        private List<Chunk> GetChunks(BinaryReader reader)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            var chunksToUpload = new List<Chunk>();
            var totalSize = reader.BaseStream.Length;
            var chunks = (int)Math.Ceiling((decimal)totalSize / _options.ChunkSizeInBytes);

            for (var index = 0; index < chunks; index++)
            {
                var chunk = new Chunk(index, _options.ChunkSizeInBytes, totalSize);
                chunk.ReadBytes(reader);
                chunksToUpload.Add(chunk);
            }

            return chunksToUpload;
        }

        public void Dispose()
        {

        }
    }
}
