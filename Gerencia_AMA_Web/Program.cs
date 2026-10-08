using System.Text;
using ApiTesourariaAMA.context;
using ApiTesourariaAMA.Services;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ApiTesourariaAMA.Models;
using ApiTesourariaAMA.Enums;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Banco de Dados SQLite (Forçando a assinatura com tipo genérico)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configurar Cloudinary
var cloudinaryAccount = new Account(
    builder.Configuration["Cloudinary:CloudName"],
    builder.Configuration["Cloudinary:ApiKey"],
    builder.Configuration["Cloudinary:ApiSecret"]
);
var cloudinary = new Cloudinary(cloudinaryAccount);
builder.Services.AddSingleton(cloudinary);

// 3. Registrar Serviços Locais (Assinatura explícita de TService no .NET 10)
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<CloudinaryService>();

// 4. Configurar Autenticação JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ChaveSeguraDeApenasTestesComPeloMenos32Caracteres!";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

// 5. Controllers e Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe o token JWT. Exemplo: Bearer eyJhbGciOiJIUzI1NiIs...",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- SEEDING INICIAL GARANTIDO ---
// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
//     // Garante que o banco e as tabelas estejam criados
//     context.Database.EnsureCreated();

//     if (!context.Usuarios.Any())
//     {
//         var tesoureiro = new Usuario
//         {
//             Nome = "Billy Grahan",
//             Email = "tesourariaiasdgenibau@gmail.com",
//             Senha = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // O próprio BCrypt gera a hash válida
//             Perfil = PerfilUsuario.Tesoureiro
//         };

//         context.Usuarios.Add(tesoureiro);
//         context.SaveChanges();
//     }
// }

app.Run();