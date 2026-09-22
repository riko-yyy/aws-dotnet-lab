# aws-dotnet-lab

このプロジェクトは、.NET CoreとAWSを使ったインフラ構築を学ぶための個人学習リポジトリです。
「動くプロダクトを作ること」ではなく「意思決定の記録を残すこと」を目的とします。

## 進め方
- 題材は小さいToy API(またはリソースの少ない架空サービス)。プロダクトへの責任を負わない前提で自由に試行錯誤する
- 難易度は段階的に上げる。各段階をクリアしてから次に進む

## 進捗
- [x] 1. ローカルでDocker化した.NET Core APIを動かす([PR #1](https://github.com/riko-yyy/aws-dotnet-lab/pull/1))
- [x] 2. ECS Fargateへのデプロイ([PR #2](https://github.com/riko-yyy/aws-dotnet-lab/pull/2))
- [x] 3. RDSと接続したCRUD実装([PR #4](https://github.com/riko-yyy/aws-dotnet-lab/pull/4))
- [x] 4. Terraformによるインフラのコード化
- [ ] 5. GitHub Actionsによる自動デプロイ

題材はTODO管理API(`src/Todo.Api`)。

## ローカル開発
RDSはプライベートサブネットにあるためローカルから直接繋げない。ローカルではDocker Composeでアプリ+ローカル用PostgreSQLをまとめて起動する([ADR-0010](docs/adr/0010-local-dev-database.md))。
```bash
docker compose up -d --build
curl http://localhost:8080/todos
docker compose down
```

## インフラのコード化(Terraform)
`infra/`配下にTerraformコードがある。第1〜3段階で手動構築したAWSリソースを`terraform import`で取り込み、全リソースがコードと一致(`terraform plan`でNo changes)することを確認済み([ADR-0018](docs/adr/0018-terraform-import-vs-recreate.md)、[ADR-0019](docs/adr/0019-terraform-state-backend.md))。Stateはこのプロジェクト専用のS3バケットに保存している。
```bash
cd infra
terraform init
terraform plan
```

## 記録のルール
- 各段階で、なぜその技術・構成を選んだかをADR(Architecture Decision Record)として [docs/adr/](docs/adr/) に残す
  - 論点/選択肢/決定/理由 の形式で記録(ddd-dojoと同じフォーマット)
  - 却下した選択肢とその理由も残す
- つまずいた箇所、詰まった原因、解決方法も [docs/journal/](docs/journal/) に簡潔にログとして残す

## Claudeへの依頼のスタンス
- 答えを一度に全部渡すのではなく、まず選択肢を提示して一緒に検討する形で進めてほしい
- 「なぜそうするのか」の説明を重視する。手順だけのコピペ的な回答は避ける
- つまずいたときは、原因の切り分け方から一緒に考えてほしい
