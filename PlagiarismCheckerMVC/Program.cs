using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Добавлено для Swagger
using PlagiarismCheckerMVC.Models;
using PlagiarismCheckerMVC.Services;
using Microsoft.AspNetCore.Rewrite; // Добавляем для URL Rewriting

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры
builder.Services.AddControllers();

// Добавляем DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Настройка параметров приложения из конфигурации
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<CloudStorageSettings>(builder.Configuration.GetSection("GoogleCloudStorage"));
builder.Services.Configure<SearchEngineSettings>(builder.Configuration.GetSection("SearchEngineSettings"));
builder.Services.Configure<PlagiarismSettings>(builder.Configuration.GetSection("PlagiarismSettings"));

// Настройка аутентификации JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecurityKey"] ?? throw new InvalidOperationException("Security key is not configured")))
        };
    });

// Регистрируем сервисы приложения
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPlagiarismService, PlagiarismService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PlagiarismSearchApp API", Version = "v1" });

    // Настройка Swagger для JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            new string[] {}
        }
    });
});

var app = builder.Build();

// Настройка Pipeline запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PlagiarismSearchApp API v1");
        // Чтобы по умолчанию открывалась не Swagger UI, а index.html
        c.RoutePrefix = "swagger";
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Закомментируем URL Rewriting
// var options = new RewriteOptions()
//     .AddRewrite("^([^-.]+)$", "$1.html", skipRemainingRules: true);
// app.UseRewriter(options);

app.UseStaticFiles();
app.UseCors("AllowAll");

// Добавляем middleware аутентификации и авторизации
app.UseAuthentication();
app.UseAuthorization();

// Маппинг контроллеров
app.MapControllers();

app.Run();
