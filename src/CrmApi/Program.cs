using CrmApi.Data;
using CrmApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CrmDatabase")
                       ?? builder.Configuration["CRM_DATABASE_CONNECTION"]
                       ?? throw new InvalidOperationException("Connection string 'CrmDatabase' not found. Configure it in appsettings.json or via CRM_DATABASE_CONNECTION environment variable.");

builder.Services.AddDbContext<CrmDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<WorkItemService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
