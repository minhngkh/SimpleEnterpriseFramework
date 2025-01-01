using System.Reflection;
using SimpleEnterpriseFramework.Abstractions.Data;

namespace SimpleEnterpriseFramework.Data.Sqlite;

internal static class Utils
{
    public static (string, object?)[] GetPairsV0(object obj)
    {
        if (obj is IDictionary<string, object> dictionary)
        {
            return dictionary
                .Where(pair => pair.Value != null)
                .Select(pair => (pair.Key, pair.Value))
                .ToArray();
        }

        var type = obj.GetType();
        return type.GetProperties()
            .Where(x => x.GetCustomAttributes<SqlitePropertyAttribute>().Any())
            .Select(x => (x.Name, x.GetValue(obj) ?? null))
            .ToArray();
    }

    public static (string prop, string name, object? val)[] GetProps<T>(T obj)
    {
        return typeof(T)
            .GetProperties()
            .Select(x => (
                    x.Name,
                    x.GetCustomAttribute<SqlitePropertyAttribute>()?.Name,
                    x.GetValue(obj) ?? null
                )
            )
            .Where(x => x.Item2 is not null)
            .ToArray()!;
    }

    public static (PropertyInfo prop, string name)[] GetPropsName<T>() where T : Model, new()
    {
        return typeof(T)
            .GetProperties()
            .Select(x => (x, x.GetCustomAttribute<SqlitePropertyAttribute>()?.Name))
            .Where(x => x.Item2 is not null)
            .ToArray()!;
    }
}