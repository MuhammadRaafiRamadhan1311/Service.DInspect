using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect.Interfaces
{
    public interface IFormFileValidator
    {
        bool IsAllowedImageFiletype(IFormFile file);

        bool IsValidFileSize(IFormFile imageFile);
    }
}
