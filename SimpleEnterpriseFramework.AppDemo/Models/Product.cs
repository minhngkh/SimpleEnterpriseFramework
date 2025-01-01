using SimpleEnterpriseFramework.App;
using SimpleEnterpriseFramework.Data;

namespace SimpleEnterpriseFramework.AppDemo.Models;

public class Product : Model {
    public Int64? Id = null;
    public string name = "";
    public double price = 0;


    public override string TableName => "products";
}

public class ProductForm : Form<Product> {
    public ProductForm(IDatabaseDriver db) : base(db)
    {
    }
}