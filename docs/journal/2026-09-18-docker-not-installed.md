# 2026-09-18: ローカルにDockerが未インストール

## つまずいた箇所
第1段階の目標である「Docker化した.NET Core APIをローカルで動かす」を検証しようとしたところ、`docker` コマンドが見つからなかった。

## 原因
マシンにDocker Desktop(または代替のDockerランタイム)が未インストールだった。

## 現状の対応
- Dockerのインストールは環境変更にあたるため、Claudeでは実施せずユーザー自身が対応する方針とした
- 先にAPIプロジェクト(Minimal API, TODO CRUD)とDockerfileを用意し、`dotnet run` でのローカル起動とCRUD動作は確認済み
- Dockerインストール後にDockerfileのビルド・起動確認を行う(未実施)

## TODO
- [ ] Docker Desktopインストール後、`docker build` / `docker run` でコンテナ起動を確認する
