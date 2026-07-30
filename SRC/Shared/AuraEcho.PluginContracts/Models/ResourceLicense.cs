using System;
namespace AuraEcho.PluginContracts.Models
{
    public class ResourceLicense
    {
        public bool IsValid { get; set; }

        public Guid ResourceId { get; set; }

        /// <summary>
        /// 当前生效的等级
        /// </summary>
        public int? TierLevel { get; set; }

        /// <summary>
        /// 当前生效的等级名称
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// 当前生效等级的权益描述
        /// </summary>
        public string TierDescription { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime? ExpiredAt { get; set; }
    }
}
