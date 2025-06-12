using CloudDrive.Core.Services;
using CloudDrive.Infrastructure.Repositories;
using CloudDrive.Infrastructure.Services;
using CloudDrive.WebAPI.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter your JWT token here",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    };

    o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            []
        }
    };

    o.AddSecurityRequirement(securityRequirement);
});


builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"), 
        x => x.MigrationsAssembly("CloudDrive.Infrastructure")
    )
);

builder.Services.AddScoped<IAccessTokenProvider, JwtAccessTokenProvider>();
builder.Services.AddScoped<IPasswordEncoder, BCryptPasswordEncoder>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFileInfoService, FileInfoService>();
builder.Services.AddScoped<IFileVersionInfoService, FileVersionInfoService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IFileManagerService, FileManagerService>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.SaveToken = true;
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? "")),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false, 
            ValidateAudience = false,
            ValidateLifetime = true,
        };
    });

builder.Services.AddHttpLogging(o => {
    o.CombineLogs = true;
    o.LoggingFields = HttpLoggingFields.All;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 8_589_934_592; // 8 GiB
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var serverAddress = app.Urls.FirstOrDefault() ?? "http://localhost:5189";
    logger.LogInformation("Swagger available on {Url}", $"{serverAddress}/swagger/index.html");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpLogging();

app.MapControllers();

app.Run();
