# ビルドは共通。最終ステージだけを基盤ごとに分ける(ADR-0034)
#   ECS用   : docker build --target runtime .
#   Lambda用: docker build --target lambda .
# CPUはECS版・Lambda版ともx86_64に揃える(ADR-0035)。ReadyToRunで事前にコンパイルする機械語もlinux-x64向け
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Todo.Api.csproj .
RUN dotnet restore -r linux-x64 -p:PublishReadyToRun=true

COPY . .
RUN dotnet publish -c Release -o /publish --no-restore \
    -r linux-x64 --self-contained false -p:PublishReadyToRun=true

# --- Lambda用 ---
# AWSのベースイメージに、Lambdaサービスとやりとりするクライアント(Runtime Interface Client)が入っている。
# Lambdaではコンテナは権限を絞った既定のユーザーで実行されるため、ECS用のようなユーザー作成は不要
FROM --platform=linux/amd64 public.ecr.aws/lambda/dotnet:10 AS lambda
COPY --from=build /publish ${LAMBDA_TASK_ROOT}
CMD ["Todo.Api"]

# --- ECS用 ---
# docker compose とデプロイワークフローは --target runtime を明示している。
# --target を省略した場合も最後のステージが使われるので、念のため最後に置いておく
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN useradd --no-create-home --shell /usr/sbin/nologin appuser
COPY --from=build --chown=appuser:appuser /publish .
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Todo.Api.dll"]
