using Amazon.DynamoDBv2;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Lambdaで動くときは、API Gateway(HTTP API)から届くイベントをHTTPリクエストに変換してエンドポイントに渡す。
// Lambda以外(ECS、ローカル)では何もせず、通常どおりKestrelで起動する(ADR-0031、ADR-0032)
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// データアクセスの実装を設定値で切り替える。ECS版は既定のPostgres、サーバーレス版はDynamoDb(ADR-0030、ADR-0037)
var useDynamoDb = builder.Configuration["Storage:Provider"] == "DynamoDb";

if (useDynamoDb)
{
    builder.Services.AddSingleton(new DynamoTodoStoreOptions
    {
        TableName = builder.Configuration["DynamoDb:TableName"] ?? "todo-api-todos",
    });

    // ServiceUrlはローカルのDynamoDB Local用。AWS上では指定せず、リージョンと認証情報は実行環境から取る
    var serviceUrl = builder.Configuration["DynamoDb:ServiceUrl"];
    builder.Services.AddSingleton<IAmazonDynamoDB>(_ => string.IsNullOrEmpty(serviceUrl)
        ? new AmazonDynamoDBClient()
        : new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = serviceUrl }));

    builder.Services.AddSingleton<ITodoStore, DynamoTodoStore>();
}
else
{
    var connectionString =
        $"Host={builder.Configuration["Db:Host"]};" +
        $"Port={builder.Configuration["Db:Port"] ?? "5432"};" +
        $"Database={builder.Configuration["Db:Name"]};" +
        $"Username={builder.Configuration["Db:Username"]};" +
        $"Password={builder.Configuration["Db:Password"]}";

    builder.Services.AddDbContext<TodoDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<ITodoStore, EfTodoStore>();
}

var app = builder.Build();

// マイグレーションはRDB(EF Core)のときだけ。DynamoDBはスキーマを持たない(ADR-0028)
if (!useDynamoDb)
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<TodoDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 一覧は最大100件まで。上限はアプリの方針としてここで掛け、各実装は遅延実行で流すだけにする(ADR-0038)
app.MapGet("/todos", (ITodoStore store) =>
    store.GetAllAsync().Take(100));

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
