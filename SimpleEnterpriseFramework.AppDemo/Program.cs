using SEF.Repository;
using SimpleEnterpriseFramework.App.Web;
using SimpleEnterpriseFramework.AppDemo.Models;
using SimpleEnterpriseFramework.Data.Sqlite;

var repo = new SqliteDriver("Data Source=test.db");
var ui = new WebApp(repo);
ui.Init();
// ui.Register<User, UserForm>(new UserForm(repo));
// ui.Register<Product, ProductForm>(new ProductForm(repo));

ui.RegisterForm<Product, ProductForm>();
ui.Start();