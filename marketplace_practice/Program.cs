using marketplace_practice;
using marketplace_practice.Utils;
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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

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

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<marketplace_practice.Models.Role>>();

    string[] roleNames = { "Покупатель", "Продавец" };
    string description = "Стандартная роль пользователя";

    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            var role = new marketplace_practice.Models.Role
            {
                Name = roleName,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                // Логируем ошибки, если не удалось создать роль
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Ошибка при создании роли {roleName}: {errors}");
            }
            else
            {
                Console.WriteLine($"Роль '{roleName}' успешно создана.");
            }
        }
    }
}
// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseHttpsRedirection();

app.Run();