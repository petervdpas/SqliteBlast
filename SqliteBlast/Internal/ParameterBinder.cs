using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace SqliteBlast.Internal;

/// <summary>
/// Binds a parameter object to a <see cref="SqliteCommand"/>. Accepts either an
/// <see cref="IDictionary"/> (string keys → values) or a POCO whose public readable
/// properties map to <c>@PropertyName</c> placeholders.
/// </summary>
internal static class ParameterBinder
{
    public static void Bind(SqliteCommand command, object? parameters)
    {
        if (parameters is null) return;

        switch (parameters)
        {
            case IDictionary<string, object?> typedDict:
                foreach (var (key, value) in typedDict)
                    AddParameter(command, key, value);
                break;

            case IDictionary dict:
                foreach (DictionaryEntry entry in dict)
                {
                    if (entry.Key is null) continue;
                    AddParameter(command, entry.Key.ToString()!, entry.Value);
                }
                break;

            default:
                foreach (var prop in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead) continue;
                    AddParameter(command, prop.Name, prop.GetValue(parameters));
                }
                break;
        }
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name.StartsWith('@') ? name : "@" + name;
        parameter.Value = Coerce(value);
        command.Parameters.Add(parameter);
    }

    private static object Coerce(object? value) => value switch
    {
        null         => DBNull.Value,
        Guid g       => g.ToString("D"),
        DateTime dt  => dt.ToString("O"),                                // ISO-8601 round-trip
        DateTimeOffset dto => dto.ToString("O"),
        DateOnly d   => d.ToString("yyyy-MM-dd"),
        TimeOnly t   => t.ToString("HH:mm:ss.fff"),
        bool b       => b ? 1L : 0L,
        Enum e       => Convert.ToInt64(e),
        _            => value,
    };
}
