using HandlebarsDotNet;
using System.IO;
using SimpleEnterpriseFramework.Membership;


public class Program {
    public static void Main(string[] args) {
        MembershipRepository repo = new MembershipRepository();
        // List<String> tableNames = repo.ListTables();

        // repo.CreateUserTable();
        // repo.CreateRoleTable();
        // repo.AddRole("admin");
        // repo.AddRole("user");
        // repo.AddUser("anhhoang123", "123", "admin");
        // repo.DeleteUser("anhhoang");
           repo.modifyUser("anhhoang123", "anh", "456", "user");
        
        // for (int i = 0; i < 15; i++) {
        //     repo.Add<User>(new User() {
        //         Id = i,
        //         Username = $"user{i}",
        //         Email = $"example{i}@gmail.com",
        //         Phone = i%2 == 0 ? null : String.Concat(Enumerable.Repeat($"{i}", 10)),
        //         Password = $"password{i}"
        //     });
        // }

        // object getTableParameters(string tableName) {
        //     return new {
        //         tableName = tableName,
        //         columns = repo.ListColumns(tableName).Select(colInfo => colInfo.name).ToArray(),
        //         data = repo.Find(tableName),
        //     };
        // }


        var builder = WebApplication.CreateSlimBuilder(args);

        var app = builder.Build();

        string tableTemplatePart = Path.Combine(Directory.GetCurrentDirectory(), "templates", "table.hbs");
        string tableTemplate = File.ReadAllText(tableTemplatePart);
        Handlebars.RegisterTemplate("table", tableTemplate);

        string indexTemplatePart = Path.Combine(Directory.GetCurrentDirectory(), "templates", "index.hbs");;
        string indexTemplate = File.ReadAllText(indexTemplatePart);
        var indexPage = Handlebars.Compile(indexTemplate);
        // var tableData = getTableParameters("User");
        // app.MapGet("/", (HttpContext context) => {
        //     context.Response.ContentType = "text/html";
        //     return indexPage(tableData);
        // });
        app.Run();
    }
}
