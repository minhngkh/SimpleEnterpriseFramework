using HandlebarsDotNet;
using SimpleEnterpriseFramework.Data.Sqlite;
using SimpleEnterpriseFramework.Abstractions.Data;

Handlebars.RegisterHelper("convertBoolean", (writer, context, parameters) => {
    if (parameters.Length == 1 && parameters[0] is Boolean) {
        writer.WriteSafeString(parameters[0].ToString()!.ToLower());
    } else {
        throw new Exception("Required 1 boolean");
    }
});
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

if (args.Length != 3) {
    printUsage();
    return 1;
}
string directory = args[0];
if (!Path.Exists(directory)) {
    printError($"directory {directory} does not exists");
    return 1;
} else if (!File.GetAttributes(directory).HasFlag(FileAttributes.Directory)) {
    printError($"{directory} is not a directory.");
    return 1;
}
string dbFile = args[1];
if (!File.Exists(dbFile)) {
    printError($"file {dbFile} does not exists");
    return 1;
}

string projectName = args[2];
if (!generateProject(directory, projectName, dbFile)) return 1;

return 0;

bool generateProject(string directory, string projectName, string dbFile) {
    if (!System.IO.Path.IsPathRooted(directory)) {
        directory = Path.Combine(Directory.GetCurrentDirectory(), directory);
    }
    try {
        Directory.CreateDirectory(Path.Combine(directory, projectName));
        Directory.CreateDirectory(Path.Combine(directory, projectName, "Models"));
        Directory.CreateDirectory(Path.Combine(directory, projectName, "Templates"));
    } catch(Exception e) {
        printError(e.ToString());
        return false;
    }

    SqliteDriverOptions driverOption = new();
    driverOption.UsePath(dbFile);
    SqliteDriver repo = new(driverOption);
    List<string> tables = repo.ListTables().Where(x => x != "_sef_user" && x != "_sef_role").ToList();
    if (!generateModels(Path.Combine(directory, projectName, "Models"), projectName, repo, tables)) return false;
    if (!generateSetting(Path.Combine(directory, projectName), dbFile)) return false;
    if (!generateCsproj(Path.Combine(directory, projectName), projectName)) return false;
    if (!generateProgram(Path.Combine(directory, projectName), tables, projectName)) return false;

    return true;
}

bool generateModels(string modelsDirectory, string projectName, SqliteDriver repo, List<string> tables) {
    string modelTemplateText = File.ReadAllText("ModelTemplate.hbs");
    HandlebarsTemplate<object, object> modelTemplate = Handlebars.Compile(modelTemplateText);
    try {
        foreach (string tableName in tables) {
            List<ColumnInfo> columnsInfo = repo.ListColumns(tableName);
            string model = modelTemplate(new {
                modelName = tableName,
                columnsInfo = columnsInfo,
                projectName = projectName,
            });
            string path = Path.Combine(modelsDirectory, $"{tableName}.cs");
            Console.WriteLine(path);
            Console.WriteLine(model);
            File.WriteAllText(path, model);
        }
        return true;
    } catch(Exception e) {
        printError(e.ToString());
        return false;
    }
}

bool generateSetting(string directory, string dbFile) {
    try {
        string settingTemplateText = File.ReadAllText("AppSettingsTemplate.hbs");
        HandlebarsTemplate<object, object> settingTemplate = Handlebars.Compile(settingTemplateText);
        string path = Path.Combine(directory, "appsettings.json");
        dbFile = Path.GetRelativePath(directory, dbFile);
        string setting = settingTemplate(new {dbFile = dbFile});
        File.WriteAllText(path, setting);
        return true;
    } catch(Exception e) {
        printError(e.ToString());
        return false;
    }
}

bool generateCsproj(string directory, string projectName) {
    try {
        string csprojTemplateText = File.ReadAllText("CsprojTemplate.hbs");
        HandlebarsTemplate<object, object> csprojTemplate = Handlebars.Compile(csprojTemplateText);
        string path = Path.Combine(directory, $"{projectName}.csproj");
        string frameworkPath = Path.GetRelativePath(directory, "..");
        string csproj = csprojTemplate(new {frameworkPath = frameworkPath});
        File.WriteAllText(path, csproj);
        return true;
    } catch(Exception e) {
        printError(e.ToString());
        return false;
    }
}

bool generateProgram(string directory, List<string> tables, string projectName) {
    try {
        string programTemplateText = File.ReadAllText("ProgramTemplate.hbs");
        HandlebarsTemplate<object, object> programTemplate = Handlebars.Compile(programTemplateText);
        string path = Path.Combine(directory, $"Program.cs");
        string program = programTemplate(new {
            models = tables,
            projectName = projectName
        });
        File.WriteAllText(path, program);

        path = Path.Combine(directory, "Templates", $"login.hbs");
        File.Copy("login.hbs", path);
        return true;
    } catch(Exception e) {
        printError(e.ToString());
        return false;
    }
}

void printUsage() {
    Console.WriteLine("Usage: dotnet run <out_directory> <sqlite_database> <project_name>");
}

void printError(string error) {
    Console.WriteLine($"Error: {error}");
    printUsage();
}