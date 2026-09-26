using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

// サーバーレス版のデータアクセス(ADR-0028、ADR-0037)。
// テーブルはパーティションキー id(文字列)だけで、ソートキーやGSIは持たない。
class DynamoTodoStore(IAmazonDynamoDB dynamo, DynamoTodoStoreOptions options) : ITodoStore
{
    // 公開URLで誰でも追加できるため、作成から7日で DynamoDB のTTLにより自動削除させる
    static readonly TimeSpan TimeToLive = TimeSpan.FromDays(7);

    // Scan 1回あたりに読む件数。一覧の上限(Program.cs)で列挙が止まれば、次のページは読まない
    const int ScanPageSize = 100;

    public async IAsyncEnumerable<TodoItem> GetAllAsync()
    {
        Dictionary<string, AttributeValue>? startKey = null;
        do
        {
            var response = await dynamo.ScanAsync(new ScanRequest
            {
                TableName = options.TableName,
                Limit = ScanPageSize,
                ExclusiveStartKey = startKey,
            });

            foreach (var item in response.Items ?? [])
            {
                yield return ToTodoItem(item);
            }

            startKey = response.LastEvaluatedKey is { Count: > 0 } key ? key : null;
        } while (startKey is not null);
    }

    public async Task<TodoItem?> GetAsync(Guid id)
    {
        var response = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = options.TableName,
            Key = KeyOf(id),
        });

        return response.Item is { Count: > 0 } item ? ToTodoItem(item) : null;
    }

    public async Task<TodoItem> AddAsync(string title)
    {
        var todo = new TodoItem { Id = Guid.NewGuid(), Title = title, Done = false };
        var expiresAt = DateTimeOffset.UtcNow.Add(TimeToLive).ToUnixTimeSeconds();

        await dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = options.TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = todo.Id.ToString() },
                ["title"] = new() { S = todo.Title },
                ["done"] = new() { BOOL = todo.Done },
                ["expiresAt"] = new() { N = expiresAt.ToString() },
            },
        });
        return todo;
    }

    public async Task<TodoItem?> UpdateAsync(Guid id, string title, bool done)
    {
        // 「存在すれば更新」を条件付き更新の1リクエストで行う。EF Coreのような取得→変更→保存はしない
        try
        {
            var response = await dynamo.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = options.TableName,
                Key = KeyOf(id),
                ConditionExpression = "attribute_exists(id)",
                // 属性名がDynamoDBの予約語と衝突しないよう、#で別名を付けて参照する
                UpdateExpression = "SET #title = :title, #done = :done",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#title"] = "title",
                    ["#done"] = "done",
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":title"] = new() { S = title },
                    [":done"] = new() { BOOL = done },
                },
                ReturnValues = ReturnValue.ALL_NEW,
            });
            return ToTodoItem(response.Attributes);
        }
        catch (ConditionalCheckFailedException)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await dynamo.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = options.TableName,
                Key = KeyOf(id),
                ConditionExpression = "attribute_exists(id)",
            });
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    static Dictionary<string, AttributeValue> KeyOf(Guid id) =>
        new() { ["id"] = new() { S = id.ToString() } };

    static TodoItem ToTodoItem(Dictionary<string, AttributeValue> item) => new()
    {
        Id = Guid.Parse(item["id"].S),
        Title = item["title"].S,
        Done = item["done"].BOOL ?? false,
    };
}

class DynamoTodoStoreOptions
{
    public string TableName { get; set; } = string.Empty;
}
