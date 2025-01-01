namespace SimpleEnterpriseFramework.Abstractions.Data;

public static class ModelHelpers
{
    public static string GetTableName<T>() where T : Model, new ()
    {
        return new T().TableName;
    }
}