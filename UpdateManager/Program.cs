using Microsoft.EntityFrameworkCore;
using UpdateManager.Database.Context;
using UpdateManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var dbContext = new ManagerContext();
dbContext.Database.Migrate();
dbContext.Dispose();

builder.Services.AddDbContext<ManagerContext>();

builder.Services.AddScoped<UpdatesService>();

builder.Services.AddHostedService<DownloadBackgroundService>();

builder.Services.AddSingleton<DownloadQueueSingleston>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();