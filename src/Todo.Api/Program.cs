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

app.MapGet("/todos", async (TodoDbContext db) =>
    await db.Todos.AsNoTracking().ToListAsync());

app.MapGet("/todos/{id:guid}", async (Guid id, TodoDbContext db) =>
    await db.Todos.FindAsync(id) is { } todo ? Results.Ok(todo) : Results.NotFound());

app.MapPost("/todos", async (CreateTodoRequest request, TodoDbContext db) =>
{
    var todo = new TodoItem { Id = Guid.NewGuid(), Title = request.Title, Done = false };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id:guid}", async (Guid id, UpdateTodoRequest request, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    todo.Title = request.Title;
    todo.Done = request.Done;
    await db.SaveChangesAsync();
    return Results.Ok(todo);
});

app.MapDelete("/todos/{id:guid}", async (Guid id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public class TodoItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Done { get; set; }
}

record CreateTodoRequest(string Title);

record UpdateTodoRequest(string Title, bool Done);

class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
}
