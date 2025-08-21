using marketplace_practice.Services.interfaces;

namespace marketplace_practice.Services
{
    public class FileUploadService(IWebHostEnvironment environment) : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment = environment;

        private readonly string[] _allowedImageTypes =
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };

        public async Task<string> SaveFileAsync(IFormFile file, string subPath)
        {
            ValidateFile(file);

            var filePath = await SaveFileToDisk(file, subPath);
            return $"/uploads/{subPath}/{Path.GetFileName(filePath)}";
        }

        public async Task<List<string>> SaveFilesAsync(List<IFormFile> files, string subPath)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentException("No files provided.");

            var urls = new List<string>();

            foreach (var file in files)
            {
                var url = await SaveFileAsync(file, subPath);
                urls.Add(url);
            }

            return urls;
        }

        public bool DeleteFile(string relativePath)
        {
            var filePath = Path.Combine("wwwroot", relativePath.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

        // Валидация файла
        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty.");

            if (!_allowedImageTypes.Contains(file.ContentType))
                throw new ArgumentException("Invalid file type. Only images are allowed.");

            if (file.Length > 100 * 1024 * 1024) // 100 MB
                throw new ArgumentException("File is too large. Maximum size is 100 MB.");
        }

        // Сохраняет и возвращает полный путь на диске
        private async Task<string> SaveFileToDisk(IFormFile file, string subPath)
        {
            //var webRoot = _environment.WebRootPath;
            var uploadsDir = Path.Combine("wwwroot", "uploads", subPath);
            Directory.CreateDirectory(uploadsDir);

            var fileExt = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }
    }
}
