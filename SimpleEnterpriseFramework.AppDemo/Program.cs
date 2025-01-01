using SEF.Repository;
using SimpleEnterpriseFramework;
using SimpleEnterpriseFramework.App.Web;
using SimpleEnterpriseFramework.AppDemo.Models;
using SimpleEnterpriseFramework.Data;
using SimpleEnterpriseFramework.Data.Sqlite;
using SimpleEnterpriseFramework.IoC;

// var repo = new SqliteDriver("Data Source=test.db");
var f = new Framework();
f.SetDatabaseDriver<SqliteDriver, SqliteDriverOptions>(options =>
{
    options.UsePath("test.db");
});
// var ui = container.Resolve<WebApp>();

var ui = f.CreateCrudApp<WebApp>();
// var ui = new WebApp();
ui.Init();
// ui.Register<User, UserForm>(new UserForm(repo));
// ui.Register<Product, ProductForm>(new ProductForm(repo));

ui.RegisterForm<Product, ProductForm>();
ui.Start();