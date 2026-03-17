using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace AuraEcho.ValidationRules;

public class PasswordValidationRule : ValidationRule
{
    public int MinimumLength { get; set; } = 8;

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        string input = value as string ?? string.Empty;

        // 1. 拦截“全空格”或“空字符串”
        if (string.IsNullOrWhiteSpace(input))
            return new ValidationResult(false, "密码不能为空白");

        // 2. 拦截“包含空格”的密码 (无论空格在开头、中间还是末尾)
        if (input.Any(char.IsWhiteSpace))
            return new ValidationResult(false, "密码不能包含空格");

        // 3. 长度检查
        if (input.Length < MinimumLength)
            return new ValidationResult(false, $"密码长度至少需要 {MinimumLength} 位");

        // 4. 复杂度检查
        bool hasLetter = input.Any(char.IsLetter);
        bool hasDigit = input.Any(char.IsDigit);

        if (!hasLetter || !hasDigit)
        {
            return new ValidationResult(false, "密码必须同时包含字母和数字");
        }

        return ValidationResult.ValidResult;
    }
}
