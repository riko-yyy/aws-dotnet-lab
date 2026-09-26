// データアクセスの差し替え口。ECS版(EF Core + PostgreSQL)とサーバーレス版(DynamoDB)で実装を切り替える。
// 更新・削除は「IDを渡して結果を受け取る」形にし、EF Coreの変更追跡に依存しないようにしている。
public interface ITodoStore
{
    // 遅延実行で1件ずつ流す。件数の上限は呼び出し側(Program.cs)で掛ける(ADR-0038)
    IAsyncEnumerable<TodoItem> GetAllAsync();

    Task<TodoItem?> GetAsync(Guid id);

    Task<TodoItem> AddAsync(string title);

    // 存在しなければnull
    Task<TodoItem?> UpdateAsync(Guid id, string title, bool done);

    // 存在しなければfalse
    Task<bool> DeleteAsync(Guid id);
}
