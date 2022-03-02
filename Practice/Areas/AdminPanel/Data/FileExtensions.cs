using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Practice.Areas.AdminPanel.Data
{
    public static class FileExtensions
    {
        public static bool IsImage(this IFormFile file)
        {
            return file.ContentType.Contains("image");
        }
        public static bool IsAllowedSize(this IFormFile file,int mb)
        {
            return file.Length < mb * 1024 * 1000;
        }
        public static async Task<string> GenerateFile(this IFormFile file, string folderPath)
        {
            var fileName = $"{Guid.NewGuid()}-{file.FileName}";
            var path = Path.Combine(folderPath, fileName);
            using (var fileStream = new FileStream(path, FileMode.CreateNew))
            {
                await file.CopyToAsync(fileStream);
            }
            return fileName;
        }
    }
}
