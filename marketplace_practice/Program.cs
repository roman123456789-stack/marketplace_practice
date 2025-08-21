using marketplace_practice;
using marketplace_practice.Middlewares;
using marketplace_practice.Models;
using marketplace_practice.Services;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection("EmailConfig"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IFavoriteProductService, FavoriteProductService>();
builder.Services.AddScoped<IFeaturedProductsService, FeaturedProductsService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddEndpointsApiExplorer();

// ДЛЯ ЛОКАЛЬНОГО ЗАПУСКА
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ДЛЯ ЗАПУСКА В DOCKER
//builder.Services.AddDbContext<AppDbContext>(options =>
//{
//    options.UseNpgsql(builder.Configuration.GetConnectionString(
//        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
//            ? "DockerInternalConnection"
//            : "DockerExternalConnection"
//    ));
//});

builder.Services.AddIdentity<User, marketplace_practice.Models.Role>(options =>
{
    // Настройки пароля потом нужно изменить
    options.Password.RequiredLength = 8; // Минимальная длина
    options.Password.RequireDigit = false; // Требуется хотя бы одна цифра
    options.Password.RequireLowercase = false; // Требуется строчная буква
    options.Password.RequireUppercase = false; // Требуется заглавная буква
    options.Password.RequireNonAlphanumeric = false; // Требуется спецсимвол
    options.Password.RequiredUniqueChars = 1; // Уникальные символы

    options.User.RequireUniqueEmail = true; // Уникальный email
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("Auth", new OpenApiInfo { Title = "Auth API", Version = "v1" });
    c.SwaggerDoc("Users", new OpenApiInfo { Title = "Users API", Version = "v1" });
    c.SwaggerDoc("Products", new OpenApiInfo { Title = "Products API", Version = "v1" });
    c.SwaggerDoc("Orders", new OpenApiInfo { Title = "Orders API", Version = "v1" });
    c.SwaggerDoc("Cart", new OpenApiInfo { Title = "Cart API", Version = "v1" });
    c.SwaggerDoc("Admin", new OpenApiInfo { Title = "Admin API", Version = "v1" });
    c.SwaggerDoc("Catalog", new OpenApiInfo { Title = "Catalog API", Version = "v1" });
    c.SwaggerDoc("Default", new OpenApiInfo { Title = "Default API", Version = "v1" });

    // Добавляем поддержку JWT в Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Введите JWT-токен",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http, // или ApiKey
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Добавляем аутентификацию и авторизацию
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtRefreshToken";
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "your-super-long-secret-key-here-32-characters-min"))
    };
})
.AddScheme<JwtRefreshTokenOptions, JwtRefreshTokenHandler>(
    "JwtRefreshToken",
    options => { });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(
            JwtBearerDefaults.AuthenticationScheme,
            "JwtRefreshToken")
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/Auth/swagger.json", "Auth");
        c.SwaggerEndpoint("/swagger/Users/swagger.json", "Users");
        c.SwaggerEndpoint("/swagger/Products/swagger.json", "Products");
        c.SwaggerEndpoint("/swagger/Orders/swagger.json", "Orders");
        c.SwaggerEndpoint("/swagger/Cart/swagger.json", "Cart");
        c.SwaggerEndpoint("/swagger/Admin/swagger.json", "Admin");
        c.SwaggerEndpoint("/swagger/Catalog/swagger.json", "Catalog");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles();

app.Run();