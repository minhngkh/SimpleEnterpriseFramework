using SEF.Repository;
using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Data.Common;
using System.Reflection;
using System.Diagnostics;

static class Utils {
    public static (string, object)[] GetPairs(object conditions)
    {
        if (conditions is IDictionary<string, object> dictionary)
        {
            return dictionary.Where(pair => pair.Value != null)
                            .Select(pair => (pair.Key, pair.Value))
                            .ToArray();
        }
        else
        {
            Type type = conditions.GetType();
            return type.GetProperties()
                .Select(p => (p.Name, p.GetValue(conditions) ?? DBNull.Value)) // Handle null by using DBNull.Value
                .ToArray();
        }
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class DbFieldAttribute: Attribute {
    public string Type;
    public bool Unique = false;
    public bool IsKey = false;
    public bool Nullable = true;
    public bool Autoincrement = false;
    public (string, string)? ForeignConstraint = null;
    static string[] allTypes = {"TEXT", "INTEGER", "REAL", "BLOB", "ANY"};
    public DbFieldAttribute(string type) {
        this.Type = type;
        if (!allTypes.Contains(type.ToUpper())) {
            throw new Exception($"Invalid SqliteType {type}, must be one of the following: TEXT, INTEGER, REAL, BLOB, ANY.");
        }
    }
    public DbFieldAttribute(string type, string referencedTable, string referencedField) {
        this.ForeignConstraint = (referencedTable, referencedField);
        this.Type = type;
        if (!allTypes.Contains(type.ToUpper())) {
            throw new Exception($"Invalid SqliteType {type}, must be one of the following: TEXT, INTEGER, REAL, BLOB, ANY.");
        }
    }
    public override string ToString() => $"[Type = {Type}, Unique = {Unique.ToString()}, IsKey = {IsKey.ToString()}]";
}

public class SqliteRepository: IRepository {
    SqliteConnection connection;

    public SqliteRepository(string connection) {
        this.connection = new(connection);
        this.connection.Open();
    }

    public List<string> ListTables() {
        List<string> result = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            command.CommandText = @"SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
            using (SqliteDataReader reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    Debug.Assert(reader.GetName(0) == "name");
                    result.Add(reader.GetString(0));
                }
            }
        }
        return result;
    }

    public List<ColumnInfo> ListColumns(string table) {
        List<ColumnInfo> result = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            command.CommandText = $"PRAGMA table_info(\"{table}\")";
            using (SqliteDataReader reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    result.Add(new ColumnInfo(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
                }
            }
        }
        return result;
    }

    public void CreateTable<T>(bool deleteIfExist = false) {
        bool primaryKeyFound = false;

        StringBuilder foreignConstraintBuilder = new();
        StringBuilder commandBuilder = new();

        commandBuilder.AppendLine($"CREATE TABLE {typeof(T).Name} (");
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance |
                                                BindingFlags.Public |
                                                BindingFlags.DeclaredOnly)
                                    .Where(field => Attribute.GetCustomAttribute(field, typeof(DbFieldAttribute)) != null)
                                    .ToArray();
        if (fields.Length == 0) return;

        using (SqliteCommand command = connection.CreateCommand()) {
            if (deleteIfExist) {
                command.CommandText = $"DROP TABLE IF EXISTS {typeof(T).Name}";
                command.ExecuteNonQuery();
            }

            foreach (FieldInfo mem in fields) {
                DbFieldAttribute dbAttr = (DbFieldAttribute)Attribute.GetCustomAttribute(mem, typeof(DbFieldAttribute))!;

                string uniqueStr = dbAttr.Unique ? "UNIQUE" : "";
                string nullableStr = dbAttr.Nullable ? "" : "NOT NULL";
                string autoincrementStr = dbAttr.Autoincrement ? "AUTOINCREMENT" : "";
                commandBuilder.Append($"{mem.Name} {dbAttr.Type} {uniqueStr} {nullableStr} {autoincrementStr}");
                if (dbAttr.IsKey) {
                    if (!primaryKeyFound) {
                        primaryKeyFound = true;
                        commandBuilder.Append(" PRIMARY KEY");
                    } else {
                        throw new Exception($"Model '{typeof(T).Name}' attempted to declare more than 1 primary key.");
                    }
                }

                if (dbAttr.ForeignConstraint != null) {
                    foreignConstraintBuilder.AppendLine($"    FOREIGN KEY({mem.Name}) REFERENCES {dbAttr.ForeignConstraint?.Item1}({dbAttr.ForeignConstraint?.Item2}),");
                }
                commandBuilder.AppendLine();
            }

            if (foreignConstraintBuilder.Length > 0) {
                commandBuilder.Append(foreignConstraintBuilder.ToString());
            }

            command.CommandText = $"{commandBuilder.ToString().TrimEnd(" \n\r,".ToCharArray())}\n) STRICT;";
            Debug.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }

    public void DeleteRow(string table, string condition) {
        using (SqliteCommand command = connection.CreateCommand()) {
            command.CommandText = $"DELETE FROM {table} WHERE {condition};";
            command.ExecuteNonQuery();
        }
    }

    public void DeleteRow(string table, object conditions) {
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"DELETE FROM {table} WHERE ");
            (string, object)[] conditionPairs = Utils.GetPairs(conditions);
            for (int i = 0; i < conditionPairs.Length; i++) {
                var (field, val) = conditionPairs[i];

                if (i > 0) commandBuilder.Append(" AND ");
                commandBuilder.Append($"{field} = @val{i}");
                command.Parameters.AddWithValue($"@val{i}", val);
            }
            Debug.WriteLine(commandBuilder.ToString());
            command.CommandText = commandBuilder.ToString();
            command.ExecuteNonQuery();
        }
    }

    public void UpdateRow(string table, string condition, string updateStatement) {
        using (SqliteCommand command = connection.CreateCommand()) {
            command.CommandText = $"UPDATE {table} SET {updateStatement} WHERE {condition}";
            command.ExecuteNonQuery();
        }
    }

    public void UpdateRow<T>(T? conditions, T updates) where T: class, IModel {
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"UPDATE {updates.TableName}");

            (string, object)[] updatePairs = updates.GetPairs().Where(x => x.Item2 != null).ToArray()!;
            Debug.Assert(updatePairs.Length > 0);

            commandBuilder.Append(" SET ")
                          .AppendJoin(", ", updatePairs.Select((item, index) => $"{item.Item1} = @val{index}"));
            for (int i = 0; i < updatePairs.Length; i++) {
                command.Parameters.AddWithValue($"@val{i}", updatePairs[i].Item2);
            }

            if (conditions != null) {
                (string, object)[] conditionPairs = conditions.GetPairs().Where(x => x.Item2 != null).ToArray()!;
                if (conditionPairs.Length > 0) {
                    commandBuilder.Append(" WHERE ")
                                  .AppendJoin(" AND ", conditionPairs.Select((item, index) => $"{item.Item1} = @cond{index}"));
                    for (int i = 0; i < conditionPairs.Length; i++) {
                        command.Parameters.AddWithValue($"@cond{i}", conditionPairs[i].Item2);
                    }
                }
            }
            foreach (SqliteParameter x in command.Parameters) {
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
        (string, object)[] valuePairs = Utils.GetPairs(values);

        using (SqliteCommand command = connection.CreateCommand()) {
            for (int i = 0; i < valuePairs.Length; i++) {
                var (field, val) = valuePairs[i];
                if (i > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(field);

                if (i > 0) valueBuilder.Append(", ");
                valueBuilder.Append($"@val{i}");
                command.Parameters.AddWithValue($"@val{i}", val ?? DBNull.Value);
            }

            command.CommandText = $"INSERT INTO {table}({fieldBuilder.ToString()}) VALUES({valueBuilder.ToString()});";
            Console.WriteLine(command.CommandText);

            try {
                command.ExecuteNonQuery();
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                throw;
            }
        }
    }

    public void Add(IModel obj) {
        if (obj == null) return;
        (string, object?)[] valuePairs = obj.GetPairs();
        if (valuePairs.Length == 0) return;

        StringBuilder fieldBuilder = new();
        StringBuilder valueBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            for (int i = 0; i < valuePairs.Length; i++) {
                var (field, val) = valuePairs[i];
                if (val == null) continue;
                if (i > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(field);

                if (i > 0) valueBuilder.Append(", ");
                valueBuilder.Append($"@val{i}");
                command.Parameters.AddWithValue($"@val{i}", val ?? DBNull.Value);
            }

            command.CommandText = $"INSERT INTO {obj.TableName}({fieldBuilder.ToString()}) VALUES({valueBuilder.ToString()});";
            Console.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }

    private T parseInto<T>(SqliteDataReader reader) where T: class, new() {
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance |
                                                  BindingFlags.Public);
        T obj = new();
        foreach (FieldInfo field in fields) {
            DbFieldAttribute? dbAttr = (DbFieldAttribute?)Attribute.GetCustomAttribute(field, typeof (DbFieldAttribute));
            if (dbAttr != null) {
                object val = reader.GetValue(reader.GetOrdinal(field.Name));
                if (val != System.DBNull.Value) {
                    // val = Convert.ChangeType(val, field.FieldType);
                    Debug.WriteLine($"{field.Name} = {val.ToString()}");
                    field.SetValue(obj, val);
                }
            }
        }
        return obj;
    }

    public List<object[]> Find(string table, object? conditions = null) {
        List<Object[]> result = new();
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"SELECT * FROM {table} ");
            if (conditions != null) {
                (string, object)[] conditionPairs = Utils.GetPairs(conditions);
                if (conditionPairs.Length > 0) {
                    commandBuilder.Append(" WHERE ")
                                  .AppendJoin(" AND ", conditionPairs.Select((item, index) => $"{item.Item1} = @cond{index}"));
                    for (int i = 0; i < conditionPairs.Length; i++) {
                        command.Parameters.AddWithValue($"@cond{i}", conditionPairs[i].Item2);
                    }
                }
            }
            Debug.WriteLine(commandBuilder.ToString());
            command.CommandText = commandBuilder.ToString();

            using (SqliteDataReader reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    object[] row = new object[reader.FieldCount];
                    Debug.Assert(reader.GetValues(row) == reader.FieldCount);
                    result.Add(row);
                }
            }
        }
        return result;
    }

    public List<T> Find<T>(object? conditions = null) where T: class, new() {
        List<T> result = new();
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"SELECT * FROM {typeof(T).Name} ");
            if (conditions != null) {
                (string, object)[] conditionPairs = Utils.GetPairs(conditions);
                if (conditionPairs.Length > 0) commandBuilder.Append("WHERE ");
                for (int i = 0; i < conditionPairs.Length; i++) {
                    var (field, val) = conditionPairs[i];
                    if (i > 0) commandBuilder.Append("AND ");
                    commandBuilder.Append($"{field} = @val{i} ");
                    command.Parameters.AddWithValue($"@val{i}", val);
                }
            }
            command.CommandText = commandBuilder.ToString();
            Debug.WriteLine(command.CommandText);

            using (SqliteDataReader reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    result.Add(parseInto<T>(reader));
                }
            }
        }
        return result;
    }

    public object[]? FindOne(string table, object conditions) {
        object[]? result = null;
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"SELECT * FROM {table} ");
            (string, object)[] conditionPairs = Utils.GetPairs(conditions);
            if (conditionPairs.Length > 0) {
                commandBuilder.Append(" WHERE ")
                              .AppendJoin(" AND ", conditionPairs.Select((item, index) => $"{item.Item1} = @cond{index}"));
                for (int i = 0; i < conditionPairs.Length; i++) {
                    command.Parameters.AddWithValue($"@cond{i}", conditionPairs[i].Item2);
                }
            }
            Debug.WriteLine(commandBuilder.ToString());
            command.CommandText = commandBuilder.ToString();

            using (SqliteDataReader reader = command.ExecuteReader()) {
                if (reader.Read()) {
                    object[] row = new object[reader.FieldCount];
                    Debug.Assert(reader.GetValues(row) == reader.FieldCount);
                    result = row;
                } else {
                    result = null;
                }
                if (reader.Read()) throw new Exception("FindOne return more than one value");
            }
        }
        return result;
    }

    public T? FindOne<T>(object conditions) where T: class, new() {
        T? result = null;
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"SELECT * FROM {typeof(T).Name} ");
            (string, object)[] conditionPairs = Utils.GetPairs(conditions);
            if (conditionPairs.Length > 0) {
                commandBuilder.Append(" WHERE ")
                    .AppendJoin(" AND ", conditionPairs.Select((item, index) => $"{item.Item1} = @cond{index}"));
                for (int i = 0; i < conditionPairs.Length; i++) {
                    command.Parameters.AddWithValue($"@cond{i}", conditionPairs[i].Item2);
                }
            }
            Debug.WriteLine(commandBuilder.ToString());
            command.CommandText = commandBuilder.ToString();

            using (SqliteDataReader reader = command.ExecuteReader()) {
                if (reader.Read()) {
                    result = parseInto<T>(reader);
                } else {
                    result = null;
                }
                if (reader.Read()) throw new Exception("FindOne return more than one value");
            }
        }
        return result;
    }

    private int GenerateNewId(string table)
    {
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT MAX(Id) FROM {table}";
            object result = command.ExecuteScalar()??1;

            int newId = (result == DBNull.Value) ? 1 : Convert.ToInt32(result) + 1;
            return newId;
        }
    }

    public string GetPrimaryColumn(string table) {
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT l.name FROM pragma_table_info(@table) as l WHERE l.pk = 1;";
            command.Parameters.AddWithValue($"@table", table);
            string result = "rowid";
            using (SqliteDataReader reader = command.ExecuteReader()) {
                if (reader.Read()) {
                    result = reader.GetString(0);
                }
                if (reader.Read()) throw new Exception("Found more than 1 primary keys?");
            }
            return result;
        }
    }
}