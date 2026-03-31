using System.Security.Claims;
using FluentValidation;
using HRS.API.Filters;
using HRS.API.Handlers;
using HRS.API.Middleware;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.API.Validators.Rental;
using HRS.Domain.Interfaces;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using HRS.Shared.Core.Authorization;
using HRS.Shared.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IRentalOrderService, RentalOrderService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IAppConfiguration, AppConfiguration>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizationHeaderHandler>();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers(options => { options.Filters.Add<ValidationFilter>(); });
builder.Services.AddValidatorsFromAssemblyContaining<RentalOrderRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ReturnRentalOrderRequestValidator>();
////////////////
builder.Services.AddHttpClient("ItemMaintenanceService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ItemMaintenanceService"]!);
}).AddHttpMessageHandler<AuthorizationHeaderHandler>();
builder.Services.AddHttpClient("PaymentService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentService"]!);
}).AddHttpMessageHandler<AuthorizationHeaderHandler>();
builder.Services.AddHttpClient("ItemService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ItemService"]!);
}).AddHttpMessageHandler<AuthorizationHeaderHandler>();

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
builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

var auth0Domain = builder.Configuration["Auth0:Domain"]!;
var auth0Audience = builder.Configuration["Auth0:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0Audience;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = $"https://{auth0Domain}/",
            ValidAudience = auth0Audience,
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var claims = context.Principal?.Claims.ToList() ?? new List<Claim>();
                var subClaim = claims.FirstOrDefault(c => c.Type == "sub");
                var claimsIdentity = (ClaimsIdentity)context.Principal?.Identity!;

                if (subClaim != null && !claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                }

                if (!claims.Any(c => c.Type == "userId"))
                {
                    var userIdClaim = claims.FirstOrDefault(c =>
                        c.Type == "https://hrs-api/userId" || c.Type == "https://hrs-api/user_id");

                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                    {
                        claimsIdentity.AddClaim(new Claim("userId", userId.ToString()));
                    }
                }

                if (!claims.Any(c => c.Type == "storeId"))
                {
                    var storeIdClaim = claims.FirstOrDefault(c =>
                        c.Type == "https://hrs-api/storeId" || c.Type == "https://hrs-api/store_id");

                    if (storeIdClaim != null && int.TryParse(storeIdClaim.Value, out var storeId))
                    {
                        claimsIdentity.AddClaim(new Claim("storeId", storeId.ToString()));
                    }
                }

                await Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Order service scopes
    options.AddPolicy("read:order", policy =>
        policy.Requirements.Add(new PermissionRequirement("read:order")));
    options.AddPolicy("write:order", policy =>
        policy.Requirements.Add(new PermissionRequirement("write:order")));
    options.AddPolicy("update:order", policy =>
        policy.Requirements.Add(new PermissionRequirement("update:order")));
    options.AddPolicy("delete:order", policy =>
        policy.Requirements.Add(new PermissionRequirement("delete:order")));
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

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
