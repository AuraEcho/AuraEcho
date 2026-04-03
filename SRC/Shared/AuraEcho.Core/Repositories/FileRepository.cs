using System.IO;
using System.Net.Http;
using AuraEcho.Core.Constants;
using AuraEcho.Core.Contracts;
using AuraEcho.Core.Models;
using AuraEcho.Core.Models.Api;
using AuraEcho.Core.Tools;

namespace AuraEcho.Core.Repositories;

public class FileRepository : IFileRepository
{
    private HttpHelper _httpHelper;
    public FileRepository(HttpHelper httpHelper)
    {
        _httpHelper = httpHelper;
    }

    public async Task<bool> DownloadFileAsync(Guid fileId, string outputPath, IProgress<double> progress)
    {
        try
        {
            using var response = await _httpHelper.GetAsync(Urls.DownloadFile(fileId), HttpCompletionOption.ResponseHeadersRead);
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
    public async Task<UploadedFile> GetFileByIdAsync(Guid fileId)
    {
        var result = await _httpHelper.GetAsync<GetUploadedFileByIdResponse>(Urls.GetFileById(fileId));
        if (result == null) return null;
        return new UploadedFile
        {
            FileName = result.FileName,
            Id = result.Id,
            RelativePath = result.RelativePath,
            Size = result.Size,
            UploadTime = result.UploadTime
        };
    }
    public async Task<List<UploadedFile>> GetUploadedFilesAsync()
    {
        var response = await _httpHelper.GetAsync<UploadFileListResponse>(Urls.GetUploadedFiles());
        if (response is null) return null;

        var result = response.Files.Select(f => new UploadedFile
        {
            FileName = f.FileName,
            Id = f.Id,
            MimeType = f.Type,
            RelativePath = f.RelativePath,
            Size = f.Size,
            UploadTime = f.UploadTime,
        }).ToList();
        return result;

    }

    public async Task<Guid?> UploadFileAsync(string filePath, string type)
    {
        using var form = new MultipartFormDataContent();
        using var fs = File.OpenRead(filePath);
        var streamContent = new StreamContent(fs);
        form.Add(streamContent, "file", Path.GetFileName(filePath));
        form.Add(new StringContent(type), "type");

        var response = await _httpHelper.PostAsync<UploadFileResponse>(Urls.UploadFile(), form);
        if (response is null) return null;

        return response.FileId;
    }

    public async Task<Guid?> UploadWithChunksAsync(string filePath, string fileType, IProgress<double> progress)
    {
        var fi = new FileInfo(filePath);
        int chunkSize = 2 * 1024 * 1024;
        int totalChunks = (int)Math.Ceiling((double)fi.Length / chunkSize);

        string sha256;
        await using (var sha256fs = new FileStream(filePath, FileMode.Open))
        {
            sha256 = await HashHelper.ComputeSha256Async(sha256fs);
        }

        // init
        var initForm = new MultipartFormDataContent
        {
            { new StringContent(fi.Name), "fileName" },
            { new StringContent(fileType), "fileType" },
            { new StringContent(fi.Length.ToString()), "totalSize" },
            { new StringContent(totalChunks.ToString()), "totalChunks" },
            { new StringContent(sha256), "sha256"  }
        };

        var initResp = await _httpHelper.PostAsync<UploadInitResponse>(Urls.UploadFileInit(), initForm);
        if (initResp is null) return null;

        if (initResp.IsDuplicated) return initResp.FileId;

        var uploadId = initResp.UploadId;

        // try get already uploaded chunks (in case resume)
        var uploadedResp = await _httpHelper.GetAsync<UploadedChunksResponse>(Urls.GetUploadedChunks(uploadId));
        var uploadedSet = new HashSet<int>(uploadedResp?.ChunkParts ?? []);

        long uploadedBytes = (long)uploadedSet.Count * chunkSize;
        progress.Report((double)uploadedBytes / fi.Length);

        using var fs = File.OpenRead(filePath);
        byte[] buffer = new byte[chunkSize];
        for (int i = 0; i < totalChunks; i++)
        {
            if (uploadedSet.Contains(i)) { fs.Position += chunkSize; continue; }
            int read = await fs.ReadAsync(buffer.AsMemory());
            if (read <= 0) break;
            using var content = new MultipartFormDataContent
            {
                { new StringContent(uploadId.ToString()), "uploadId" },
                { new StringContent(i.ToString()), "chunkIndex" },
                { new StreamContent(new MemoryStream(buffer, 0, read)), "chunk", $"chunk{i}" }
            };
            var uploadResult = await _httpHelper.PostAsync(Urls.UploadChunk(), content);
            if (!uploadResult) return null;

            uploadedBytes += read;
            progress?.Report(uploadedBytes * 100D / fi.Length);
        }

        // merge
        var mergeForm = new MultipartFormDataContent { { new StringContent(uploadId.ToString()), "uploadId" } };

        var mergeResp = await _httpHelper.PostAsync<UploadMergeResponse>(Urls.UploadMerge(), mergeForm);
        if (mergeResp is null) return null;

        return mergeResp.FileId;
    }

}
