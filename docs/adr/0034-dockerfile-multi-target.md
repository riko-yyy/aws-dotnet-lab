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
Bを採用する。ビルドのステージ(`restore`と`publish`)は共通にし、最終ステージとしてLambda用の`lambda`と、ECS用の`runtime`を持たせる。ECS用の`runtime`は、ファイルの最後に置く。

## 理由
- ビルドの手順が1か所にまとまる。Aでは`restore`と`publish`が重複し、片方だけ直してもう片方を直し忘れるおそれがある
- 「同じビルド結果を、2つの基盤用に包み分けている」ことが、1つのファイルから読み取れる
- `--target`を指定しないと、最後のステージが作られる。ECS用を最後に置けば、docker composeや既存のデプロイワークフロー(`docker build`に`--target`を付けていない)は変更しなくて済む
- BuildKitは、指定されたステージに必要のないステージを作らない。ECS用を作るときに、Lambda用のステージは作られない(逆も同じ)

## 却下した選択肢
- A: それぞれのファイルは単純で読みやすいが、ビルドの手順が重複する

## トレードオフ
- 「ECS用のステージを最後に置く」という順番に意味がある。順番を入れ替えると、docker composeで作られるイメージがLambda用に変わってしまう。Dockerfileにコメントで明記する
- ECS用のステージでは、root以外のユーザーを作って実行している(journal: 2026-09-19-dockerfile-non-root-user)。Lambdaでは、コンテナは権限を絞った既定のユーザーで実行されるので、Lambda用のステージでは同じ設定は不要
