using HandlebarsDotNet;
using System.IO;

var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

// Load and compile the Handlebars template
var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "index.hbs");
var templateContent = await File.ReadAllTextAsync(templatePath);
var template = Handlebars.Compile(templateContent);

// Data for the template
var testdata = new
{
    title = "Hello world!",
    message = "Welcome to Handlebars in ASP.NET!"
};

var User_templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "users.hbs");
var User_templateContent = await File.ReadAllTextAsync(User_templatePath);
var User_template = Handlebars.Compile(User_templateContent);

var Users_data = new
{
    title = "User List",
    users = new[]
    {
        new { username = "User1", email = "user1@example.com", password = "password1", phone = "123-456-7890" },
        new { username = "User2", email = "user2@example.com", password = "password2", phone = "987-654-3210" },
        new { username = "User3", email = "user3@example.com", password = "password3", phone = "555-666-7777" },
        new { username = "User4", email = "user4@example.com", password = "password4", phone = "444-555-6666" },
        new { username = "User5", email = "user5@example.com", password = "password5", phone = "333-222-1111" },
        new { username = "User6", email = "user6@example.com", password = "password6", phone = "222-333-4444" },
        new { username = "User7", email = "user7@example.com", password = "password7", phone = "111-222-3333" },
        new { username = "User8", email = "user8@example.com", password = "password8", phone = "666-777-8888" },
        new { username = "User9", email = "user9@example.com", password = "password9", phone = "777-888-9999" },
        new { username = "User10", email = "user10@example.com", password = "password10", phone = "999-000-1111" },
    }
};

app.MapGet("/", (HttpContext context) =>
{
    context.Response.ContentType = "text/html";
    return User_template(Users_data);
});
app.Run();
