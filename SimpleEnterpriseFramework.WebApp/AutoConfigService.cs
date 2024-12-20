using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SimpleEnterpriseFramework.Membership;

namespace SimpleEnterpriseFramework.WebApp;

public class AutoConfigService
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Tự động cấu hình JWT từ appsettings.json
        services.Configure<JwtConfig>(configuration.GetSection("JwtConfig"));

        // Cấu hình Authentication với JWT
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtConfig = configuration.GetSection("JwtConfig").Get<JwtConfig>();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),
                    ClockSkew = TimeSpan.Zero // Giảm thời gian chênh lệch cho token hết hạn
                };
            });

        services.AddAuthorization();
    }
}
