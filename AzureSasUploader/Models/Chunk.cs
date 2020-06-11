using System;
using System.IO;
using System.Text;

namespace AzureSasUploader.Models
{
    internal class Chunk
    {
        public int Index { get; set; }
        public string Id { get; set; }
        public long Start { get; set; }
        public long Length { get; set; }
        public byte[] Bytes { get; set; }

        public Chunk(int index, long chunkSizeInBytes, long fileSize)
        {
            Index = index;
            Id = Convert.ToBase64String(Encoding.ASCII.GetBytes(index.ToString("0000")));
            Start = index * chunkSizeInBytes;
            Length = Math.Min(chunkSizeInBytes, fileSize - Start);
        }

        public void ReadBytes(BinaryReader reader)
        {
            Bytes = reader.ReadBytes((int)Length);
        }
    }
}
