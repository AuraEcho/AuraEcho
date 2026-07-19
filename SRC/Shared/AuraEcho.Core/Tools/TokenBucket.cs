namespace AuraEcho.Core.Tools;

/// <summary>
/// 令牌桶限流器
/// </summary>
public sealed class TokenBucket
{
    private readonly object _lock = new();
    private readonly double _capacity;
    private readonly double _refillPerSecond;

    private double _tokens;
    private long _lastRefillTicks;

    /// <summary>
    /// 创建一个令牌桶。
    /// </summary>
    /// <param name="ratePerSecond">每秒补充的令牌数</param>
    /// <param name="capacity">桶容量</param>
    public TokenBucket(double ratePerSecond, double capacity)
    {
        if (ratePerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(ratePerSecond), "补充速率必须为正数");
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "桶容量必须为正数");

        _refillPerSecond = ratePerSecond;
        _capacity = capacity;
        _tokens = capacity;
        _lastRefillTicks = DateTime.UtcNow.Ticks;
    }

    /// <summary>
    /// 每秒补充的令牌数
    /// </summary>
    public double RatePerSecond => _refillPerSecond;

    /// <summary>
    /// 桶容量
    /// </summary>
    public double Capacity => _capacity;

    /// <summary>
    /// 尝试消耗一个令牌
    /// </summary>
    public bool TryAcquire()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow.Ticks;
            var elapsedSeconds = (now - _lastRefillTicks) / (double)TimeSpan.TicksPerSecond;
            if (elapsedSeconds > 0)
            {
                _tokens = Math.Min(_capacity, _tokens + elapsedSeconds * _refillPerSecond);
                _lastRefillTicks = now;
            }

            if (_tokens < 1.0)
                return false;

            _tokens -= 1.0;
            return true;
        }
    }
}
