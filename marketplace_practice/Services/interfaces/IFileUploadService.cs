namespace marketplace_practice.Services.interfaces
{
    public interface IFileUploadService
    {
        Task<string> SaveProductImageAsync(IFormFile file);
    }
}
