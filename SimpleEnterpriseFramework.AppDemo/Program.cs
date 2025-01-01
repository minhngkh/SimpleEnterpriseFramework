using SimpleEnterpriseFramework.App.Web;
using SimpleEnterpriseFramework.AppDemo.Models;
using SimpleEnterpriseFramework.Core;
using SimpleEnterpriseFramework.Data.Sqlite;

var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
var dbPath = configuration.GetValue<string>("DatabasePath");
var port = configuration.GetValue<int>("Port");
var key = configuration.GetValue<string>("SecretKey");

var f = new Framework(options => { options.SecretKey = key!; });
f.SetDatabaseDriver<SqliteDriver, SqliteDriverOptions>(options =>
{
    options.UsePath(dbPath!);
});


var m = f.Membership;
// m.Setup(true);
var result = m.Register("minhngkh@gmail.com", "minh134");
var result2 = m.Login("minhngkh@gmail.com", "minh134", out var token);
Console.WriteLine(result2 + ": " + token);
// m.Setup();
//
// var ui = f.CreateCrudApp<WebApp>();
// ui.Init();
// ui.RegisterForm<Product, ProductForm>();
// ui.RegisterForm<User, UserForm>();
// ui.Start();