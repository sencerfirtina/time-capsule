using TimeCapsule.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TimeCapsule.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using TimeCapsule.API.ExceptionHandlers;
using TimeCapsule.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
        maxRetryCount:5,
        maxRetryDelay: TimeSpan.FromSeconds(5),
        errorNumbersToAdd: null
    ) 
    );
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
   var secretKey = builder.Configuration["JwtSettings:SecretKey"];

   options.TokenValidationParameters = new TokenValidationParameters
   {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)) 
   };
});

builder.Services.AddCustomCors(builder.Configuration);

builder.Services.AddHostedService<CapsuleMonitorService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICapsuleService,CapsuleService>();
builder.Services.AddScoped<ISpotifyService,SpotifyService>();
builder.Services.AddScoped<IAuthService,AuthService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch(Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();    
        logger.LogError(ex,"An error occurred during the database migration process.");
    }
}

app.Run();

