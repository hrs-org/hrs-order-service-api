using System.Text;
using FluentValidation;
using HRS.API.Filters;
using HRS.API.Middleware;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.API.Validators.Rental;
using HRS.Domain.Interfaces;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using HRS.Shared.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IRentalOrderService, RentalOrderService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

builder.Services.AddScoped(typeof(ICrudRepository<>), typeof(CrudRepository<>));
builder.Services.AddScoped<IRentalOrderRepository, RentalOrderRepository>();
builder.Services.AddScoped<IAppConfiguration, AppConfiguration>();
builder.Services.AddHttpContextAccessor();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers(options => { options.Filters.Add<ValidationFilter>(); });
builder.Services.AddValidatorsFromAssemblyContaining<RentalOrderRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ReturnRentalOrderRequestValidator>();
////////////////
builder.Services.AddHttpClient("ItemMaintenanceService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ItemMaintenanceService"]!);
});
builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentService"]!);
});
builder.Services.AddHttpClient("ItemService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ItemService"]!);
});

// ----------------------------
// IConfiguration & MongoClient
// ----------------------------

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionMongoDB")
        ?? throw new InvalidOperationException("Missing MongoDB connection string.");
    return new MongoClient(connectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoDB:DatabaseName"]
        ?? throw new InvalidOperationException("Missing MongoDB:DatabaseName in configuration.");
    return client.GetDatabase(databaseName);
});
// MongoContext
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<IRentalOrderMongoDBRepository, RentalOrderMongoDBRepository>();
builder.Services.AddScoped(typeof(ICrudMongoDBRepository<>), typeof(CrudMongoDBRepository<>));




builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HRS API", Version = "v1" });

    // 🔑 Enable JWT Bearer in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token.\n\nExample: **Bearer eyJhbGciOi...**"
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
            []
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnectionMySql");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString,
        Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(connectionString),
        b => b.MigrationsAssembly("HRS.Migrations")));

builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebClient", policy =>
        policy.WithOrigins(
                builder.Configuration["AllowedOrigins"]?.Split(',') ?? Array.Empty<string>()
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
if (app.Environment.IsDevelopment()) app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowWebClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
