using System;
using System.ComponentModel;
using System.Reflection;

namespace abremir.AllMyBricks.Data.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            var attribute = fi.GetCustomAttribute<DescriptionAttribute>();
            return attribute.Description;
        }
    }
}
