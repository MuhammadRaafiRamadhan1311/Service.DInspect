using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Interfaces
{
    public interface IBlobStorageRepository
    {
        Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string rotation, string subFolder);

        Task<bool> DeleteFileAsync(string fileName, string subFolder);

        Task<Stream> Download(string filePath, string subFolder);

        Task<string> GetFileUrl(string filePath, string subFolder);

        Task<MemoryStream> GetFileUrlWithTokenAsync(string filename, string blobName);
    }
}
