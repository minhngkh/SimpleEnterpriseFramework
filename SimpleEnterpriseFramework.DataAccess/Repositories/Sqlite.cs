using Microsoft.Data.Sqlite;
using System.Text;
using System.Data.Common;
using System.Reflection;
using System.Diagnostics;

public class SqliteUpdateCommandBuilder: IUpdateCommandBuilder {
    string? table, condition, updateStatement;
    SqliteCommand command;

    public SqliteUpdateCommandBuilder(SqliteCommand command) {
        this.command = command;
    }

    public IUpdateCommandBuilder SetTable(string table) {
        this.table = $"UPDATE {table}";
        return this;
    }

    public IUpdateCommandBuilder SetCondition(params (string, object)[] andConditions) {
        StringBuilder builder = new();
        for (int i = 0; i < andConditions.Length; i++) {
            var (field, val) = andConditions[i];
            if (i > 0) builder.Append(" AND ");
            builder.Append($"{field} = @cond{i}");
            command.Parameters.AddWithValue($"@cond{i}", val);
        }
        this.condition = $"WHERE {builder.ToString()}";
        return this;
    }

    public IUpdateCommandBuilder SetUpdateStatement(params (string, object)[] updateStatement) {
        StringBuilder builder = new();
        for (int i = 0; i < updateStatement.Length; i++) {
            var (field, val) = updateStatement[i];
            if (i > 0) builder.Append(" , ");
            builder.Append($"{field} = @val{i}");
            command.Parameters.AddWithValue($"@val{i}", val);
        }
        this.updateStatement = $"SET {builder.ToString()}";
        return this;
    }

    public void Update() {
        command.CommandText = $"{table} {updateStatement} {condition};";
        Debug.WriteLine(command.CommandText);
        command.ExecuteNonQuery();
    }

    public void Dispose() {
        command.Dispose();
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

    public void CreateTable<T>(bool deleteIfExist = false) where T: struct {
        bool primaryKeyFound = false;

        StringBuilder foreignConstraintBuilder = new();
        StringBuilder commandBuilder = new();

        commandBuilder.AppendLine($"CREATE TABLE {typeof(T).Name} (");
        MemberInfo[] fields = typeof(T).GetFields(BindingFlags.Instance |
                                                  BindingFlags.Public |
                                                  BindingFlags.DeclaredOnly);
        if (fields.Length == 0) return;

        using (SqliteCommand command = connection.CreateCommand()) {
            if (deleteIfExist) {
                command.CommandText = $"DROP TABLE IF EXISTS {typeof(T).Name}";
                command.ExecuteNonQuery();
            }
            for (int i = 0; i < fields.Length; i++) {
                MemberInfo mem = fields[i];

                DbFieldAttribute? dbAttr = (DbFieldAttribute?)Attribute.GetCustomAttribute(mem, typeof (DbFieldAttribute));
                if (dbAttr == null) {
                    throw new Exception($"Invalid model type {typeof(T).FullName}");
                } else {
                    if (dbAttr.IsKey) {
                        if (!primaryKeyFound) primaryKeyFound = true;
                        else throw new Exception($"Model '{typeof(T).Name}' attempted to declare more than 1 primary key.");
                    }
                    string uniqueStr = dbAttr.Unique ? "UNIQUE" : "";
                    string nullalbleStr = dbAttr.Nullable ? "" : "NOT NULL";
                    string key = dbAttr.IsKey ? "PRIMARY KEY" : "";

                    if (dbAttr.ForeignConstraint != null) {
                        foreignConstraintBuilder.AppendLine($"    FOREIGN KEY({mem.Name}) REFERENCES {dbAttr.ForeignConstraint?.Item1}({dbAttr.ForeignConstraint?.Item2}),");
                    }
                    commandBuilder.AppendLine($"    {mem.Name} {dbAttr.Type} {uniqueStr} {key} {nullalbleStr},");
                }
            }
            if (foreignConstraintBuilder.Length > 0) {
                commandBuilder.Append(foreignConstraintBuilder.ToString());
            }
            commandBuilder.Remove(commandBuilder.Length-2, 1); // remove trailing ','
            commandBuilder.AppendLine(") STRICT;");
            command.CommandText = commandBuilder.ToString();
            Debug.WriteLine(commandBuilder.ToString());
            command.ExecuteNonQuery();
        }
    }

    public void DeleteRow(string table, string condition) {
        using (SqliteCommand command = connection.CreateCommand()) {
            command.CommandText = $"DELETE FROM {table} WHERE {condition};";
            command.ExecuteNonQuery();
        }
    }

    public void DeleteRow(string table, params (string, object)[] andConditions) {
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"DELETE FROM {table} WHERE ");
            for (int i = 0; i < andConditions.Length; i++) {
                var (field, val) = andConditions[i];

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
            command.CommandText = $"UPDATE TABLE {table} WHERE {condition} SET {updateStatement};";
            command.ExecuteNonQuery();
        }
    }

    public IUpdateCommandBuilder UpdateRowBuilder() {
        SqliteCommand command = connection.CreateCommand();
        return new SqliteUpdateCommandBuilder(command);
    }

    public void Add(string table, params (string, object)[] values) {
        StringBuilder fieldBuilder = new();
        StringBuilder valueBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            for (int i = 0; i < values.Length; i++) {
                var (field, val) = values[i];
                if (i > 0) fieldBuilder.Append(", ");
                fieldBuilder.Append(field);

                if (i > 0) valueBuilder.Append(", ");
                valueBuilder.Append($"@val{i}");
                command.Parameters.AddWithValue($"@val{i}", val);
            }

            command.CommandText = $"INSERT INTO {table}({fieldBuilder.ToString()}) VALUES({valueBuilder.ToString()});";
            Debug.WriteLine(command.CommandText);
            command.ExecuteNonQuery();
        }
    }

    public void Add<T>(T obj) {
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance |
                                                  BindingFlags.Public |
                                                  BindingFlags.DeclaredOnly);
        if (fields.Length == 0) return;
        StringBuilder fieldBuilder = new();
        StringBuilder valueBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            for (int i = 0; i < fields.Length; i++) {
                FieldInfo field = fields[i];

                DbFieldAttribute? dbAttr = (DbFieldAttribute?)Attribute.GetCustomAttribute(field, typeof (DbFieldAttribute));
                if (dbAttr != null) {
                    object? val = field.GetValue(obj);

                    if (i > 0) fieldBuilder.Append(", ");
                    fieldBuilder.Append(field.Name);

                    if (i > 0) valueBuilder.Append(", ");
                    valueBuilder.Append($"@val{i}");
                    command.Parameters.AddWithValue($"@val{i}", val);
                }
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

    public List<object[]> Find(string table, string? condition) {
        List<Object[]> result = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            command.CommandText = $"SELECT * FROM {table}";
            if (condition != null) {
                command.CommandText += $"WHERE {condition}";
            }
            command.CommandText += ";";

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

    public List<T> Find<T>(string? condition) where T: struct {
        List<T> result = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            command.CommandText = $"SELECT * FROM {typeof(T).Name} ";
            if (condition != null) {
                command.CommandText += $"WHERE {condition}";
            }
            command.CommandText += ";";
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

    public List<object[]> Find(string table, params (string, object)[] andConditions) {
        List<Object[]> result = new();
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"SELECT * FROM {table} ");
            if (andConditions.Length > 0) {
                commandBuilder.Append("WHERE ");
                for (int i = 0; i < andConditions.Length; i++) {
                    var (field, val) = andConditions[i];
                    if (i > 0) commandBuilder.Append("AND ");
                    commandBuilder.Append($"{field} = @val{i} ");
                    command.Parameters.AddWithValue($"@val{i}", val);
                }
                commandBuilder.Append(";");
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

    public List<T> Find<T>(params (string, object)[] andConditions) where T: struct {
        List<T> result = new();
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"SELECT * FROM {typeof(T).Name} ");
            if (andConditions.Length > 0) {
                commandBuilder.Append("WHERE ");
                for (int i = 0; i < andConditions.Length; i++) {
                    var (field, val) = andConditions[i];
                    if (i > 0) commandBuilder.Append("AND ");
                    commandBuilder.Append($"{field} = 123 ");
                    command.Parameters.AddWithValue($"@val{i}", val);
                }
                commandBuilder.Append(";");
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

    public object[]? FindOne(string table, params (string, object)[] andConditions) {
        object[]? result = null;
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"SELECT * FROM {table} ");
            if (andConditions.Length > 0) {
                commandBuilder.Append("WHERE ");
                for (int i = 0; i < andConditions.Length; i++) {
                    var (field, val) = andConditions[i];
                    if (i > 0) commandBuilder.Append("AND ");
                    commandBuilder.Append($"{field} = @val{i} ");
                    command.Parameters.AddWithValue($"@val{i}", val);
                }
                commandBuilder.Append(";");
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

    public T? FindOne<T>(params (string, object)[] andConditions) where T: struct {
        T? result = null;
        StringBuilder commandBuilder = new();
        using (SqliteCommand command = connection.CreateCommand()) {
            commandBuilder.Append($"SELECT * FROM {typeof(T).Name} ");
            if (andConditions.Length > 0) {
                commandBuilder.Append("WHERE ");
                for (int i = 0; i < andConditions.Length; i++) {
                    var (field, val) = andConditions[i];
                    if (i > 0) commandBuilder.Append("AND ");
                    commandBuilder.Append($"{field} = @val{i} ");
                    command.Parameters.AddWithValue($"@val{i}", val);
                }
                commandBuilder.Append(";");
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
}
