using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class BlobStorageRepository : IBlobStorageRepository
    {
        private CloudBlobContainer blobContainer;
        private readonly string containerDInspect;
        private readonly CloudBlobClient client;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger<BlobStorageRepository> logger;
        private const string Rotation = "Rotation";

        public BlobStorageRepository(string connectionString, string containerAmAdm, IMemoryCache memoryCache, ILogger<BlobStorageRepository> logger)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            client = cloudStorageAccount.CreateCloudBlobClient();
            containerDInspect = containerAmAdm;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public async Task<string> UploadFileAsync(byte[] fileBytes, string fileName, string contentType, string rotation, string subFolder)
        {
            blobContainer = client.GetContainerReference(containerDInspect);
            await blobContainer.CreateIfNotExistsAsync();
            var directory = blobContainer.GetDirectoryReference(subFolder);
            var blob = directory.GetBlockBlobReference(fileName);
            blob.Properties.ContentType = contentType;
            blob.Metadata[Rotation] = rotation;
            //await blob.UploadFromByteArrayAsync(imageFileByteArray, 0, imageFileByteArray.Length, AccessCondition.GenerateIfNotExistsCondition(), options: writeOptions, new OperationContext());
            await blob.UploadFromByteArrayAsync(fileBytes, 0, fileBytes.Length);
            await blob.SetMetadataAsync();

            return blob.Uri.AbsoluteUri;
        }

        public async Task<bool> DeleteFileAsync(string fileName, string subFolder)
        {
            blobContainer = client.GetContainerReference(containerDInspect);
            var directory = blobContainer.GetDirectoryReference(subFolder);
            var blob = directory.GetBlockBlobReference(fileName);
            return await blob.DeleteIfExistsAsync().ConfigureAwait(false);
        }

        public async Task<Stream> Download(string filePath, string subFolder)
        {
            blobContainer = client.GetContainerReference(containerDInspect);
            var directoryContainer = blobContainer.GetDirectoryReference(subFolder);
            var blob = directoryContainer.GetBlockBlobReference(filePath.ToLower());

            var memoryStream = new MemoryStream();
            await blob.DownloadToStreamAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }

        public async Task<string> GetFileUrl(string filePath, string subFolder)
        {
            blobContainer = client.GetContainerReference(containerDInspect);
            var directoryContainer = blobContainer.GetDirectoryReference(subFolder);
            var blob = directoryContainer.GetBlockBlobReference(filePath.ToLower());
            string url = null;

            if (await blob.ExistsAsync())
            {
                url = blob.Uri.AbsoluteUri;
            }

            return url;
        }

        public async Task<MemoryStream> GetFileUrlWithTokenAsync(string filename, string blobName)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                logger.LogTrace("Starting GetFileUrlWithTokenAsync for blobName: {urlFilename}", blobName);

                blobContainer = client.GetContainerReference(containerDInspect);
                var directoryContainer = blobContainer.GetDirectoryReference(blobName);
                var blob = directoryContainer.GetBlockBlobReference(filename);
                var url = await GetBlobSasUriAsync(directoryContainer, filename);

                await blob.DownloadToStreamAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting a SAS token for image: {urlFilename}", blobName);
            }

            logger.LogError("Error getting a SAS token, so returning original url");
            return ms;
        }

        private static async Task<string> GetBlobSasUriAsync(CloudBlobDirectory container, string blobName)
        {
            string sasBlobToken;
            var blob = container.GetBlockBlobReference(blobName);
            await blob.FetchAttributesAsync().ConfigureAwait(false);
            int expireDuration = 24;

            var adHocSAS = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = EnumCommonProperty.CurrentDateTime.AddMinutes(-15),
                SharedAccessExpiryTime = EnumCommonProperty.CurrentDateTime.AddHours(expireDuration),
                Permissions = SharedAccessBlobPermissions.Read
            };

            sasBlobToken = blob.GetSharedAccessSignature(adHocSAS);

            if (blob.Metadata != null && blob.Metadata.ContainsKey(Rotation))
            {
                return blob.Uri + sasBlobToken + $"&{Rotation}={blob.Metadata[Rotation]}";
            }

            // Return the URI string for the container, including the SAS token.
            return blob.Uri + sasBlobToken + $"&{Rotation}=0";
        }
    }
}
