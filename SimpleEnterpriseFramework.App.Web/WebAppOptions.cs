using SimpleEnterpriseFramework.IoC;

namespace SimpleEnterpriseFramework.App.Web;

public class WebAppOptions
{
    public int Port { get; set; } = 3000;
}

public static class WebAppServiceExtensions
{
    public static IContainer ConfigureWebApp(this IContainer container,
        Action<WebAppOptions> configureOptions)
    {
        container.Configure(configureOptions);
        return container;
    }
}