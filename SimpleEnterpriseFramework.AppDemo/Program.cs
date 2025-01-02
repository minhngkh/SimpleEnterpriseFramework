using HandlebarsDotNet;
using SimpleEnterpriseFramework.App.Web;
using SimpleEnterpriseFramework.AppDemo.Models;
using SimpleEnterpriseFramework.Core;
using SimpleEnterpriseFramework.Data.Sqlite;

var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var dbPath = configuration.GetValue<string>("DatabasePath");
var port = configuration.GetValue<int>("Port");
var key = configuration.GetValue<string>("SecretKey");

var db = new SqliteDriver(new SqliteDriverOptions().UsePath(dbPath!));
db.CreateTable<User>(true);
db.CreateTable<Product>(true);

for (var i = 0; i < 16; i++)
{
    db.Add(new Product
        {
            Name = $"product{i}",
            Price = i * 1000
        },
        ["Name", "Price"]
    );
    db.Add(new User
        {
            Username = $"user{i}",
            Email = $"example{i}@gmail.com",
            Phone = i % 2 == 0 ? null : new string($"{i}"[0], 10),
            Password = $"password{i}"
        }
        , ["Username", "Email", "Phone", "Password"]
    );
}


var f = new Framework(options => { options.SecretKey = key!; });
f.SetDatabaseDriver<SqliteDriver, SqliteDriverOptions>(options =>
{
    options.UsePath(dbPath!);
});


var membership = f.Membership;
// // m.Setup(true);
membership.Setup(true);
membership.Register("minhngkh@gmail.com", "minh134", "admin");
membership.Register("tuong@gmail.com", "tuong", "admin");
membership.Register("user@gmail.com", "user", "user");
// var result2 = m.Login("minhngkh@gmail.com", "minh134", out var token);
// Console.WriteLine(result2 + ": " + token);
// m.Setup();
//


var ui = f.CreateCrudApp<WebApp>();
ui.Init();

//Login template
var loginTemplatePath =
    Path.Combine(Directory.GetCurrentDirectory(), "Templates", "login.hbs");
var loginTemplate = File.ReadAllText(loginTemplatePath);
var loginPage = Handlebars.Compile(loginTemplate);

ui.App.Use((context, next) =>
{
    if (context.Request.Path == "/login")
    {
        return next(context);
    }
    
    if (context.Request.Cookies.TryGetValue("token", out var token))
    {
        if (membership.IsLoggedInAsRole(token, "admin"))
        {
            return next(context);
        }
    }
    context.Response.Cookies.Delete("token");
    context.Response.Redirect("/login");
    return Task.CompletedTask;
});

ui.App.MapGet("/login", (HttpContext context) =>
{
    context.Response.ContentType = "text/html";
    var renderedLogin = loginPage(null);
    return Results.Text(renderedLogin, "text/html");
});

ui.App.MapPost("/register", (string username, string password) =>
{
    var success = membership.Register(username, password);
    if (!success)
    {
        return Results.BadRequest("Username already exists.");
    }

    return Results.Ok("User registered successfully.");
});

ui.App.MapPost("/login", async (HttpContext context) =>
{
    var res1 = context.Request.Form.TryGetValue("username", out var username);
    var res2 = context.Request.Form.TryGetValue("password", out var password);
    // var loginRequest = await context.Request.ReadFromJsonAsync<LoginRequest>();

    if (!res1 || !res2)
    {
        return Results.BadRequest("Invalid request payload.");
    }

    var result =
        membership.Login(username, password, out var token, out var role);
    if (!result || role != "admin")
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new { Token = token });
});

ui.App.MapPost("/logout", (string token) =>
{
    membership.Logout(token);
    return Results.Ok("Logged out successfully.");
});

ui.RegisterForm<Product, ProductForm>();
ui.RegisterForm<User, UserForm>();


ui.Start();