using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using OllamaSharp;
using OllamaSharp.Models.Chat;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton(sp => new OllamaApiClient(
    new Uri(builder.Configuration.GetValue<string>("OllamaHost") ?? "http://localhost:11434")
));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet(
    "/models",
    async (OllamaApiClient ollamaClient) =>
    {
        var models = await ollamaClient.ListRunningModelsAsync();
        //var models = await ollamaClient.ListLocalModelsAsync();
        StringBuilder sb = new StringBuilder();
        foreach (var model in models)
        {
            sb.AppendLine($"{model.Name}: Size: {model.Size:N0}");
        }
        return Results.Content(sb.ToString());
    }
);

app.MapGet(
    "/question",
    async (string q, OllamaApiClient ollamaClient, HttpContext context) =>
    {
        context.Response.ContentType = "text/plain";
        var chat = ollamaClient.ChatAsync(
            new ChatRequest()
            {
                Model = "llama3",
                Messages = new List<Message> { new Message(ChatRole.User, q) }
            }
        );
        await foreach (var rsp in chat)
        {
            await context.Response.WriteAsync(rsp?.Message.Content ?? "");
            await context.Response.Body.FlushAsync();
        }
    }
);

app.Run();
