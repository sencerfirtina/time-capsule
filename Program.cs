using TimeCapsule.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TimeCapsule.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHostedService<CapsuleMonitorService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICapsuleService,CapsuleService>();
builder.Services.AddScoped<ISpotifyService,SpotifyService>();


var app = builder.Build();

app.MapControllers();

app.Run();

