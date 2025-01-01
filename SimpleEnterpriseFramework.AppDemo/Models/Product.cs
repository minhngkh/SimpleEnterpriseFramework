using SimpleEnterpriseFramework.Abstractions.App;
using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Data.Sqlite;

namespace SimpleEnterpriseFramework.AppDemo.Models;

public class Product : Model
{
    [SqliteProperty("INTEGER", "id", IsKey = true, Autoincrement = true)]
    public long Id { get; set; }

    [SqliteProperty("TEXT", "name", Nullable = false)]
    public string Name { get; set; }

    [SqliteProperty("REAL", "price", Nullable = false)]
    public double Price { get; set; }

    public override string TableName => "product";
}

public class ProductForm(IDatabaseDriver db) : Form<Product>(db);