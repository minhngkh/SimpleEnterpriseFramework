using HandlebarsDotNet;
using SEF.Repository;

const string modelTemplateText = @"using SEF.UI;
using SEF.Repository;

public class {{modelName}} {
#pragma warning disable 0414
    {{#each columnsInfo}}
    public {{convertType this.type}}{{#if this.nullable}}?{{/if}} {{this.name}} = {{defaultValue this}};
    {{/each}}
#pragma warning restore 0414
}

public class {{modelName}}Form : UIForm<{{modelName}}> {
    public {{modelName}}Form(IRepository repo): base(repo) {
    }
}
";
HandlebarsTemplate<object, object> modelTemplate = Handlebars.Compile(modelTemplateText);
Handlebars.RegisterHelper("convertType", (writer, context, parameters) => {
    if (parameters.Length == 1) {
        string cSharpType = (parameters[0].ToString()!.ToLower()) switch {
            "text" => "string",
            "real" => "double",
            "integer" => "Int64",
            "int" => "Int64",
            _ => throw new Exception("Unreachable")
        };
        writer.WriteSafeString(cSharpType);
    } else {
        throw new Exception("Required 1 argument");
    }
});
Handlebars.RegisterHelper("defaultValue", (writer, context, parameters) => {
    if (parameters.Length == 1 && parameters[0] is ColumnInfo colInfo) {
        string defaultValue;
        if (colInfo.nullable) {
            defaultValue = "null";
        } else {
            defaultValue = (colInfo.type.ToString()!.ToLower()) switch {
                "text" => "\"\"",
                "real" => "0",
                "integer" => "0",
                "int" => "0",
                _ => throw new Exception("Unreachable")
            };
        }
        writer.WriteSafeString(defaultValue);
    } else {
        throw new Exception("Required 1 column info");
    }
});

if (args.Length != 1) {
    Console.WriteLine("Usage: dotnet run <directory>");
    return 1;
}
string directory = args[0];
if (!Path.Exists(directory)) {
    Console.WriteLine($"Error: directory {directory} does not exists.");
    Console.WriteLine("Usage: dotnet run <directory>");
    return 1;
} else if (!File.GetAttributes(directory).HasFlag(FileAttributes.Directory)) {
    Console.WriteLine($"Error: {directory} is not a directory.");
    Console.WriteLine("Usage: dotnet run <directory>");
    return 1;
}

SqliteRepository repo = new("Data Source=test.db");
foreach (string tableName in repo.ListTables()) {
    List<ColumnInfo> columnsInfo = repo.ListColumns(tableName);
    string model = modelTemplate(new {
        modelName = tableName,
        columnsInfo = columnsInfo
    });
    string path = Path.Combine(Directory.GetCurrentDirectory(), directory, $"{tableName}.cs");
    Console.WriteLine(path);
    // Console.WriteLine(model);
    File.WriteAllText(path, model);
}
return 0;