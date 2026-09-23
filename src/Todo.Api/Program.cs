using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString =
    $"Host={builder.Configuration["Db:Host"]};" +
    $"Port={builder.Configuration["Db:Port"] ?? "5432"};" +
    $"Database={builder.Configuration["Db:Name"]};" +
    $"Username={builder.Configuration["Db:Username"]};" +
    $"Password={builder.Configuration["Db:Password"]}";

builder.Services.AddDbContext<TodoDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ITodoStore, EfTodoStore>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<TodoDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/todos", async (ITodoStore store) =>
    await store.GetAllAsync());

app.MapGet("/todos/{id:guid}", async (Guid id, ITodoStore store) =>
    await store.GetAsync(id) is { } todo ? Results.Ok(todo) : Results.NotFound());

app.MapPost("/todos", async (CreateTodoRequest request, ITodoStore store) =>
{
    var todo = await store.AddAsync(request.Title);
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id:guid}", async (Guid id, UpdateTodoRequest request, ITodoStore store) =>
    await store.UpdateAsync(id, request.Title, request.Done) is { } todo ? Results.Ok(todo) : Results.NotFound());

app.MapDelete("/todos/{id:guid}", async (Guid id, ITodoStore store) =>
    await store.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

app.Run();

record CreateTodoRequest(string Title);

record UpdateTodoRequest(string Title, bool Done);
