# ADR-0013: DB接続情報とパスワードの渡し方

## 論点
ローカル開発環境で、DBの接続情報(特にパスワード)をAPIコンテナにどう渡すか。

## 選択肢
1. Composeの`environment`に直書き(現状)
2. `.env`ファイルに分離する
3. `appsettings.json`に書く(Docker公式の[.NETガイド](https://docs.docker.com/guides/dotnet/)の例)
4. Docker secretsを使う(公式ガイドのcompose例)

## 決定
1(`environment`への直書き)のまま。

## 理由
ECS Fargateでも「環境変数経由で接続情報を渡す」仕組みを使っており(`Db__Host`などの命名規則もアプリ側の設定規約に合わせて共通)、ローカルとAWSで渡し方を一貫させられる。AWS側は環境変数の中身をプレーンな値からSecrets Manager参照(ADR-0009関連)に差し替えるだけで済み、アプリ側のコードは一切変更不要になっている。

## 却下した選択肢
- `appsettings.json`: 値がイメージ自体に焼き込まれてしまい、環境ごとに切り替えにくい
- Docker secrets: アプリ側をファイル読み込み対応に変更する必要があり、ローカル検証用途としては過剰
- `.env`ファイルへの分離: `git clone`して`docker compose up`するだけで動く手軽さを優先し、あえて分離しなかった

## 注意
このリポジトリは公開状態のため、ここに書く値はローカル専用の使い捨てパスワードに限定する。実際のRDSの認証情報とは完全に別物で、漏れても実害が無いことを前提にした運用(詳細は[docs/journal/2026-09-20-docker-compose-mental-model.md](../journal/2026-09-20-docker-compose-mental-model.md))。
