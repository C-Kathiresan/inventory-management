using Microsoft.IdentityModel.Tokens;
using System.Text;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Infrastructure.Repositories;
using System.Data;
using Inventory.API;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()                        // Include context information (e.g., correlation IDs)
    .Enrich.WithMachineName()                      // Optionally add machine name to logs
    .Enrich.WithProcessId()                         // Optionally add process id to logs
    .WriteTo.Console()                              // Log events to the console
    .WriteTo.File(
        path: "Logs/log-.txt",                       // Log files will be created under a 'Logs' folder.
        rollingInterval: RollingInterval.Day,        // A new log file per day.
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddStackExchangeRedisCache(op =>
{
    op.Configuration = builder.Configuration["Redis:Connection"];
    op.InstanceName = "Inventroy_";
    

});
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"]));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
 
builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAngularApp",
//        policy => policy.WithOrigins("http://localhost:4200,http://localhost:3000") // your Angular app URL
//                        .AllowAnyHeader()
//                        .AllowAnyMethod());
//});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
var app = builder.Build();
app.UseExceptionMiddleware();
// Configure the HTTP request pipeline.
if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseJwtValidationMiddleware();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();
