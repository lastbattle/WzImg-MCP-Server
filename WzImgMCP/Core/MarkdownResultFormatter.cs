using System.Collections;
using System.Reflection;
using System.Text;
namespace WzImgMCP.Core;
public static class MarkdownResultFormatter
{
    public static string Format(object? value)
    {
        var sb = new StringBuilder();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        WriteValue(sb, value, 0, visited, null);
        return sb.ToString().TrimEnd();
    }
    private static void WriteValue(StringBuilder sb, object? value, int level, HashSet<object> visited, string? label)
    {
        var indent = new string(' ', level * 2);
        if (value is null)
        {
            if (label != null)
                sb.AppendLine($"{indent}- {label}: null");
            else
                sb.AppendLine($"{indent}null");
            return;
        }
        var type = value.GetType();
        if (IsSimple(type))
        {
            var scalar = FormatScalar(value);
            if (label != null)
                sb.AppendLine($"{indent}- {label}: {scalar}");
            else
                sb.AppendLine($"{indent}{scalar}");
            return;
        }
        if (!type.IsValueType)
        {
            if (!visited.Add(value))
            {
                if (label != null)
                    sb.AppendLine($"{indent}- {label}: [circular]");
                else
                    sb.AppendLine($"{indent}[circular]");
                return;
            }
        }
        if (value is IDictionary dict)
        {
            if (label != null)
                sb.AppendLine($"{indent}- {label}:");
            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString() ?? "key";
                WriteValue(sb, entry.Value, level + (label != null ? 1 : 0), visited, key);
            }
            return;
        }
        if (value is IEnumerable enumerable && value is not string)
        {
            if (label != null)
                sb.AppendLine($"{indent}- {label}:");
            var childLevel = level + (label != null ? 1 : 0);
            foreach (var item in enumerable)
            {
                if (item is null || IsSimple(item.GetType()))
                {
                    sb.AppendLine($"{new string(' ', childLevel * 2)}- {FormatScalar(item)}");
                }
                else
                {
                    sb.AppendLine($"{new string(' ', childLevel * 2)}-");
                    WriteObjectMembers(sb, item, childLevel + 1, visited);
                }
            }
            return;
        }
        if (label != null)
            sb.AppendLine($"{indent}- {label}:");
        WriteObjectMembers(sb, value, level + (label != null ? 1 : 0), visited);
    }
    private static void WriteObjectMembers(StringBuilder sb, object value, int level, HashSet<object> visited)
    {
        var props = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .OrderBy(p => p.Name, StringComparer.Ordinal);
        foreach (var prop in props)
        {
            var propValue = prop.GetValue(value);
            if (propValue is null) continue;
            if (propValue is bool b && b == false)
            {
                continue;
            }
            if (propValue is string s && string.IsNullOrEmpty(s))
            {
                continue;
            }
            WriteValue(sb, propValue, level, visited, ToSnakeCase(prop.Name));
        }
    }
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new StringBuilder(name.Length + 8);
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
    private static bool IsSimple(Type t)
    {
        return t.IsPrimitive
            || t.IsEnum
            || t == typeof(string)
            || t == typeof(decimal)
            || t == typeof(DateTime)
            || t == typeof(DateTimeOffset)
            || t == typeof(Guid)
            || t == typeof(TimeSpan);
    }
    private static string FormatScalar(object? value)
    {
        return value switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            string s => s.Replace("\r", "").Replace("\n", " "),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
    }
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
public abstract class MarkdownResultBase
{
    public static implicit operator string(MarkdownResultBase value)
        => MarkdownResultFormatter.Format(value);
}
