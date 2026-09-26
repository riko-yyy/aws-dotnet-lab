using Microsoft.EntityFrameworkCore;

class EfTodoStore(TodoDbContext db) : ITodoStore
{
    // 呼び出し側のTake()はSQLのLIMITにはならない(IAsyncEnumerableはC#側で列挙を止めるだけ)
    public IAsyncEnumerable<TodoItem> GetAllAsync() =>
        db.Todos.AsNoTracking().AsAsyncEnumerable();

    public async Task<TodoItem?> GetAsync(Guid id) =>
        await db.Todos.FindAsync(id);

    public async Task<TodoItem> AddAsync(string title)
    {
        var todo = new TodoItem { Id = Guid.NewGuid(), Title = title, Done = false };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();
        return todo;
    }

    public async Task<TodoItem?> UpdateAsync(Guid id, string title, bool done)
    {
        var todo = await db.Todos.FindAsync(id);
        if (todo is null)
        {
            return null;
        }

        todo.Title = title;
        todo.Done = done;
        await db.SaveChangesAsync();
        return todo;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var todo = await db.Todos.FindAsync(id);
        if (todo is null)
        {
            return false;
        }

        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return true;
    }
}

// 既存のMigrationが typeof(TodoDbContext) と エンティティ名 "TodoItem" を参照しているため、
// 名前空間を付けずにグローバルのまま置いている(付けるとモデル差分扱いになりMigrateが失敗する)。
class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
}
