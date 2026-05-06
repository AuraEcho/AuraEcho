using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AuraEcho.Core.Constants;

namespace AuraEcho.Core.Tools;

public class HttpHelper
{
    private readonly HttpClient _client;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public HttpHelper(HttpClient client)
    {
        _client = client ?? new HttpClient();
    }

    /// <summary>
    /// 统一 GET 请求
    /// </summary>
    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            var response = await _client.GetAsync(url);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return default;
        }
    }

    public Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption option)
    {
        return _client.GetAsync(url, option);
    }

    /// <summary>
    /// 统一 POST JSON 请求
    /// </summary>
    public async Task<T?> PostAsync<T>(string url, object data)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(url, data, _jsonOptions);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return default;
        }
    }

    /// <summary>
    /// 统一 POST JSON 请求
    /// </summary>
    public async Task<T?> PostAsync<T>(string url, HttpContent data)
    {
        try
        {
            var response = await _client.PostAsync(url, data);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return default;
        }
    }

    /// <summary>
    /// 统一 POST JSON 请求
    /// </summary>
    public async Task<bool> PostAsync(string url, object data)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(url, data, _jsonOptions);

            // 只要 200-299 都算成功
            if (response.IsSuccessStatusCode)
                return true;

            // 如果有统一错误处理
            await HandleResponse(response);

            return false;
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return false;
        }
    }

    /// <summary>
    /// 统一 POST JSON 请求
    /// </summary>
    public async Task<bool> PostAsync(string url, HttpContent data)
    {
        try
        {
            var response = await _client.PostAsync(url, data);

            // 只要 200-299 都算成功
            if (response.IsSuccessStatusCode)
                return true;

            // 如果有统一错误处理
            await HandleResponse(response);

            return false;
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return false;
        }
    }


    /// <summary>
    /// POST 表单请求
    /// </summary>
    public async Task<string?> PostFormAsync(string url, Dictionary<string, string> formData)
    {
        try
        {
            var content = new FormUrlEncodedContent(formData);
            var response = await _client.PostAsync(url, content);
            return await HandleResponse(response);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return null;
        }
    }

    /// <summary>
    /// 统一 POST JSON 请求
    /// </summary>
    public async Task<T?> PutAsync<T>(string url, object data)
    {
        try
        {
            var response = await _client.PutAsJsonAsync(url, data, _jsonOptions);
            return await HandleResponse<T>(response);
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string url)
    {
        try
        {
            var resp = await _client.DeleteAsync(url);
            resp.EnsureSuccessStatusCode();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // -------------------- 内部统一处理逻辑 --------------------

    private async Task<T?> HandleResponse<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            // HTTP 200-299
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }
        else
        {
            // 统一处理错误码
            string error = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"❌ HTTP {response.StatusCode}: {error}");
            return default;
        }
    }

    private async Task<string?> HandleResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            string error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ HTTP {response.StatusCode}: {error}");
            return null;
        }
    }

    private void HandleException(Exception ex)
    {
        if (ex is TaskCanceledException)
        {
            Debug.WriteLine("⚠️ 请求超时");
        }
        else if (ex is HttpRequestException)
        {
            Debug.WriteLine("⚠️ 网络异常，请检查连接");
        }
        else
        {
            Debug.WriteLine($"⚠️ 未知异常：{ex.Message}");
        }
    }
}
