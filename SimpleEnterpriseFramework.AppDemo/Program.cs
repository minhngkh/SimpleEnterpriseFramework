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


var m = f.Membership;
// // m.Setup(true);
// var result = m.Register("minhngkh@gmail.com", "minh134");
// var result2 = m.Login("minhngkh@gmail.com", "minh134", out var token);
// Console.WriteLine(result2 + ": " + token);
// m.Setup();
//

var ui = f.CreateCrudApp<WebApp>();
ui.Init();
ui.RegisterForm<Product, ProductForm>();
ui.RegisterForm<User, UserForm>();
ui.Start();