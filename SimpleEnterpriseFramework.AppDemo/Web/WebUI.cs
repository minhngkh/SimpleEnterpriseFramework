using SEF.UI;
using SEF.Repository;
using HandlebarsDotNet;
using System.Text;
using System.IO.Pipelines;
using System.Collections;
using Newtonsoft.Json;

public class WebUI: IUI {
    IRepository repo;

    WebApplication app;
    List<string> tableNames;
    HandlebarsTemplate<object, object> tableTemplate;

    public WebUI(IRepository repo) {
        this.repo = repo;
        this.app = WebApplication.Create([]);
        string tableTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Web", "Templates", "table.hbs");
        string tableTemplate = File.ReadAllText(tableTemplatePath);
        this.tableTemplate = Handlebars.Compile(tableTemplate);
        this.tableNames = new();
    }

    public void Init() {
        Handlebars.RegisterHelper("neq", (writer, context, parameters) => {
            if (parameters.Length > 0 && parameters[0].ToString() != "Id") {
                writer.WriteSafeString(context);
            }
        });

        string formTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Web", "Templates", "form.hbs");
        string formTemplate = File.ReadAllText(formTemplatePath);
        Handlebars.RegisterTemplate("form", formTemplate);

        app.Use(next => context => {
            context.Request.EnableBuffering();
            return next(context);
        });
    }

    // public static async Task<string> GetRawBodyAsync(
    //         HttpRequest request,
    //         Encoding? encoding = null) {
    //     request.Body.Position = 0;
    //     var reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8);
    //     var body = await reader.ReadToEndAsync().ConfigureAwait(false);
    //     request.Body.Position = 0;
    //     return body;
    // }
    public async Task<string> readAllStream(PipeReader reader) {
        StringBuilder builder = new();
        int i = 0;
        UTF8Encoding encoding = new();
        ReadResult result;
        do {
            result = await reader.ReadAsync();
            builder.Append(encoding.GetString(result.Buffer));
            reader.AdvanceTo(result.Buffer.End);
            i++;
        } while(!result.IsCompleted && i < 10);
        Console.WriteLine(builder.ToString());
        return builder.ToString();
    }

    public void Register<Model, Form>(Form form) where Model: class, new()
                                                 where Form: UIForm<Model> {
        string tableName = form.TableName;
        List<string> columnNames = form.GetColumnNames();
        tableNames.Add(form.TableName);
        Console.WriteLine($"/table/{tableName}");
        app.MapGet($"/table/{tableName}", (HttpContext context) => {
            var parameters = new {
                tableName = tableName,
                columns = columnNames,
                data = form.GetAllData(),
            };
            return Results.Content(tableTemplate(parameters), "text/html");
        });

        app.MapPost($"/table/{tableName}", async (HttpContext context) => {
            string jsonString = await readAllStream(context.Request.BodyReader);
            JsonSerializer serializer = new JsonSerializer();
            Model? data = JsonConvert.DeserializeObject<Model>(jsonString);
            if (data == null) return Results.BadRequest();
            form.Add(data);
            return Results.Ok();
        });

        app.MapPatch($"/table/{tableName}", async (HttpContext context) => {
            string jsonString = await readAllStream(context.Request.BodyReader);
            JsonSerializer serializer = new JsonSerializer();
            PatchData<Model>? data = JsonConvert.DeserializeObject<PatchData<Model>>(jsonString);
            if (data == null || data.oldData == null || data.newData == null) return Results.BadRequest();
            Console.WriteLine(data.oldData);
            Console.WriteLine(data.newData);
            form.Update(data.oldData, data.newData);
            return Results.Ok();
        });

        app.MapDelete($"/table/{tableName}", async (HttpContext context) => {
            string jsonString = await readAllStream(context.Request.BodyReader);
            JsonSerializer serializer = new JsonSerializer();
            Model? data = JsonConvert.DeserializeObject<Model>(jsonString);
            Console.WriteLine(data);
            if (data == null) return Results.BadRequest();
            form.Delete(data);
            return Results.Ok();
        });
    }

    public void Start() {
        string indexTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "Web", "Templates", "index.hbs");
        string indexTemplate = File.ReadAllText(indexTemplatePath);
        HandlebarsTemplate<object, object> indexPage = Handlebars.Compile(indexTemplate);
        this.app.MapGet("/", (HttpContext context) =>
        {
            context.Response.ContentType = "text/html";
            return indexPage(new {
                tableNames = tableNames,
            });
        });
        app.Run();
    }

    class PatchData<T> where T: class {
        public T? oldData = null;
        public T? newData = null;
    }
}