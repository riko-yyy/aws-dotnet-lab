# aws-dotnet-lab

このプロジェクトは、.NET CoreとAWSを使ったインフラ構築を学ぶための個人学習リポジトリです。
「動くプロダクトを作ること」ではなく「意思決定の記録を残すこと」を目的とします。

## 進め方
- 題材は小さいToy API(またはリソースの少ない架空サービス)。プロダクトへの責任を負わない前提で自由に試行錯誤する
- 難易度は段階的に上げる。各段階をクリアしてから次に進む

## 進捗
- [x] 1. ローカルでDocker化した.NET Core APIを動かす([PR #1](https://github.com/riko-yyy/aws-dotnet-lab/pull/1))
- [x] 2. ECS Fargateへのデプロイ([PR #2](https://github.com/riko-yyy/aws-dotnet-lab/pull/2))
- [ ] 3. RDSと接続したCRUD実装
- [ ] 4. Terraformによるインフラのコード化
- [ ] 5. GitHub Actionsによる自動デプロイ

題材はTODO管理API(`src/Todo.Api`)。

## 記録のルール
- 各段階で、なぜその技術・構成を選んだかをADR(Architecture Decision Record)として [docs/adr/](docs/adr/) に残す
  - 論点/選択肢/決定/理由 の形式で記録(ddd-dojoと同じフォーマット)
  - 却下した選択肢とその理由も残す
- つまずいた箇所、詰まった原因、解決方法も [docs/journal/](docs/journal/) に簡潔にログとして残す

## Claudeへの依頼のスタンス
- 答えを一度に全部渡すのではなく、まず選択肢を提示して一緒に検討する形で進めてほしい
- 「なぜそうするのか」の説明を重視する。手順だけのコピペ的な回答は避ける
- つまずいたときは、原因の切り分け方から一緒に考えてほしい
