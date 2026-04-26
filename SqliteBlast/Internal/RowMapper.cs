using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace SqliteBlast.Internal;

/// <summary>
/// Maps a <see cref="SqliteDataReader"/> row into an instance of the supplied
/// generic type by matching column names (case-insensitive) to public settable properties.
/// </summary>
internal static class RowMapper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropCache = new();

    public static T Map<T>(SqliteDataReader reader) where T : new()
    {
        var instance = new T();
        var props = PropCache.GetOrAdd(typeof(T), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.IsDBNull(i)) continue;

            var columnName = reader.GetName(i);
            var prop = FindProperty(props, columnName);
            if (prop is null || !prop.CanWrite) continue;

            var raw = reader.GetValue(i);
            var converted = ConvertTo(raw, prop.PropertyType);
            if (converted is not null || !prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) is not null)
                prop.SetValue(instance, converted);
        }
        return instance;
    }

    private static PropertyInfo? FindProperty(PropertyInfo[] props, string name)
    {
        for (var i = 0; i < props.Length; i++)
            if (string.Equals(props[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return props[i];
        return null;
    }

    private static object? ConvertTo(object? value, Type targetType)
    {
        if (value is null) return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsInstanceOfType(value)) return value;

        if (underlying == typeof(Guid))
            return value is string gs ? Guid.Parse(gs) : Guid.Parse(value.ToString()!);

        if (underlying == typeof(DateTime))
            return value is string ds ? DateTime.Parse(ds, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)
                                      : Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture);

        if (underlying == typeof(DateTimeOffset))
            return value is string dos ? DateTimeOffset.Parse(dos, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)
                                       : new DateTimeOffset(Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture));

        if (underlying == typeof(DateOnly))
            return DateOnly.Parse(value.ToString()!, System.Globalization.CultureInfo.InvariantCulture);

        if (underlying == typeof(TimeOnly))
            return TimeOnly.Parse(value.ToString()!, System.Globalization.CultureInfo.InvariantCulture);

        if (underlying == typeof(bool))
            return value switch
            {
                bool b   => b,
                long l   => l != 0,
                int i    => i != 0,
                string s => s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase),
                _        => Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture),
            };

        if (underlying.IsEnum)
            return value is string es ? Enum.Parse(underlying, es, ignoreCase: true)
                                      : Enum.ToObject(underlying, Convert.ToInt64(value));

        if (underlying == typeof(byte[]))
            return value as byte[] ?? throw new InvalidCastException($"Cannot map column to byte[].");

        return Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.InvariantCulture);
    }
}
