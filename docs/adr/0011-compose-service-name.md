# ADR-0011: Composeのサービス名を `api` にする

## 論点
`docker-compose.yml`のAPI用サービス名をどうするか。

## 選択肢
- `api`(現状)
- `server`(Docker公式の[.NETガイド](https://docs.docker.com/guides/dotnet/)の例)

## 決定
`api`のまま。

## 理由
サービス一覧に`api`と`db`が並ぶ方が、それぞれの役割が名前だけで直感的にわかる。この名前が実際に影響する範囲は、Composeが作る内部ネットワークでのホスト名解決(`Db__Host: db`のように参照する側)と、`docker compose logs api`のようなCLI操作での指定先だけで、アプリの動作自体には影響しない。

## 却下した選択肢
- `server`: 公式ガイドの命名。汎用的すぎて、サービスが増えた時に何を指しているか名前から読み取りにくいため採用しなかった
