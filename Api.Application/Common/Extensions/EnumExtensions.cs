using System.ComponentModel;
using System.Reflection;

namespace Api.Application.Common.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        Type type = value.GetType();

        string? name = Enum.GetName(type, value);
        if (name == null) return value.ToString();

        FieldInfo? field = type.GetField(name);
        if (field == null) return value.ToString();

        if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
        {
            return attribute.Description;
        }

        return value.ToString();
    }
}