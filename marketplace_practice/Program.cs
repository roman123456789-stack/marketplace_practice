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

// ��������� �������������� � �����������
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
    // ��������� ������ ����� ����� ��������
    options.Password.RequiredLength = 4; // ����������� �����
    options.Password.RequireDigit = false; // ��������� ���� �� ���� �����
    options.Password.RequireLowercase = false; // ��������� �������� �����
    options.Password.RequireUppercase = false; // ��������� ��������� �����
    options.Password.RequireNonAlphanumeric = false; // ��������� ����������
    options.Password.RequiredUniqueChars = 1; // ���������� �������

    options.User.RequireUniqueEmail = true; // ���������� email
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