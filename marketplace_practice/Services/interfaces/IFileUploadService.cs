namespace marketplace_practice.Services.interfaces
{
    public interface IFileUploadService
    {
        Task<string> SaveFileAsync(IFormFile file, string subPath);
        Task<List<string>> SaveFilesAsync(List<IFormFile> files, string subPath);
    }
}
