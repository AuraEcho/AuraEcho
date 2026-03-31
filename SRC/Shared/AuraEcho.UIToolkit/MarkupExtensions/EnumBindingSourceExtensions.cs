using System;
using System.Windows.Markup;

namespace AuraEcho.UIToolkit.MarkupExtensions
{
    public class EnumBindingSourceExtension : MarkupExtension
    {
        private Type _enumType;
        public Type EnumType
        {
            get => _enumType;
            set
            {
                if (_enumType == value) return;

                if (null != value)
                {
                    Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                    if (!enumType.IsEnum)
                        throw new ArgumentException("Type 必须为枚举类型");
                }

                _enumType = value;
            }
        }

        public EnumBindingSourceExtension() { }
        public EnumBindingSourceExtension(Type enumType)
            => EnumType = enumType;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (null == _enumType)
                throw new InvalidOperationException("EnumType 不能为 NULL");

            Type actualEnumType = Nullable.GetUnderlyingType(_enumType) ?? _enumType;
            Array enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == _enumType)
                return enumValues;

            Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }
}
