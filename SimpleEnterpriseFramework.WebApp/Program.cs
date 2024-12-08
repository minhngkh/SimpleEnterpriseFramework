using HandlebarsDotNet;
using System.IO;
using System.Web;
using System.Text;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Primitives;
using Microsoft.Data.Sqlite;


#nullable enable

struct Product
{
    [DbField("INTEGER", Unique = true, IsKey = true)]
    public int? Id = null; // Field, not a property

    [DbField("TEXT", Nullable = false)]
    public string name; // Field, not a property

    [DbField("REAL", Nullable = false)]
    public float price; // Field, not a property

    public Product(string name, float price) : this()
    {
        this.name = name;
        this.price = price;
    }
}

struct User
{
    [DbField("INTEGER", Unique = true, IsKey = true)]
    public int? Id = null; // Field, not a property

    [DbField("TEXT", Unique = true, Nullable = false)]
    public string username; // Field, not a property

    [DbField("TEXT", Unique = true, Nullable = false)]
    public string email; // Field, not a property

    [DbField("TEXT")]
    public string? phone; // Field, not a property

    [DbField("TEXT", Nullable = false)]
    public string password; // Field, not a property

    public User(string username, string email, string? phone, string password) : this()
    {
        this.username = username;
        this.email = email;
        this.phone = phone;
        this.password = password;
    }
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
        List<string> tableNames = repo.ListTables();

        repo.CreateTable<User>(true);
        repo.CreateTable<Product>(true);

        for (int i = 0; i < 16; i++)
        {
            repo.Add<Product>(new Product(
                name: $"product{i}",
                price: i * 1000.0f
            ));
            repo.Add<User>(new User(
                username: $"user{i}",
                email: $"example{i}@gmail.com",
                phone: i % 2 == 0 ? null : new string($"{i}"[0], 10),
                password: $"password{i}"
            ));
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
                writer.WriteSafeString(context);
            }
        });

        string indexTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "templates", "index.hbs");
        string indexTemplate = File.ReadAllText(indexTemplatePath);
        var indexPage = Handlebars.Compile(indexTemplate);

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
                var parameters = getTableParameters(tableName[0]!);
                return Results.Content(tablePart(parameters), "text/html");
            }
            return Results.BadRequest("Table name is missing.");
        });

        app.MapPost("/update", async (HttpContext context) =>
        {
            var formData = await context.Request.ReadFormAsync();
            string? tableName = formData["tableName"];
            if (string.IsNullOrEmpty(tableName)) return Results.BadRequest("Table name is missing");
            var updateData = new Dictionary<string, object?>();

            // List all columns from the table
            var columns = repo.ListColumns(tableName);
            Console.WriteLine("Columns: " + string.Join(", ", columns));
            if (!formData.ContainsKey("Id")) return Results.BadRequest("Id is missing");

            int id = 0;
            try {
                id = int.Parse(formData["Id"].ToString());
            } catch (Exception _) {
                return Results.BadRequest($"Invalid id {formData["Id"].ToString()}");
            }

            // Collect the form data for each column
            foreach (var column in columns)
            {
                if (formData.ContainsKey(column.name))
                {
                    var value = formData[column.name].ToString();
                    if (column.name == "Id" && string.IsNullOrEmpty(value)) continue;
                    updateData[column.name] = value;
                }
                else if (column.name != "Id") // Skip Id if not provided
                {
                    // Set columns that are missing (except Id) to null
                    updateData[column.name] = null;
                }
            }

            // Printing updateData with detailed key-value pairs
            Console.WriteLine("Data:");
            foreach (var item in updateData) Console.WriteLine($"{item.Key}: {item.Value}");
            Console.WriteLine($"Id: {id}");

            try
            {
                repo.UpdateRow(tableName, new {Id = id}, updateData);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Results.Problem($"Error adding data: {ex.Message}");
            }
        });

        app.MapPost("/create", async (HttpContext context) =>
        {
            var formData = await context.Request.ReadFormAsync();
            string? tableName = formData["tableName"];

            if (string.IsNullOrEmpty(tableName))
            {
                return Results.BadRequest("Table name is missing.");
            }

            var newData = new Dictionary<string, object?>();

            // List all columns from the table
            var columns = repo.ListColumns(tableName);
            Console.WriteLine("Columns: " + string.Join(", ", columns));

            // Collect the form data for each column
            foreach (var column in columns)
            {
                if (formData.ContainsKey(column.name))
                {
                    var value = formData[column.name].ToString();
                    if (column.name == "Id" && string.IsNullOrEmpty(value))
                    {
                        // Skip the 'Id' field, SQLite will auto-increment it
                        continue;
                    }
                    newData[column.name] = value;
                }
                else if (column.name != "Id") // Skip Id if not provided
                {
                    // Set columns that are missing (except Id) to null
                    newData[column.name] = null;
                }
            }

            // Printing newData with detailed key-value pairs
            Console.WriteLine("Data:");
            foreach (var item in newData)
            {
                Console.WriteLine($"{item.Key}: {item.Value}");
            }

            try
            {
                // Call the appropriate Add method, where repo handles auto-increment of Id
                repo.Add(tableName, newData);
                return Results.Ok(200);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Results.Problem($"Error adding data: {ex.Message}");
            }
        });
        app.MapPost("/delete", async (HttpContext context) =>
        {
            var formData = await context.Request.ReadFormAsync();
            string? tableName = formData["tableName"];

            if (string.IsNullOrEmpty(tableName))
            {
                return Results.BadRequest("Table name is missing.");
            }

            if (!formData.ContainsKey("Id"))
            {
                return Results.BadRequest("Id is missing.");
            }

            int id = int.Parse(formData["Id"].ToString());

            try
            {
                repo.DeleteRow(tableName, new { Id = id });
                return Results.Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Results.Problem($"Error deleting data: {ex.Message}");
            }
        });

        app.Run();
    }
}
