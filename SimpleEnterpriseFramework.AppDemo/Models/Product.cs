using SimpleEnterpriseFramework.App;
using SimpleEnterpriseFramework.Data;

namespace SimpleEnterpriseFramework.AppDemo.Models;

public class Product : Model {
    public Int64? Id = null;
    public string name = "";
    public double price = 0;


    public override string TableName => "Product";
}

public class ProductForm(IDatabaseDriver db) : Form<Product>(db);