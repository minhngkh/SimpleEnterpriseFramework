using HandlebarsDotNet;
using System.IO;

var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

// Load and compile the Handlebars template
var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "index.hbs");
var templateContent = await File.ReadAllTextAsync(templatePath);
var template = Handlebars.Compile(templateContent);

// Data for the template
var data = new
{
    title = "Hello world!",
    message = "Welcome to Handlebars in ASP.NET!"
};

app.MapGet("/", () => template(data));

app.Run();
