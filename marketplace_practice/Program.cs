using marketplace_practice;
using marketplace_practice.Models;
using marketplace_practice.Services;
using marketplace_practice.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Добавляем аутентификацию и авторизацию
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
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
});
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<AuthUtils>();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddIdentity<User, Role>(options =>
{
    // Настройки пароля потом нужно изменить
    options.Password.RequiredLength = 4; // Минимальная длина
    options.Password.RequireDigit = false; // Требуется хотя бы одна цифра
    options.Password.RequireLowercase = false; // Требуется строчная буква
    options.Password.RequireUppercase = false; // Требуется заглавная буква
    options.Password.RequireNonAlphanumeric = false; // Требуется спецсимвол
    options.Password.RequiredUniqueChars = 1; // Уникальные символы

    options.User.RequireUniqueEmail = true; // Уникальный email
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var app = builder.Build();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();