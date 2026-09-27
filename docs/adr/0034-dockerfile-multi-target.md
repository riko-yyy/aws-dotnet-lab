# ADR-0034: Dockerfileは1つにし、最終ステージを基盤ごとに分ける

## 論点
ECS版とLambda版で、コンテナイメージのベースイメージが違う([ADR-0033](0033-serverless-deploy-and-packaging.md))。Dockerfileをどう持つか。

## 背景
- ECS版は`mcr.microsoft.com/dotnet/aspnet:10.0`をベースに、Kestrelで起動している
- Lambdaのコンテナイメージには、AWSが提供するベースイメージ(`public.ecr.aws/lambda/dotnet:10`)を使う。この中に、Lambdaとやりとりするためのクライアント(Runtime Interface Client)が入っている

## 選択肢
- A: Dockerfileを2つ用意する(`Dockerfile`と`Dockerfile.lambda`)
- B: Dockerfileは1つにし、マルチステージビルドで最終ステージを2つ持たせる。どちらを作るかは`docker build --target`で選ぶ

## 決定
Bを採用する。ビルドのステージ(`restore`と`publish`)は共通にし、最終ステージとしてLambda用の`lambda`と、ECS用の`runtime`を持たせる。

どちらのステージを作るかは、使う側で必ず`--target`を明示する(docker composeは`target: runtime`、デプロイワークフローは`--target runtime`)。

## 理由
- ビルドの手順が1か所にまとまる。Aでは`restore`と`publish`が重複し、片方だけ直してもう片方を直し忘れるおそれがある
- 「同じビルド結果を、2つの基盤用に包み分けている」ことが、1つのファイルから読み取れる
- `--target`を明示するので、Dockerfileの中のステージの順番に依存しない
- BuildKitは、指定されたステージに必要のないステージを作らない。ECS用を作るときに、Lambda用のステージは作られない(逆も同じ)

## 却下した選択肢
- A: それぞれのファイルは単純で読みやすいが、ビルドの手順が重複する

## トレードオフ
- docker composeとデプロイワークフローにも、1行ずつ変更が入る
  - 当初は、`--target`を省略すると最後のステージが作られることを利用し、「ECS用を最後に置けば、composeとワークフローは変更しなくて済む」と考えていた
  - しかしそれでは、ステージの順番をうっかり入れ替えただけで、ECSにLambda用のイメージが載ってしまう。そこで、順番に頼らず明示する形に改めた。念のため、ECS用は今も最後に置いている
- ビルドの出力先は`/publish`とした。最終ステージの置き場所(ECS用の`/app`、Lambda用の`/var/task`)と名前が重なると、`COPY --from=build /app ...`がどのステージの`/app`を指しているのか紛らわしいため
- ECS用のステージでは、root以外のユーザーを作って実行している(journal: 2026-09-19-dockerfile-non-root-user)。Lambdaでは、コンテナは権限を絞った既定のユーザーで実行されるので、Lambda用のステージでは同じ設定は不要
