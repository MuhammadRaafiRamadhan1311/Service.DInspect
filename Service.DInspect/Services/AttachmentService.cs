using Microsoft.AspNetCore.Http;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public class AttachmentService : ServiceBase
    {
        private readonly IBlobStorageRepository _blobStorageRepository;

        public AttachmentService(MySetting appSetting, IConnectionFactory connectionFactory, string container, IBlobStorageRepository blobStorageRepository, string accessToken) : base(appSetting, connectionFactory, container, accessToken)
        {
            _blobStorageRepository = blobStorageRepository;
        }

        public async Task<ServiceResult> UploadAttachment(IFormFile files, string userAccount)
        {
            try
            {
                DateTime currenDateTime = DateTime.Now;

                int dotPos = files.FileName.LastIndexOf('.');
                long timeStamp = new DateTimeOffset(EnumCommonProperty.CurrentDateTime).ToUnixTimeMilliseconds();

                string fileName = files.FileName.Substring(0, dotPos);
                string fileType = files.FileName.Substring(dotPos + 1, files.FileName.Length - (dotPos + 1));
                string formattedFileName = $"dinspect.{timeStamp}";

                byte[] fileData = new byte[files.Length];
                var dataFile = $"{formattedFileName}.{fileType}";

                var memorySystem = new MemoryStream();
                files.CopyTo(memorySystem);
                var bytes = memorySystem.ToArray();

                var resultUpload = await _blobStorageRepository.UploadFileAsync(bytes, fileName, files.ContentType, "rotation", "Transaction");

                return new ServiceResult()
                {
                    Message = "Success",
                    Content = resultUpload,
                    IsError = false
                };

            }
            catch (Exception ex)
            {
                return new ServiceResult()
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public async Task<MemoryStream> Download(string fileUrl)
        {
            var result = await _blobStorageRepository.GetFileUrlWithTokenAsync(fileUrl, "Transaction").ConfigureAwait(false);
            return result;
        }

        public async Task<bool> DeleteSync(string fileUrl)
        {
            var result = await _blobStorageRepository.DeleteFileAsync(fileUrl, "Transaction").ConfigureAwait(false);
            return result;
        }
    }
}
