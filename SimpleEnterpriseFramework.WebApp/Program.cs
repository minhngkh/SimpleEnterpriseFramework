using HandlebarsDotNet;
using System.IO;
using System.Web;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Primitives;

struct Product
{
    [DbField("INTEGER", Unique = true, IsKey = true)]
    public int Id;

    [DbField("TEXT", Nullable = false)]
    public string Name;

    [DbField("REAL", Nullable = false)]
    public float Price;
}

struct User
{
    [DbField("INTEGER", Unique = true, IsKey = true)]
    public int Id;

    [DbField("TEXT", Unique = true, Nullable = false)]
    public string Username;

    [DbField("TEXT", Unique = true, Nullable = false)]
    public string Email;

    [DbField("TEXT")]
    public string? Phone;

    [DbField("TEXT", Nullable = false)]
    public string Password;
}

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}

public class Program
{
    public static void Main(string[] args)
    {
        IRepository repo = new SqliteRepository("Data Source=test.db");
        List<String> tableNames = repo.ListTables();

        repo.CreateTable<User>(true);
        repo.CreateTable<Product>(true);

        for (int i = 0; i < 15; i++)
        {
            repo.Add<Product>(new Product()
            {
                Id = i,
                Name = $"product{i}",
                Price = i * 1000.0f,
            });
            repo.Add<User>(new User()
            {
                Id = i,
                Username = $"user{i}",
                Email = $"example{i}@gmail.com",
                Phone = i % 2 == 0 ? null : String.Concat(Enumerable.Repeat($"{i}", 10)),
                Password = $"password{i}"
            });
        }

        object getTableParameters(string tableName)
        {
            return new
            {
                tableName = tableName,
                columns = repo.ListColumns(tableName).Select(colInfo => colInfo.name).ToArray(),
                data = repo.Find(tableName),
            };
        }

        var builder = WebApplication.CreateSlimBuilder(args);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0,
                AppJsonSerializerContext.Default);
        });

        WebApplication app = builder.Build();

        string tableTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "templates", "table.hbs");
        string tableTemplate = File.ReadAllText(tableTemplatePath);
        var tablePart = Handlebars.Compile(tableTemplate);
        Handlebars.RegisterTemplate("table", tableTemplate);

        // Register 'neq' helper
        Handlebars.RegisterHelper("neq", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && parameters[0].ToString() != "Id")
            {
                writer.WriteSafeString(context);  // Render the content if condition is true
            }
        });

        string indexTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "templates", "index.hbs");
        string indexTemplate = File.ReadAllText(indexTemplatePath);
        var indexPage = Handlebars.Compile(indexTemplate);
        var tableData = getTableParameters("User");

        string formTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "templates", "form.hbs");
        string formTemplate = File.ReadAllText(formTemplatePath);
        Handlebars.RegisterTemplate("form", formTemplate);

        app.MapGet("/", (HttpContext context) =>
        {
            context.Response.ContentType = "text/html";
            return indexPage(new
            {
                tableNames = repo.ListTables(),
            });
        });

        app.MapGet("/table", (HttpContext context) =>
        {
            StringValues tableName = context.Request.Query["tableName"];
            if (!StringValues.IsNullOrEmpty(tableName))
            {
                Debug.WriteLine(tableName.ToString());
                var parameters = getTableParameters(tableName[0]!);
                return Results.Content(tablePart(parameters), "text/html");
            }
            return Results.BadRequest("Table name is missing.");
        });

        app.MapGet("/add", (HttpContext context) =>
        {
            StringValues tableName = context.Request.Query["tableName"];
            if (!StringValues.IsNullOrEmpty(tableName))
            {
                var rawColumns = repo.ListColumns(tableName[0]!);
                var columns = rawColumns.Select(col =>
                {
                    var parts = col.name.Split(':');
                    return parts.Length > 2 ? parts[2] : col.name;
                }).ToList();
                var data = new { tableName = tableName[0], columns = columns };
                var template = Handlebars.Compile(File.ReadAllText("templates/add.hbs"));

                return Results.Content(template(data), "text/html");
            }

            return Results.BadRequest("Table name is missing.");
        });

        app.MapPost("/submit", async (HttpContext context) =>
        {
            var formData = await context.Request.ReadFormAsync();
            string? tableName = formData["tableName"];

            if (string.IsNullOrEmpty(tableName))
            {
                return Results.BadRequest("Table name is missing.");
            }

            var newData = new Dictionary<string, object>();
            var columns = repo.ListColumns(tableName);

            Console.WriteLine("Columns in table:");
            fapp.MapPost("/submit", async (HttpContext context) =>
        {
            var formData = await context.Request.ReadFormAsync();
            string? tableName = formData["tableName"];

            if (string.IsNullOrEmpty(tableName))
            {
                return Results.BadRequest("Table name is missing.");
            }

            var newData = new Dictionary<string, object>();
            var columns = repo.ListColumns(tableName);

            Console.WriteLine("Columns in table:");
            foreach (var column in columns)
            {
                Console.WriteLine($"{column.name} (Type: {column.type})");
            }

            // Add columns to newData, skip Id if not provided
            foreach (var column in columns)
            {
                if (column.name == "Id" && !formData.ContainsKey("Id"))
                {
                    // Skip adding Id if it's not provided
                    continue;
                }

                if (formData.ContainsKey(column.name))
                {
                    var value = formData[column.name].ToString();
                    if (column.type == "INTEGER" && int.TryParse(value, out var intValue))
                    {
                        newData[column.name] = intValue;
                    }
                    else if (column.type == "REAL" && float.TryParse(value, out var floatValue))
                    {
                        newData[column.name] = floatValue;
                    }
                    else
                    {
                        newData[column.name] = value;
                    }
                }
                else
                {
                    newData[column.name] = null; // Handle nullable columns
                }
            }

            Console.WriteLine("Received data to insert:");
            foreach (var kvp in newData)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }

            // Check for parameter mismatch
            if (newData.Count != columns.Count(col => col.name != "Id"))
            {
                return Results.BadRequest("Parameter count mismatch.");
            }

            try
            {
                repo.Add(tableName, newData);
                return Results.Redirect($"/table?tableName={tableName}");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error adding data: {ex.Message}");
            }
        });


        app.Run();
    }
}
