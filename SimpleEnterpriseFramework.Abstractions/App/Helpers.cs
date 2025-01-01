namespace SimpleEnterpriseFramework.Abstractions.App;

public static class Helpers
{
    public static string GetTableName<T>() where T : Model, new()
    {
        return new T().TableName;
    }
}