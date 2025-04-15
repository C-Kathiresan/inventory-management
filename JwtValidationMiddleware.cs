using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.API
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        public JwtValidationMiddleware(RequestDelegate next,IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }
        private ClaimsPrincipal? ValidateJwtToken(string token)
        {
            
                // Create token validation parameters using config values
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ClockSkew = TimeSpan.Zero // Reduce default 5 min tolerance if needed
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                // Validate the token. This will throw if invalid.
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

                // Optionally, additional checks can be performed here.
                return principal;
           
        }
        public Task Invoke(HttpContext httpContext)
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                var principal = ValidateJwtToken(token);

            }
                return _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class JwtValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtValidationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtValidationMiddleware>();
        }
    }
}
