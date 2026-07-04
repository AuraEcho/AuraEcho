using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using AlibabaCloud.OSS.V2;
using AlibabaCloud.OSS.V2.Credentials;
using AlibabaCloud.OSS.V2.IO;
using AlibabaCloud.OSS.V2.Models;
using AuraEcho.ClientApi.V1.File;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories
{
    public class DebugAlibabaCloudOssRepository : AlibabaCloudOssRepository, IStorageRepository
    {
        public DebugAlibabaCloudOssRepository(HttpHelper httpHelper) : base(httpHelper)
        {
        }

        protected override async Task<UploadFileResponse> SimpleUploadAsync(string filePath)
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
                var request = new PutObjectRequest
                {
                    Bucket = BUCKET_NAME,
                    Key = objectName,
                    Body = fileStream
                };
                var result = await client.PutObjectAsync(request);
                if (result.StatusCode == 200)
                {
                    var uploadResult = await ConfirmUploadAsync(objectName, fileStream.Length, sha256, Path.GetFileName(filePath));
                    return uploadResult;
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
        protected override async Task<UploadFileResponse> MultipartUploadAsync(string filePath, IProgress<double> progress)
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

                // 完成分片上传
                var cmResult = await client.CompleteMultipartUploadAsync(new()
                {
                    Bucket = BUCKET_NAME,
                    Key = objectName,
                    UploadId = initResult.UploadId,
                    CompleteMultipartUpload = new()
                    {
                        Parts = uploadParts
                    }
                });

                if (cmResult.StatusCode == 200)
                {
                    progress?.Report(100);
                    var uploadResult = await ConfirmUploadAsync(objectName, fileStream.Length, sha256, Path.GetFileName(filePath));
                    return uploadResult;
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
        private async Task<UploadFileResponse> ConfirmUploadAsync(
            string relativePath,
            long size,
            string contentHash,
            string originalName)
        {
            var request = new
            {
                RelativePath = relativePath,
                Size = size,
                ContentHash = contentHash,
                OriginalName = originalName
            };
            var result = await _httpHelper.PostAsync<UploadFileResponse>(Urls.FileCallback(), request);
            return result;
        }
    }
}
