using Microsoft.Data.Sqlite;
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

    public void CreateTable<T>(bool deleteIfExist = false) where T : struct {
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

        using (SqliteCommand command = connection.CreateCommand())
        {
            if (deleteIfExist)
            {
                command.CommandText = $"DROP TABLE IF EXISTS {typeof(T).Name}";
                command.ExecuteNonQuery();
            }

            foreach (FieldInfo mem in fields)
            {
                DbFieldAttribute dbAttr = (DbFieldAttribute)Attribute.GetCustomAttribute(mem, typeof(DbFieldAttribute))!;
                string columnDefinition;

                if (dbAttr.IsKey)
                {
                    if (!primaryKeyFound)
                    {
                        primaryKeyFound = true;
                        // Define the primary key as auto-incrementing
                        columnDefinition = $"{mem.Name} INTEGER PRIMARY KEY AUTOINCREMENT";
                    }
                    else
                    {
                        throw new Exception($"Model '{typeof(T).Name}' attempted to declare more than 1 primary key.");
                    }
                }
                else
                {
                    string uniqueStr = dbAttr.Unique ? "UNIQUE" : "";
                    string nullableStr = dbAttr.Nullable ? "" : "NOT NULL";
                    columnDefinition = $"{mem.Name} {dbAttr.Type} {uniqueStr} {nullableStr}";
                }

                if (dbAttr.ForeignConstraint != null)
                {
                    foreignConstraintBuilder.AppendLine($"    FOREIGN KEY({mem.Name}) REFERENCES {dbAttr.ForeignConstraint?.Item1}({dbAttr.ForeignConstraint?.Item2}),");
                }

                commandBuilder.AppendLine($"    {columnDefinition},");
            }

            if (foreignConstraintBuilder.Length > 0)
            {
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

    public void UpdateRow(string table, object? conditions, object updates) {
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"UPDATE {table}");

            (string, object)[] updatePairs = Utils.GetPairs(updates);
            Debug.Assert(updatePairs.Length > 0);

            commandBuilder.Append(" SET ")
                          .AppendJoin(", ", updatePairs.Select((item, index) => $"{item.Item1} = @val{index}"));
            for (int i = 0; i < updatePairs.Length; i++) {
                command.Parameters.AddWithValue($"@val{i}", updatePairs[i].Item2);
            }

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

            command.CommandText = commandBuilder.ToString();
            Debug.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }

    public void Add(string table, object values)
    {
        StringBuilder fieldBuilder = new();
        StringBuilder valueBuilder = new();
        (string, object)[] valuePairs = Utils.GetPairs(values);

        using (SqliteCommand command = connection.CreateCommand())
        {
            bool hasIdField = valuePairs.Any(pair => pair.Item1 == "Id");
            object idValue = valuePairs.FirstOrDefault(pair => pair.Item1 == "Id").Item2;

            if (!hasIdField || idValue == null || idValue.Equals(0))
            {
                int newId = GenerateNewId(table);
                valuePairs = valuePairs.Append(("Id", newId)).ToArray();
            }

            // Prepare the fields and value placeholders
            for (int i = 0; i < valuePairs.Length; i++)
            {
                var (field, val) = valuePairs[i];
                if (i > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(field);

                if (i > 0) valueBuilder.Append(", ");
                valueBuilder.Append($"@val{i}");
                command.Parameters.AddWithValue($"@val{i}", val ?? DBNull.Value);
            }

            // Debugging SQL command string and parameters
            Console.WriteLine("SQL Command Text: " + command.CommandText);
            Console.WriteLine("SQL Command Parameters:");
            foreach (DbParameter param in command.Parameters)
            {
                Console.WriteLine($"{param.ParameterName}: {param.Value}");
            }

            // Final command text with values
            command.CommandText = $"INSERT INTO {table}({fieldBuilder.ToString()}) VALUES({valueBuilder.ToString()});";
            
            try
            {
                // Execute the query
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Handle any exceptions during query execution
                Console.WriteLine("Error: " + ex.Message);
                throw;
            }
        }
    }

    public void Add<T>(T obj)
    {
        // Get all fields with the DbFieldAttribute that are not null
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance |
                                                BindingFlags.Public |
                                                BindingFlags.DeclaredOnly)
                                    .Where(field => Attribute.GetCustomAttribute(field, typeof(DbFieldAttribute)) != null &&
                                                    field.GetValue(obj) != null)
                                    .ToArray();

        if (fields.Length == 0) return;

        StringBuilder fieldBuilder = new();
        StringBuilder valueBuilder = new();
        using (SqliteCommand command = connection.CreateCommand())
        {
            bool hasIdField = false;
            object idValue = null;

            // Check if the 'Id' field is part of the object and if it's set to 0 or null
            foreach (var field in fields)
            {
                if (field.Name == "Id")
                {
                    hasIdField = true;
                    idValue = field.GetValue(obj);
                    break;
                }
            }

            // Exclude the 'Id' field from insertion if it's null or 0
            var filteredFields = fields.Where(field => !(field.Name == "Id" && (idValue == null || idValue.Equals(0)))).ToArray();

            // Build the field and value parts of the query
            for (int i = 0; i < filteredFields.Length; i++)
            {
                FieldInfo field = filteredFields[i];
                object val = field.GetValue(obj);

                if (i > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(field.Name);

                if (i > 0) valueBuilder.Append(", ");
                valueBuilder.Append($"@val{i}");
                command.Parameters.AddWithValue($"@val{i}", val);
            }

            command.CommandText = $"INSERT INTO {typeof(T).Name}({fieldBuilder.ToString()}) VALUES({valueBuilder.ToString()});";
            Debug.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }


    private void parseInto<T>(SqliteDataReader reader, ref T obj) {
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance |
                                                  BindingFlags.Public);
        foreach (FieldInfo field in fields) {
            DbFieldAttribute? dbAttr = (DbFieldAttribute?)Attribute.GetCustomAttribute(field, typeof (DbFieldAttribute));
            if (dbAttr != null) {
                object val = reader.GetValue(reader.GetOrdinal(field.Name));
                val = Convert.ChangeType(val, field.FieldType);
                Debug.WriteLine($"{field.Name} = {val.ToString()}");
                object boxedObj = obj!;
                field.SetValue(boxedObj, val);
                obj = (T)boxedObj;
            }
        }
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

    public List<T> Find<T>(object? conditions = null) where T: struct {
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
                    T obj = new();
                    parseInto<T>(reader, ref obj);
                    result.Add(obj);
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

    public T? FindOne<T>(object conditions) where T: struct {
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
                    T obj = new();
                    parseInto<T>(reader, ref obj);
                    result = obj;
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
            object result = command.ExecuteScalar();

            int newId = (result == DBNull.Value) ? 1 : Convert.ToInt32(result) + 1;
            return newId;
        }
    }
}