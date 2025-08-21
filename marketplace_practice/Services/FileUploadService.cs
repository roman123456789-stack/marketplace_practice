using marketplace_practice.Services.interfaces;

namespace marketplace_practice.Services
{
    public class FileUploadService(IWebHostEnvironment environment) : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment = environment;

        public async Task<string> SaveProductImageAsync(IFormFile file)
        {
            // 1. Валидация: только изображения
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
                throw new ArgumentException("Invalid file type. Only images are allowed.");

            // 2. Валидация: размер до 5 МБ
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File is too large. Maximum size is 5 MB.");

            // 3. Путь: wwwroot/uploads/products/
            var uploadsDir = Path.Combine("wwwroot", "uploads", "products");
            Directory.CreateDirectory(uploadsDir); // создаст папку, если нет

            // 4. Уникальное имя файла
            var fileExt = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // 5. Сохраняем файл
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 6. Возвращаем относительный URL
            return $"/uploads/products/{fileName}";
        }
    }
}
