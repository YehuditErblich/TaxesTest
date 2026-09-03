using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using server.Application.FormTemplates;
using server.Application.Statuses;
using server.Application.Submissions;
using server.Application.Workflows;
using server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase(builder.Configuration["Database:Name"] ?? "TaxesTest"));
builder.Services.AddScoped<IFormTemplateService, FormTemplateService>();
builder.Services.AddScoped<IWorkflowTemplateService, WorkflowTemplateService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddCors(options => options.AddPolicy("LocalClient", policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LocalClient");
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
