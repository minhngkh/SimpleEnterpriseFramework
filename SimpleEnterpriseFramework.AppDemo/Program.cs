using SimpleEnterpriseFramework.App.Web;
using SimpleEnterpriseFramework.AppDemo.Models;
using SimpleEnterpriseFramework.Core;
using SimpleEnterpriseFramework.Data.Sqlite;

var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var dbPath = configuration.GetValue<string>("DatabasePath");
var port = configuration.GetValue<int>("Port");
var key = configuration.GetValue<string>("SecretKey");

var f = new Framework(options =>
{
    options.SecretKey = key!;
});
f.SetDatabaseDriver<SqliteDriver, SqliteDriverOptions>(options =>
{
    options.UsePath(dbPath!);
});

var ui = f.CreateCrudApp<WebApp>();
ui.Init();
ui.RegisterForm<Product, ProductForm>();
ui.Start();