using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartResume.Data;
using SmartResume.AppServices;
using SmartResume.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddCors(options =>
{
   options.AddPolicy("AllowBlazorClient",
        policy =>
        {
            policy
                .WithOrigins(
                    "https://localhost:7099",
                    "http://localhost:5207",
                    "http://localhost:7099",
                    "http://localhost:5039",
                    "http://localhost:7067"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});
builder.Services
    .AddAuthentication(options =>
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddHttpClient<IJobService, JobService>();
builder.Services.AddScoped<IJobAppService, JobAppService>();
builder.Services.AddScoped<IResumeAppService, ResumeAppService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddHttpClient(); 
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazorClient");
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
