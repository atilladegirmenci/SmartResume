using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using SmartResume.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

public class FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public FileStorageService(IConfiguration configuration)
    {
        var accessKey = configuration["CloudflareR2:AccessKey"];
        var secretKey = configuration["CloudflareR2:SecretKey"];
        var endpoint = configuration["CloudflareR2:Endpoint"];
        _bucketName = configuration["CloudflareR2:BucketName"];

        var config = new AmazonS3Config 
        { 
            ServiceURL = endpoint,
            ForcePathStyle = true, // R2 için şart
            AuthenticationRegion = "auto" // R2 otomatik bölge kullanır
        };
        
        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var fileKey = $"resumes/{Guid.NewGuid()}_{fileName}";
        
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            InputStream = fileStream,
            ContentType = contentType,
            // R2 İÇİN EN KRİTİK AYARLAR BURASI:
            DisablePayloadSigning = true, 
            UseChunkEncoding = false      // STREAMING hatasını bu çözer
        };

        await _s3Client.PutObjectAsync(request);
        return fileKey; 
    }
public async Task<byte[]> DownloadFileAsync(string fileKey)
{
    var request = new GetObjectRequest
    {
        BucketName = _bucketName,
        Key = fileKey
    };

    using var response = await _s3Client.GetObjectAsync(request);
    using var ms = new MemoryStream();
    await response.ResponseStream.CopyToAsync(ms);
    return ms.ToArray();
}
    public async Task DeleteFileAsync(string fileKey)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, fileKey);
    }
}