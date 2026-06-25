using AuraEcho.Core.Converters.TypeConverters;
using AuraEcho.Core.Strings;
using System.ComponentModel;

namespace AuraEcho.Core.Enums;

/// <summary>
/// 程序语言枚举
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum AppLanguage
{
    [Description(nameof(Labels.AppLanguage_zh_CN))]
    zh_CN,
    [Description(nameof(Labels.AppLanguage_en_US))]
    en_US,
    [Description(nameof(Labels.AppLanguage_ko_KR))]
    ko_KR,
    [Description(nameof(Labels.AppLanguage_ja_JP))]
    ja_JP
}
