using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Models.Enums
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DisplayAttribute>();
            return attribute?.Name ?? value.ToString();
        }

        public static T ParseFromDisplayName<T>(this string displayName) where T : Enum
        {
            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentException("Display name cannot be null or empty", nameof(displayName));
            }

            var type = typeof(T);

            // Ищем все значения enum
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<DisplayAttribute>();
                var currentDisplayName = attribute?.Name ?? field.Name;

                if (string.Equals(currentDisplayName, displayName, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)field.GetValue(null);
                }
            }

            throw new ArgumentException($"Display name '{displayName}' not found for enum {type.Name}");
        }
    }
}
