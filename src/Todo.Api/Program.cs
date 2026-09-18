var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<ITodoStore, InMemoryTodoStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/todos", (ITodoStore store) => store.GetAll());

app.MapGet("/todos/{id:guid}", (Guid id, ITodoStore store) =>
    store.Get(id) is { } todo ? Results.Ok(todo) : Results.NotFound());

app.MapPost("/todos", (CreateTodoRequest request, ITodoStore store) =>
{
    var todo = store.Add(request.Title);
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id:guid}", (Guid id, UpdateTodoRequest request, ITodoStore store) =>
    store.Update(id, request.Title, request.Done) is { } todo ? Results.Ok(todo) : Results.NotFound());

app.MapDelete("/todos/{id:guid}", (Guid id, ITodoStore store) =>
    store.Delete(id) ? Results.NoContent() : Results.NotFound());

app.Run();

record TodoItem(Guid Id, string Title, bool Done);

record CreateTodoRequest(string Title);

record UpdateTodoRequest(string Title, bool Done);

interface ITodoStore
{
    IEnumerable<TodoItem> GetAll();
    TodoItem? Get(Guid id);
    TodoItem Add(string title);
    TodoItem? Update(Guid id, string title, bool done);
    bool Delete(Guid id);
}

class InMemoryTodoStore : ITodoStore
{
    private readonly Dictionary<Guid, TodoItem> _items = new();

    public IEnumerable<TodoItem> GetAll() => _items.Values;

    public TodoItem? Get(Guid id) => _items.GetValueOrDefault(id);

    public TodoItem Add(string title)
    {
        var todo = new TodoItem(Guid.NewGuid(), title, Done: false);
        _items[todo.Id] = todo;
        return todo;
    }

    public TodoItem? Update(Guid id, string title, bool done)
    {
        if (!_items.ContainsKey(id))
        {
            return null;
        }

        var todo = new TodoItem(id, title, done);
        _items[id] = todo;
        return todo;
    }

    public bool Delete(Guid id) => _items.Remove(id);
}
