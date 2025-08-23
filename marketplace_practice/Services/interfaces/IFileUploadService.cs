using marketplace_practice.Services.service_models;

namespace marketplace_practice.Services.interfaces
{
    public interface IFileUploadService
    {
        Task<string> SaveFileAsync(IFormFile file, string subPath);
        public Task<ICollection<string>> SaveFilesAsync(List<IFormFile> files, string subPath);
    }
}
