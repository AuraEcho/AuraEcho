using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AlibabaCloud.OSS.V2;
using AlibabaCloud.OSS.V2.Credentials;
using AlibabaCloud.OSS.V2.IO;
using AlibabaCloud.OSS.V2.Models;
using AuraEcho.ClientApi.V1.File;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories;

public class AlibabaCloudOssRepository : IStorageRepository
{
    protected StsToken _stsToken;
    protected readonly HttpHelper _httpHelper;
    protected const string ENDPOINT = "oss-cn-beijing.aliyuncs.com";
    protected const string REGION = "cn-beijing";
    protected const string BUCKET_NAME = "auraecho-uat";
    public AlibabaCloudOssRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }
    public async Task<bool> DownloadFileAsync(string url, string outputPath, IProgress<double> progress)
    {
        try
        {
            using var response = await _httpHelper.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[80 * 1024];
            long totalRead = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                totalRead += read;

                if (totalBytes > 0)
                {
                    double percent = totalRead * 100.0 / totalBytes;
                    progress?.Report(percent);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<UploadFileResponse> UploadFileAsync(string filePath, IProgress<double> progress = null)
    {
        const long MULTIPART_UPLOAD_THRESHOLD = 5 * 1024 * 1024;
        var targetFile = new FileInfo(filePath);
        if (targetFile.Length < MULTIPART_UPLOAD_THRESHOLD)
        {
            var simpleUploadResult = await SimpleUploadAsync(filePath);
            return simpleUploadResult;
        }

        var multipartUploadResult = await MultipartUploadAsync(filePath, progress);
        return multipartUploadResult;
    }

    protected virtual async Task<UploadFileResponse> SimpleUploadAsync(string filePath)
    {
        string sha256;
        await using (var sha256fs = new FileStream(filePath, FileMode.Open))
            sha256 = await HashHelper.ComputeSha256Async(sha256fs);

        var isUploadedResult = await CheckFileIsUploadedAsync(sha256, Path.GetFileName(filePath));
        if (isUploadedResult.IsUploaded)
        {
            return new UploadFileResponse
            {
                FileId = isUploadedResult.FileId.Value,
                FileUrl = isUploadedResult.FileUrl
            };
        }

        if (IsTokenExpired())
            _stsToken = await GetStsTokenAsync();

        string objectName = $"temp/{Guid.NewGuid():N}{Path.GetExtension(filePath)}";

        var credentialsProvider = new StaticCredentialsProvider(
            _stsToken.AccessKeyId,
            _stsToken.AccessKeySecret,
            _stsToken.SecurityToken
        );

        var config = new Configuration
        {
            CredentialsProvider = credentialsProvider,
            Region = REGION,
            Endpoint = ENDPOINT
        };
        using var client = new Client(config);

        try
        {
            using FileStream fileStream = File.OpenRead(filePath);
            (string callbackBase64, string callbackVarBase64) = BuildCallbackData(sha256, Path.GetFileName(filePath));
            var request = new PutObjectRequest
            {
                Bucket = BUCKET_NAME,
                Key = objectName,
                Body = fileStream,
                Callback = callbackBase64,
                CallbackVar = callbackVarBase64,
            };
            var result = await client.PutObjectAsync(request);
            if (result.StatusCode == 200 && result.CallbackResult is not null)
            {
                var callbackResult = JsonSerializer.Deserialize<UploadFileResponse>(result.CallbackResult);
                return callbackResult;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"上传失败: {ex.Message}");
        }
        return null;
    }

    protected virtual async Task<UploadFileResponse> MultipartUploadAsync(string filePath, IProgress<double> progress)
    {
        const long MULTIPART_CHUNK_SIZE = 1 * 1024 * 1024;
        string sha256;
        await using (var sha256fs = new FileStream(filePath, FileMode.Open))
            sha256 = await HashHelper.ComputeSha256Async(sha256fs);

        var isUploadedResult = await CheckFileIsUploadedAsync(sha256, Path.GetFileName(filePath));
        if (isUploadedResult.IsUploaded)
        {
            progress?.Report(100);
            return new UploadFileResponse
            {
                FileId = isUploadedResult.FileId.Value,
                FileUrl = isUploadedResult.FileUrl
            };
        }

        if (IsTokenExpired())
            _stsToken = await GetStsTokenAsync();

        string objectName = $"temp/{Guid.NewGuid():N}{Path.GetExtension(filePath)}";

        var credentialsProvider = new StaticCredentialsProvider(
            _stsToken.AccessKeyId,
            _stsToken.AccessKeySecret,
            _stsToken.SecurityToken
        );

        var config = new Configuration
        {
            CredentialsProvider = credentialsProvider,
            Region = REGION,
            Endpoint = ENDPOINT
        };
        using var client = new Client(config);

        try
        {
            using FileStream fileStream = File.OpenRead(filePath);

            InitiateMultipartUploadResult initResult =
                await client.InitiateMultipartUploadAsync(new()
                {
                    Bucket = BUCKET_NAME,
                    Key = objectName,
                });

            long partNumber = 1;

            // 存储所有分片上传后的信息
            var uploadParts = new List<UploadPart>();

            // 分块上传文件
            for (long offset = 0; offset < fileStream.Length; offset += MULTIPART_CHUNK_SIZE)
            {
                // 计算当前分片的大小
                var size = Math.Min(MULTIPART_CHUNK_SIZE, fileStream.Length - offset);
                // 上传单个分片
                var upResult = await client.UploadPartAsync(new()
                {
                    Bucket = BUCKET_NAME,
                    Key = objectName,
                    PartNumber = partNumber,
                    UploadId = initResult.UploadId,
                    Body = new BoundedStream(fileStream, offset, size)
                });

                // 保存分片信息，用于后续完成上传
                uploadParts.Add(new() { PartNumber = partNumber, ETag = upResult.ETag });
                partNumber++;

                long uploadedSize = offset + MULTIPART_CHUNK_SIZE;
                progress?.Report(
                    Math.Round(
                        (double)uploadedSize / fileStream.Length * 100,
                        2));
            }

            // 按分片号排序
            uploadParts.Sort((left, right) => (left.PartNumber > right.PartNumber) ? 1 : -1);

            (string callbackBase64, string callbackVarBase64) = BuildCallbackData(sha256, Path.GetFileName(filePath));
            // 完成分片上传
            var cmResult = await client.CompleteMultipartUploadAsync(new()
            {
                Bucket = BUCKET_NAME,
                Key = objectName,
                UploadId = initResult.UploadId,
                CompleteMultipartUpload = new()
                {
                    Parts = uploadParts
                },
                Callback = callbackBase64,
                CallbackVar = callbackVarBase64
            });

            if (cmResult.StatusCode == 200 && cmResult.CallbackResult is not null)
            {
                progress?.Report(100);
                var callbackResult = JsonSerializer.Deserialize<UploadFileResponse>(cmResult.CallbackResult);
                return callbackResult;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"上传失败: {ex.Message}");
        }
        progress?.Report(0);
        return null;
    }

    private (string CallbackBody, string CallbackVar) BuildCallbackData(string contentHash, string originalName)
    {
        var callbackBody = new
        {
            RelativePath = "${object}",
            Size = "${size}",
            ContentHash = "${x:content_hash}",
            OriginalName = "${x:original_name}"
        };
        var callbackBodyJson =
            JsonSerializer.Serialize(callbackBody)
                          .Replace(@"""${object}""", "${object}")
                          .Replace(@"""${size}""", "${size}")
                          .Replace(@"""${x:content_hash}""", "${x:content_hash}")
                          .Replace(@"""${x:original_name}""", "${x:original_name}");
        var callbackObj = new
        {
            callbackUrl = _stsToken.CallbackUrl,
            callbackBody = callbackBodyJson,
            callbackBodyType = "application/json",
        };
        string callbackJson = JsonSerializer.Serialize(callbackObj);
        string callbackBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(callbackJson));
        var callbackVar = new Dictionary<string, string>
        {
            ["x:content_hash"] = contentHash,
            ["x:original_name"] = originalName
        };
        string callbackVarJson = JsonSerializer.Serialize(callbackVar);
        string callbackVarBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(callbackVarJson));
        return (callbackBase64, callbackVarBase64);
    }
    protected bool IsTokenExpired()
    {
        if (_stsToken == null)
            return true;

        return DateTime.UtcNow > DateTime.Parse(_stsToken.Expiration).AddMinutes(-5); // 提前5分钟刷新
    }

    protected async Task<FileCheckResponse> CheckFileIsUploadedAsync(string contentHash, string fileName)
    {
        var result = await _httpHelper.GetAsync<FileCheckResponse>(Urls.FileCheck(contentHash, fileName));
        return result;
    }

    protected async Task<StsToken> GetStsTokenAsync()
    {
        var result = await _httpHelper.GetAsync<StsToken>(Urls.GetStsToken());
        return result;
    }
}
