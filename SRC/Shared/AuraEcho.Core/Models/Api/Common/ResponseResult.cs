namespace AuraEcho.Core.Models.Api;

public class ResponseResult<T>
{
    /// <summary>
    /// 状态结果
    /// </summary>
    public ResultStatus Status { get; set; }

    /// <summary>
    /// 消息描述
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 返回结果
    /// </summary>
    public T? Data { get; set; }
}
