public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteFileAsync(string fileKey);
    Task<byte[]> DownloadFileAsync(string fileKey);
}