using SEF.UI;
using SEF.Repository;

public class Product {
#pragma warning disable 0414
    public Int64? Id = null;
    public string name = "";
    public double price = 0;
#pragma warning restore 0414
}

public class ProductForm : UIForm<Product> {
    public ProductForm(IRepository repo): base(repo) {
    }
}
