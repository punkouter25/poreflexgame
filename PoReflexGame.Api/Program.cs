using PoReflexGame.Api.Services; // Add this using directive
using Microsoft.OpenApi.Models; // Add this for Swagger info

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Use default PascalCase
    }); // Add controllers service
builder.Services.AddEndpointsApiExplorer(); // For Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PoReflexGame API", Version = "v1" });
}); // For Swagger/OpenAPI

// Configure AzureStorage section from appsettings.json
builder.Services.Configure<AzureStorageConfig>(builder.Configuration.GetSection("AzureStorage"));

// Register AzureTableStorageService and HttpClient
builder.Services.AddHttpClient<AzureTableStorageService>();
builder.Services.AddSingleton<AzureTableStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PoReflexGame API V1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization(); // Use authorization middleware

app.MapControllers(); // Map controller routes

app.Run();
