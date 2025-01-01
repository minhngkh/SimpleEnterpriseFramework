using System.Reflection;

namespace SimpleEnterpriseFramework.Data.Sqlite;

internal static class Utils {
    public static (string, object)[] GetPairs(object obj)
    {
        if (obj is IDictionary<string, object> dictionary)
        {
            return dictionary.Where(pair => pair.Value != null)
                .Select(pair => (pair.Key, pair.Value))
                .ToArray();
        }
        else
        {
            Type type = obj.GetType();
            return type.GetFields(BindingFlags.Instance |
                                  BindingFlags.Public |
                                  BindingFlags.DeclaredOnly)
                .Select(x => (x.Name, x.GetValue(obj) ?? DBNull.Value))
                .ToArray();
        }
    }
}