using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.Helpers.Enums;
using Newtonsoft.Json;
using SimpleEnterpriseFramework.Abstractions.App;
using SimpleEnterpriseFramework.Abstractions.Data;

namespace SimpleEnterpriseFramework.App.Web;

public class WebApp : CrudApp
{
    public WebApplication App => app;
    
    
    private WebAppOptions _options;
    private IHandlebars _hbs;

    private JsonSerializerSettings _jsonSettings;

    string outputDirectory;

    WebApplication app;
    List<string> tableNames;
    HandlebarsTemplate<object, object> tableTemplate;

    public WebApp(IDatabaseDriver db, WebAppOptions options) : base(db)
    {
        _options = options;
        this.app = WebApplication.Create([]);
        _hbs = Handlebars.Create();
        this.outputDirectory =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string tableTemplatePath = Path.Combine(outputDirectory, "Templates", "table.hbs");
        string temp = File.ReadAllText(tableTemplatePath);
        this.tableTemplate = _hbs.Compile(temp);
        this.tableNames = [];

        _jsonSettings = new JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                Console.WriteLine(
                    $"Error parsing property {args.ErrorContext.Path}: {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true; // Continue parsing other properties
            },
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
    }

    public void Init()
    {
        _hbs.RegisterHelper("ifIsNull", (writer, options, context, arguments) =>
        {
            if (arguments.Length == 1 && arguments[0] == null)
            {
                options.Template(writer, context);
            }
            else
            {
                options.Inverse(writer, context);
            }
        });
        _hbs.RegisterHelper("neq", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && parameters[0].ToString() != "Id")
            {
                writer.WriteSafeString(context);
            }
        });
        HandlebarsHelpers.Register(_hbs, Category.String);

        string formTemplatePath = Path.Combine(outputDirectory, "Templates", "form.hbs");
        string formTemplate = File.ReadAllText(formTemplatePath);
        _hbs.RegisterTemplate("form", formTemplate);

        app.Use(next => context =>
        {
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
    public async Task<string> readAllStream(PipeReader reader)
    {
        StringBuilder builder = new();
        int i = 0;
        UTF8Encoding encoding = new();
        ReadResult result;
        do
        {
            result = await reader.ReadAsync();
            builder.Append(encoding.GetString(result.Buffer));
            reader.AdvanceTo(result.Buffer.End);
            i++;
        } while (!result.IsCompleted && i < 10);

        Console.WriteLine(builder.ToString());
        return builder.ToString();
    }

    protected override void RegisterForm<TModel>(Form<TModel> form)
    {
        string tableName = form.TableName;
        List<ColumnInfo> columnsInfo = form.GetColumnsInfo();
        tableNames.Add(form.TableName);
        Console.WriteLine($"/table/{tableName}");
        app.MapGet($"/table/{tableName}", (HttpContext context) =>
        {
            var parameters = new
            {
                tableName = tableName,
                columns = columnsInfo,
                data = form.GetFields()
            };
            return Results.Content(tableTemplate(parameters), "text/html");
        });

        app.MapPost($"/table/{tableName}", async (HttpContext context) =>
        {
            string jsonString = await readAllStream(context.Request.BodyReader);
            JsonSerializer serializer = new JsonSerializer();

            var errorList = new List<string>();
            
            // Crazy hack...
            var jsonSettings = new JsonSerializerSettings
            {
                Error = (sender, args) =>
                {
                    if (args.ErrorContext.Error.Message.Contains("null"))
                    {
                        errorList.Add(args.ErrorContext.Path);
                        args.ErrorContext.Handled = true;
                    }
                },
            };

            TModel? data = JsonConvert.DeserializeObject<TModel>(jsonString, jsonSettings);
            if (data == null) return Results.BadRequest();
            Console.WriteLine(errorList);
            
            form.Add(data, errorList);
            return Results.Ok();
        });

        app.MapPatch($"/table/{tableName}", async (HttpContext context) =>
        {
            string jsonString = await readAllStream(context.Request.BodyReader);
            JsonSerializer serializer = new JsonSerializer();
            PatchData<TModel>? data =
                JsonConvert.DeserializeObject<PatchData<TModel>>(jsonString);
            if (data == null || data.oldData == null || data.newData == null)
                return Results.BadRequest();
            Console.WriteLine(data.oldData);
            Console.WriteLine(data.newData);
            form.Update(data.oldData, data.newData);
            return Results.Ok();
        });

        app.MapDelete($"/table/{tableName}", async (HttpContext context) =>
        {
            string jsonString = await readAllStream(context.Request.BodyReader);
            JsonSerializer serializer = new JsonSerializer();
            TModel? data = JsonConvert.DeserializeObject<TModel>(jsonString);
            Console.WriteLine(data);
            if (data == null) return Results.BadRequest();
            form.Delete(data);
            return Results.Ok();
        });
    }

    public void Start()
    {
        string indexTemplatePath = Path.Combine(outputDirectory, "Templates", "index.hbs");
        string indexTemplate = File.ReadAllText(indexTemplatePath);
        HandlebarsTemplate<object, object> indexPage = _hbs.Compile(indexTemplate);
        this.app.MapGet("/", (HttpContext context) =>
        {
            context.Response.ContentType = "text/html";
            return indexPage(new
            {
                tableNames = tableNames,
            });
        });
        app.Run($"http://localhost:{_options.Port}");
    }

    class PatchData<T> where T : class
    {
        public T? oldData = null;
        public T? newData = null;
    }
}