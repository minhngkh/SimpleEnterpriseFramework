using HandlebarsDotNet;
using System.IO;
using System.Web;
using System.Diagnostics;
using Microsoft.Extensions.Primitives;

struct Product {
    [DbField("INTEGER", Unique = true, IsKey = true)]
    public int Id;

    [DbField("TEXT", Nullable = false)]
    public string Name;

    [DbField("REAL", Nullable = false)]
    public float Price;
}

struct User {
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

public class Program {
    public static void Main(string[] args) {
        IRepository repo = new SqliteRepository("Data Source=test.db");
        List<String> tableNames = repo.ListTables();

        repo.CreateTable<User>(true);
        repo.CreateTable<Product>(true);

        for (int i = 0; i < 15; i++) {
            repo.Add<Product>(new Product() {
                Id = i,
                Name = $"product{i}",
                Price = i*1000.0f,
            });
            repo.Add<User>(new User() {
                Id = i,
                Username = $"user{i}",
                Email = $"example{i}@gmail.com",
                Phone = i%2 == 0 ? null : String.Concat(Enumerable.Repeat($"{i}", 10)),
                Password = $"password{i}"
            });
        }

        object getTableParameters(string tableName) {
            return new {
                tableName = tableName,
                columns = repo.ListColumns(tableName).Select(colInfo => colInfo.name).ToArray(),
                data = repo.Find(tableName),
            };
        }


        var builder = WebApplication.CreateSlimBuilder(args);

        var app = builder.Build();

        string tableTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "templates", "table.hbs");
        string tableTemplate = File.ReadAllText(tableTemplatePath);
        var tablePart = Handlebars.Compile(tableTemplate);
        Handlebars.RegisterTemplate("table", tableTemplate);

        string indexTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "templates", "index.hbs");;
        string indexTemplate = File.ReadAllText(indexTemplatePath);
        var indexPage = Handlebars.Compile(indexTemplate);
        var tableData = getTableParameters("User");

        app.MapGet("/", (HttpContext context) => {
            context.Response.ContentType = "text/html";
            return indexPage(new {
                tableNames = repo.ListTables(),
                // data = tableData,
            });
        });
        app.MapGet("/table", (HttpContext context) => {
            StringValues tablename;
            context.Request.Query.TryGetValue("tableName", out tablename);
            Debug.WriteLine(tablename.ToString());
            
            if (tablename.Count > 0) {
                var parameters = getTableParameters(tablename[0]);
                return Results.Content(tablePart(parameters), "text/html");
                //return tablePart(getTableParameters(strings[0]!));
            }
            throw new Exception("Invalid usage of route /table, require query parameter 'tableName'");
        });
        app.MapGet("/add", (HttpContext context) => {
            StringValues tableName;
            context.Request.Query.TryGetValue("tableName", out tableName);

            if (tableName.Count > 0)
            {
                var rawColumns = repo.ListColumns(tableName[0]);
                //var columns = repo.ListColumns(tableName[0]);  
                var columns = rawColumns.Select(col => 
                {
                    var parts = col.name.Split(':');
                    return parts.Length > 2 ? parts[2] : col.name;
                }).ToList();
                var data = new { tableName = tableName[0], columns = columns };
                var template = Handlebars.Compile(File.ReadAllText("templates/add.hbs"));

                Console.WriteLine("Columns: " + string.Join(", ", columns));
                Console.WriteLine($"Data: tableName={data.tableName}, columns={string.Join(", ", data.columns)}");
                Console.WriteLine("Template Generated Successfully");

                return Results.Content(template(data), "text/html");
            }

            return Results.Content("Invalid table name.", "text/html");
        });
        app.MapPost("/submit", async (HttpContext context) => {
            var formData = await context.Request.ReadFormAsync();
            string tableName = formData["tableName"];

            if (string.IsNullOrEmpty(tableName))
            {
                return Results.BadRequest("Table name is missing.");
            }

            var newData = new Dictionary<string, object>();
            foreach (var column in repo.ListColumns(tableName))
            {
                if (formData.ContainsKey(column.name))
                {
                    newData[column.name] = formData[column.name].ToString();
                }
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