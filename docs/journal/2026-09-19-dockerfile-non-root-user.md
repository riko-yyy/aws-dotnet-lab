# 2026-09-19: Dockerfileを非rootユーザーで実行するように変更

## きっかけ
Docker公式の[.NET向けガイド](https://docs.docker.com/guides/dotnet/)のDockerfileサンプルと、うちのDockerfileを比較したところ、公式は非rootユーザー(`appuser`)を作成して`USER`命令で権限を落としてから起動していた。うちは指定が無く、コンテナ内でrootのまま動いていた。

セキュリティの多層防御の観点で、アプリが乗っ取られた場合の被害範囲(blast radius)を減らすため、うちにも取り入れることにした。

## つまずいた箇所
公式サンプルの`adduser`コマンドをそのまま `mcr.microsoft.com/dotnet/aspnet:10.0`(無印/Debian系イメージ)に使ったところ、以下のエラーになった。
```
/bin/sh: 1: adduser: not found
```

## 原因
公式サンプルは `aspnet:10.0-alpine`(Alpine Linuxベース)を使っており、`adduser` はAlpineのBusyBoxに含まれるコマンド。うちが使っている `aspnet:10.0` は Ubuntu 24.04 ベースで、`adduser` ではなく `useradd`(`/usr/sbin/useradd`)が使えるコマンドだった。ベースイメージのディストリビューションが違うと、使えるユーザー管理コマンドも変わることを学んだ。

## 解決
```dockerfile
RUN useradd --no-create-home --shell /usr/sbin/nologin appuser
COPY --from=build --chown=appuser:appuser /app .
USER appuser
```
`docker exec ... whoami` で `appuser`(UID 1655、非0)になっていることを確認。CRUD動作も変わらず問題なし。
