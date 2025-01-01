using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Data.Sqlite;
using SimpleEnterpriseFramework.Abstractions.Data;

namespace SimpleEnterpriseFramework.Data.Sqlite;

// TODO: Add ability to have custom field name in table model
public class SqliteDriver : IDatabaseDriver
{
    private readonly SqliteDriverOptions _options;

    private readonly SqliteConnection _connection;

    public SqliteDriver(SqliteDriverOptions options)
    {
        _options = options;
        _connection = new SqliteConnection(_options.ConnectionString);
        _connection.Open();
    }

    public void Add<T>(T obj, List<string>? selectedFields = null) where T : Model, new()
    {
        // ArgumentNullException.ThrowIfNull(obj);

        var table = obj.TableName;

        StringBuilder fieldBuilder = new();
        StringBuilder valueBuilder = new();
        var valuePairs = Utils.GetPairs(obj);

        Console.WriteLine(valuePairs.Length);

        using var command = _connection.CreateCommand();

        var i = 0;
        foreach (var (field, val) in valuePairs)
        {
            if (selectedFields is not null)
            {
                if (!selectedFields.Contains(field)) continue;
            }

            if (i > 0) fieldBuilder.Append(", ");
            fieldBuilder.Append(field);

            if (i > 0) valueBuilder.Append(", ");
            valueBuilder.Append($"@val{i}");
            command.Parameters.AddWithValue($"@val{i}", val ?? DBNull.Value);

            i++;
        }

        command.CommandText =
            $"INSERT INTO {table}({fieldBuilder}) VALUES({valueBuilder});";

        Console.WriteLine(command.CommandText);

        command.ExecuteNonQuery();
    }

    public List<T> Find<T>(T? obj, List<string>? selectedFields = null)
        where T : Model, new()
    {
        var table = ModelHelpers.GetTableName<T>();

        var result = new List<T>();
        StringBuilder commandBuilder = new();

        using var command = _connection.CreateCommand();

        commandBuilder.Append($"SELECT * FROM {table}");
        if (obj is not null)
        {
            var pairs = Utils.GetPairs(obj);

            var i = 0;
            foreach (var (field, val) in pairs)
            {
                if (selectedFields is not null)
                {
                    if (!selectedFields.Contains(field)) continue;
                }

                if (i == 0) commandBuilder.Append(" WHERE ");

                if (i > 0) commandBuilder.Append(" AND ");
                if (val is null)
                {
                    commandBuilder.Append($"{field} IS NULL ");
                }
                else
                {
                    commandBuilder.Append($"{field} = @val{i} ");
                    command.Parameters.AddWithValue($"@val{i}", val);
                    i++;
                }
            }
        }

        command.CommandText = commandBuilder.ToString();
        Debug.WriteLine(command.CommandText);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(parseInto<T>(reader));
        }

        return result;
    }

    public T? First<T>(T? obj, List<string>? selectedFields = null) where T : Model, new()
    {
        var result = Find(obj, selectedFields);

        return result.Count == 0 ? null : result[0];
    }

    public void Update<T>(
        T updates, List<string>? updateFields, T conditions, List<string>? conditionFields
    ) where T : Model, new()
    {
        var table = updates.TableName;

        StringBuilder commandBuilder = new();
        using var command = _connection.CreateCommand();
        commandBuilder.Append($"UPDATE {table}");

        var updatePairs = Utils.GetPairs(updates);
        if (updatePairs.Length == 0) return;

        // commandBuilder
        //     .Append(" SET ")
        //     .AppendJoin(
        //         ", ",
        //         updatePairs.Select(
        //             (item, index) => item.Item2 is null
        //                 ? $"{item.Item1} IS NULL"
        //                 : $"{item.Item1} = @val{index}"
        //         )
        //     );

        commandBuilder.Append(" SET ");

        var i = 0;
        foreach (var (field, val) in updatePairs)
        {
            if (updateFields is not null)
            {
                if (!updateFields.Contains(field)) continue;
            }

            if (i > 0) commandBuilder.Append(", ");

            commandBuilder.Append($"{field} = @val{i}");
            command.Parameters.AddWithValue($"@val{i}", val ?? DBNull.Value);

            i++;
        }

        var conditionPairs = Utils.GetPairs(conditions);
        if (conditionPairs.Length == 0) throw new Exception("Conditions cannot be empty");

        // commandBuilder
        //     .Append(" WHERE ")
        //     .AppendJoin(" AND ",
        //         conditionPairs.Select(
        //             (item, index) => item.Item2 is null
        //                 ? $"{item.Item1} IS NULL"
        //                 : $"{item.Item1} = @cond{index}"
        //         )
        //     );

        commandBuilder.Append(" WHERE ");

        i = 0;
        foreach (var (field, val) in conditionPairs)
        {
            if (conditionFields is not null)
            {
                if (!conditionFields.Contains(field)) continue;
            }

            if (i > 0) commandBuilder.Append(" AND ");

            if (val is null)
            {
                commandBuilder.Append($"{field} IS NULL ");
            }
            else
            {
                commandBuilder.Append($"{field} = @cond{i} ");
                command.Parameters.AddWithValue($"@cond{i}", val);
                i++;
            }
        }

        // foreach (SqliteParameter x in command.Parameters)
        // {
        //     Console.WriteLine($"{x.ParameterName} {x.DbType} {x.Value}");
        // }

        command.CommandText = commandBuilder.ToString();
        Console.WriteLine(command.CommandText);
        command.ExecuteNonQuery();
    }

    public void Delete<T>(T obj, List<string>? selectedFields = null) where T : Model, new()
    {
        var table = obj.TableName;

        StringBuilder commandBuilder = new();
        using var command = _connection.CreateCommand();

        commandBuilder.Append($"DELETE FROM {table} WHERE ");
        var pairs = Utils.GetPairs(obj);
        if (pairs.Length == 0 || selectedFields?.Count == 0)
            throw new Exception("Conditions cannot be empty");

        var i = 0;
        foreach (var (field, value) in pairs)
        {
            if (selectedFields is not null)
            {
                if (!selectedFields.Contains(field)) continue;
            }

            if (i > 0) commandBuilder.Append(" AND ");
            if (value is null)
            {
                commandBuilder.Append($"{field} IS NULL ");
            }
            else
            {
                commandBuilder.Append($"{field} = @val{i}");
                command.Parameters.AddWithValue($"@val{i}", value);
                i++;
            }
        }

        Debug.WriteLine(commandBuilder.ToString());
        command.CommandText = commandBuilder.ToString();
        command.ExecuteNonQuery();
    }

    public List<string> ListTables()
    {
        List<string> result = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            command.CommandText =
                @"SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
            using (SqliteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Debug.Assert(reader.GetName(0) == "name");
                    result.Add(reader.GetString(0));
                }
            }
        }

        return result;
    }

    public List<ColumnInfo> ListColumns(string table)
    {
        List<ColumnInfo> result = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA table_info('{table}')";
            using (SqliteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    ColumnInfo info = new ColumnInfo(
                        id: reader.GetInt64(0),
                        name: reader.GetString(1),
                        type: reader.GetString(2),
                        nullable: reader.GetInt32(3) == 0,
                        isPrimaryKey: reader.GetInt32(5) > 0
                    );
                    result.Add(info);
                }
            }
        }

        return result;
    }

    public void CreateTable<T>(bool deleteIfExist = false) where T : Model, new()
    {
        Console.WriteLine(ModelHelpers.GetTableName<T>());

        bool primaryKeyFound = false;

        StringBuilder foreignConstraintBuilder = new();
        StringBuilder commandBuilder = new();

        commandBuilder.AppendLine($"CREATE TABLE {ModelHelpers.GetTableName<T>()} (");
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance |
                                                 BindingFlags.Public |
                                                 BindingFlags.DeclaredOnly)
            .Where(field =>
                Attribute.GetCustomAttribute(field, typeof(SqliteFieldAttribute)) != null)
            .ToArray();
        if (fields.Length == 0) return;

        using (SqliteCommand command = _connection.CreateCommand())
        {
            if (deleteIfExist)
            {
                command.CommandText =
                    $"DROP TABLE IF EXISTS {ModelHelpers.GetTableName<T>()}";
                command.ExecuteNonQuery();
            }

            foreach (FieldInfo mem in fields)
            {
                SqliteFieldAttribute dbAttr =
                    (SqliteFieldAttribute)Attribute.GetCustomAttribute(mem,
                        typeof(SqliteFieldAttribute))!;

                var uniqueStr = dbAttr.Unique ? "UNIQUE" : "";
                var nullableStr = dbAttr.Nullable ? "" : "NOT NULL";
                commandBuilder.Append(
                    $"{mem.Name} {dbAttr.Type} {uniqueStr} {nullableStr}");
                if (dbAttr.IsKey)
                {
                    if (!primaryKeyFound)
                    {
                        primaryKeyFound = true;
                        commandBuilder.Append(" PRIMARY KEY");

                        if (dbAttr.Autoincrement)
                        {
                            commandBuilder.Append(" AUTOINCREMENT");
                        }
                    }
                    else
                    {
                        throw new Exception(
                            $"Model '{ModelHelpers.GetTableName<T>()}' attempted to declare more than 1 primary key.");
                    }
                }

                if (dbAttr.ForeignConstraint != null)
                {
                    foreignConstraintBuilder.AppendLine(
                        $"    FOREIGN KEY({mem.Name}) REFERENCES {dbAttr.ForeignConstraint?.Item1}({dbAttr.ForeignConstraint?.Item2}),");
                }

                commandBuilder.AppendLine(",");
            }

            if (foreignConstraintBuilder.Length > 0)
            {
                commandBuilder.Append(foreignConstraintBuilder.ToString());
            }

            command.CommandText =
                $"{commandBuilder.ToString().TrimEnd(" \n\r,".ToCharArray())}\n) STRICT;";
            Console.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }

    public void DeleteRow(string table, string condition)
    {
        using (SqliteCommand command = _connection.CreateCommand())
        {
            command.CommandText = $"DELETE FROM {table} WHERE {condition};";
            command.ExecuteNonQuery();
        }
    }

    public void DeleteRow(string table, object conditions)
    {
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            commandBuilder.Append($"DELETE FROM {table} WHERE ");
            (string, object)[] conditionPairs = Utils.GetPairs(conditions);
            for (int i = 0; i < conditionPairs.Length; i++)
            {
                var (field, val) = conditionPairs[i];

                if (i > 0) commandBuilder.Append(" AND ");
                if (val != DBNull.Value)
                {
                    commandBuilder.Append($"{field} = @val{i}");
                    command.Parameters.AddWithValue($"@val{i}", val);
                }
                else
                {
                    commandBuilder.Append($"{field} IS NULL ");
                }
            }

            Debug.WriteLine(commandBuilder.ToString());
            command.CommandText = commandBuilder.ToString();
            command.ExecuteNonQuery();
        }
    }

    public void UpdateRow(string table, string condition, string updateStatement)
    {
        using (SqliteCommand command = _connection.CreateCommand())
        {
            command.CommandText = $"UPDATE {table} SET {updateStatement} WHERE {condition}";
            command.ExecuteNonQuery();
        }
    }

    // TODO: fix these 3 shenanigans
    public void UpdateRow(string table, object? conditions, object updates)
    {
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            commandBuilder.Append($"UPDATE {table}");

            (string, object)[] updatePairs = Utils.GetPairs(updates);
            Debug.Assert(updatePairs.Length > 0);

            commandBuilder.Append(" SET ")
                .AppendJoin(", ",
                    updatePairs.Select((item, index) => $"{item.Item1} = @val{index}"));
            for (int i = 0; i < updatePairs.Length; i++)
            {
                command.Parameters.AddWithValue($"@val{i}", updatePairs[i].Item2);
            }

            if (conditions != null)
            {
                (string, object)[]
                    conditionPairs =
                        Utils.GetPairs(
                            conditions); // conditions.GetPairs().Where(x => x.Item2 != null).ToArray()!;
                if (conditionPairs.Length > 0)
                {
                    commandBuilder.Append(" WHERE ")
                        .AppendJoin(" AND ",
                            conditionPairs.Select((item, index) =>
                                item.Item2 == DBNull.Value
                                    ? $"{item.Item1} IS NULL"
                                    : $"{item.Item1} = @cond{index}"));
                    for (int i = 0; i < conditionPairs.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@cond{i}",
                            conditionPairs[i].Item2);
                    }
                }
            }

            foreach (SqliteParameter x in command.Parameters)
            {
                Console.WriteLine($"{x.ParameterName} {x.DbType} {x.Value}");
            }

            command.CommandText = commandBuilder.ToString();
            Console.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }

    public void UpdateRow(object? conditions, object updates)
    {
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            commandBuilder.Append($"UPDATE {updates.GetType().Name}");

            (string, object)[] updatePairs = Utils.GetPairs(updates);
            Debug.Assert(updatePairs.Length > 0);

            commandBuilder.Append(" SET ")
                .AppendJoin(", ",
                    updatePairs.Select((item, index) => $"{item.Item1} = @val{index}"));
            for (int i = 0; i < updatePairs.Length; i++)
            {
                command.Parameters.AddWithValue($"@val{i}", updatePairs[i].Item2);
            }

            if (conditions != null)
            {
                (string, object)[]
                    conditionPairs =
                        Utils.GetPairs(
                            conditions); // conditions.GetPairs().Where(x => x.Item2 != null).ToArray()!;
                if (conditionPairs.Length > 0)
                {
                    commandBuilder.Append(" WHERE ")
                        .AppendJoin(" AND ",
                            conditionPairs.Select((item, index) =>
                                item.Item2 == DBNull.Value
                                    ? $"{item.Item1} IS NULL"
                                    : $"{item.Item1} = @cond{index}"));
                    for (int i = 0; i < conditionPairs.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@cond{i}",
                            conditionPairs[i].Item2);
                    }
                }
            }

            foreach (SqliteParameter x in command.Parameters)
            {
                Console.WriteLine($"{x.ParameterName} {x.DbType} {x.Value}");
            }

            command.CommandText = commandBuilder.ToString();
            Console.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }

    public void Add(string table, object values)
    {
        StringBuilder fieldBuilder = new();
        StringBuilder valueBuilder = new();
        var valuePairs = Utils.GetPairs(values);

        Console.WriteLine(valuePairs.Length);

        using var command = _connection.CreateCommand();

        for (var i = 0; i < valuePairs.Length; i++)
        {
            var (field, val) = valuePairs[i];
            if (i > 0) fieldBuilder.Append(", ");
            fieldBuilder.Append(field);

            if (i > 0) valueBuilder.Append(", ");
            valueBuilder.Append($"@val{i}");
            command.Parameters.AddWithValue($"@val{i}", val ?? DBNull.Value);
        }

        command.CommandText =
            $"INSERT INTO {table}({fieldBuilder.ToString()}) VALUES({valueBuilder.ToString()});";
        Console.WriteLine(command.CommandText);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            throw;
        }
    }

    public void Add(object obj)
    {
        if (obj == null) return;
        (string, object)[]
            valuePairs =
                Utils.GetPairs(
                    obj); // obj.GetPairs().Where(x => x.Item2 != null).ToArray()!;
        if (valuePairs.Length == 0) return;

        StringBuilder fieldBuilder = new();
        StringBuilder valueBuilder = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            for (int i = 0; i < valuePairs.Length; i++)
            {
                var (field, val) = valuePairs[i];
                if (i > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(field);

                if (i > 0) valueBuilder.Append(", ");
                valueBuilder.Append($"@val{i}");
                command.Parameters.AddWithValue($"@val{i}", val ?? DBNull.Value);
            }

            command.CommandText =
                $"INSERT INTO {obj.GetType().Name}({fieldBuilder.ToString()}) VALUES({valueBuilder.ToString()});";
            Console.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }

    private T parseInto<T>(SqliteDataReader reader) where T : class, new()
    {
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance |
                                                 BindingFlags.Public);
        T obj = new();
        foreach (FieldInfo field in fields)
        {
            try
            {
                object val = reader.GetValue(reader.GetOrdinal(field.Name));
                if (val != System.DBNull.Value)
                {
                    // val = Convert.ChangeType(val, field.FieldType);
                    Debug.WriteLine($"{field.Name} = {val.ToString()}");
                    field.SetValue(obj, val);
                }
            }
            catch (System.IndexOutOfRangeException ex)
            {
                Console.WriteLine(ex);
                continue;
            }
        }

        return obj;
    }

    public List<object[]> Find(string table, object? conditions = null)
    {
        List<Object[]> result = new();
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            commandBuilder.Append($"SELECT * FROM {table} ");
            if (conditions != null)
            {
                (string, object)[] conditionPairs = Utils.GetPairs(conditions);
                if (conditionPairs.Length > 0)
                {
                    commandBuilder.Append(" WHERE ")
                        .AppendJoin(" AND ",
                            conditionPairs.Select((item, index) =>
                                item.Item2 == DBNull.Value
                                    ? $"{item.Item1} IS NULL"
                                    : $"{item.Item1} = @cond{index}"));
                    for (int i = 0; i < conditionPairs.Length; i++)
                    {
                        command.Parameters.AddWithValue($"@cond{i}",
                            conditionPairs[i].Item2);
                    }
                }
            }

            Debug.WriteLine(commandBuilder.ToString());
            command.CommandText = commandBuilder.ToString();

            using (SqliteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    object[] row = new object[reader.FieldCount];
                    Debug.Assert(reader.GetValues(row) == reader.FieldCount);
                    result.Add(row);
                }
            }
        }

        return result;
    }

    public List<T> Find<T>(object? conditions = null) where T : Model, new()
    {
        List<T> result = new();
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            commandBuilder.Append($"SELECT * FROM {ModelHelpers.GetTableName<T>()} ");
            if (conditions != null)
            {
                (string, object)[] conditionPairs = Utils.GetPairs(conditions);
                if (conditionPairs.Length > 0) commandBuilder.Append("WHERE ");
                for (int i = 0; i < conditionPairs.Length; i++)
                {
                    var (field, val) = conditionPairs[i];
                    if (i > 0) commandBuilder.Append("AND ");
                    if (val != DBNull.Value)
                    {
                        commandBuilder.Append($"{field} = @val{i} ");
                        command.Parameters.AddWithValue($"@val{i}", val);
                    }
                    else
                    {
                        commandBuilder.Append($"{field} IS NULL ");
                    }
                }
            }

            command.CommandText = commandBuilder.ToString();
            Debug.WriteLine(command.CommandText);

            using (SqliteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(parseInto<T>(reader));
                }
            }
        }

        return result;
    }

    public object[]? FindOne(string table, object conditions)
    {
        object[]? result = null;
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            commandBuilder.Append($"SELECT * FROM {table} ");
            (string, object)[] conditionPairs = Utils.GetPairs(conditions);
            if (conditionPairs.Length > 0)
            {
                commandBuilder.Append(" WHERE ")
                    .AppendJoin(" AND ",
                        conditionPairs.Select((item, index) =>
                            item.Item2 == DBNull.Value
                                ? $"{item.Item1} IS NULL"
                                : $"{item.Item1} = @cond{index}"));
                for (int i = 0; i < conditionPairs.Length; i++)
                {
                    command.Parameters.AddWithValue($"@cond{i}", conditionPairs[i].Item2);
                }
            }

            Debug.WriteLine(commandBuilder.ToString());
            command.CommandText = commandBuilder.ToString();

            using (SqliteDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    object[] row = new object[reader.FieldCount];
                    Debug.Assert(reader.GetValues(row) == reader.FieldCount);
                    result = row;
                }
                else
                {
                    result = null;
                }

                if (reader.Read())
                    throw new Exception("FindOne return more than one value");
            }
        }

        return result;
    }

    public T? FindOne<T>(object conditions) where T : Model, new()
    {
        T? result = null;
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = _connection.CreateCommand())
        {
            commandBuilder.Append($"SELECT * FROM {ModelHelpers.GetTableName<T>()} ");
            (string, object)[] conditionPairs = Utils.GetPairs(conditions);
            if (conditionPairs.Length > 0)
            {
                commandBuilder.Append(" WHERE ")
                    .AppendJoin(" AND ",
                        conditionPairs.Select((item, index) =>
                            item.Item2 == DBNull.Value
                                ? $"{item.Item1} IS NULL"
                                : $"{item.Item1} = @cond{index}"));
                for (int i = 0; i < conditionPairs.Length; i++)
                {
                    command.Parameters.AddWithValue($"@cond{i}", conditionPairs[i].Item2);
                }
            }

            Debug.WriteLine(commandBuilder.ToString());
            command.CommandText = commandBuilder.ToString();

            using (SqliteDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    result = parseInto<T>(reader);
                }
                else
                {
                    result = null;
                }

                if (reader.Read())
                    throw new Exception("FindOne return more than one value");
            }
        }

        return result;
    }

    private int GenerateNewId(string table)
    {
        using (SqliteCommand command = _connection.CreateCommand())
        {
            command.CommandText = $"SELECT MAX(Id) FROM {table}";
            object result = command.ExecuteScalar() ?? 1;

            int newId = (result == DBNull.Value) ? 1 : Convert.ToInt32(result) + 1;
            return newId;
        }
    }

    public string GetPrimaryColumn(string table)
    {
        using (SqliteCommand command = _connection.CreateCommand())
        {
            command.CommandText =
                $"SELECT l.name FROM pragma_table_info(@table) as l WHERE l.pk = 1;";
            command.Parameters.AddWithValue($"@table", table);
            string result = "rowid";
            using (SqliteDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    result = reader.GetString(0);
                }

                if (reader.Read()) throw new Exception("Found more than 1 primary keys?");
            }

            return result;
        }
    }
}