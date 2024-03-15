using Microsoft.AspNetCore.Http;
using Service.DInspect.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Helpers
{
    public class FormFileValidator : IFormFileValidator
    {
        private readonly int maxFileSize;

        private readonly byte[] standardPngSpecification = new byte[] { 137, 80, 78, 71 };

        private readonly byte[] standardJpegSpecification = new byte[] { 255, 216, 255, 224 };

        private readonly byte[] standardJpegCanonSpecification = new byte[] { 255, 216, 255, 225 };

        public FormFileValidator(int maxFileSize)
        {
            this.maxFileSize = maxFileSize;
        }

        public bool IsAllowedImageFiletype(IFormFile file)
        {
            if (file != null)
            {
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    fileBytes = ms.ToArray();
                }

                return IsValidImageFile(fileBytes);
            }

            return false;
        }

        public bool IsValidFileSize(IFormFile imageFile)
        {
            return imageFile?.Length <= maxFileSize * 1024 * 1024;
        }

        private bool IsValidImageFile(byte[] imageBytes)
        {
            return standardPngSpecification.SequenceEqual(imageBytes.Take(standardPngSpecification.Length))
                ? true
                : standardJpegSpecification.SequenceEqual(imageBytes.Take(standardJpegSpecification.Length))
                || standardJpegCanonSpecification.SequenceEqual(imageBytes.Take(standardJpegCanonSpecification.Length)) ? true : false;
        }
    }
}
