# 2026-09-18: AWS CLI v1が壊れていた

## つまずいた箇所
`aws --version` を実行すると以下のエラーで起動しなかった。
```
dyld[...]: Library not loaded: @executable_path/../.Python
```

## 原因
既存の `aws` コマンドがPython依存の旧v1系(`/usr/local/aws` 配下)で、参照していたPython環境が失われていたため起動できなかった。

## 解決
[ADR-0004](../adr/0004-iam-authentication.md)でIAM Identity Center(SSO)を使う方針にしたため、どのみちSSOをネイティブサポートするAWS CLI v2への入れ替えが必要だった。公式pkgインストーラーで入れ替えて解消。
```bash
curl "https://awscli.amazonaws.com/AWSCLIV2.pkg" -o "AWSCLIV2.pkg"
sudo installer -pkg AWSCLIV2.pkg -target /
```
`aws --version` で `aws-cli/2.36.48` を確認。
