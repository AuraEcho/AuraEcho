using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace AuraEcho.ValidationRules;

public partial class EmailValidationRule : ValidationRule
{
    // 使用预编译的正则以提高性能
    private static readonly Regex EmailRegex = BuildEmailRegex();

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        string input = value as string ?? string.Empty;

        // 非空检查
        if (string.IsNullOrWhiteSpace(input))
            return new ValidationResult(false, "邮箱地址不能为空");

        // 非空格字符 + @ + 非空格字符 + . + 非空格字符
        if (!EmailRegex.IsMatch(input))
            return new ValidationResult(false, "请输入有效的邮箱格式 (例如: example@domain.com)");

        // 长度检查
        if (input.Length > 254)
            return new ValidationResult(false, "邮箱长度超出限制");

        return ValidationResult.ValidResult;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "zh-CN")]
    private static partial Regex BuildEmailRegex();
}
