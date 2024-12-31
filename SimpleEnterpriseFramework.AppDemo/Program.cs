using SEF.UI;
using SEF.Repository;

SqliteRepository repo = new("Data Source=test.db");
WebUI ui = new WebUI(repo);
ui.Init();
ui.Register<User, UserForm>(new UserForm(repo));
ui.Register<Product, ProductForm>(new ProductForm(repo));
ui.Start();