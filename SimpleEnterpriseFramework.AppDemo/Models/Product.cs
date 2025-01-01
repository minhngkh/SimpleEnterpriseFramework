using SimpleEnterpriseFramework.Abstractions.App;
using SimpleEnterpriseFramework.Abstractions.Data;
using SimpleEnterpriseFramework.Data.Sqlite;

namespace SimpleEnterpriseFramework.AppDemo.Models;

public class Product : Model
{
    [SqliteField("INTEGER", "id", IsKey = true, Autoincrement = true)]
    public long Id;

    [SqliteField("TEXT", "name", Nullable = false)]
    public string Name;
    
    [SqliteField("REAL", "price", Nullable = false)]
    public double Price;
    
    public override string TableName => "Product";
}

public class ProductForm(IDatabaseDriver db) : Form<Product>(db);