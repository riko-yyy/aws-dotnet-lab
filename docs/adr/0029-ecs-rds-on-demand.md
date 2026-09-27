# ADR-0029: ECS版のRDSは普段は削除し、必要なときにTerraformで再作成する

## 論点
サーバーレス版を公開用にしたあと([ADR-0028](0028-serverless-database.md))、ECS版のRDSをどう維持するか。

## 背景
- [ADR-0020](0020-ecs-desired-count-default-zero.md)でECSのタスク数は基本0にしたが、RDSは常に稼働していた。そのため、アクセスがなくても月約$24の待機コストがかかっていた(2026年9月の実績から計算)
- 面接官に見せる公開URLはサーバーレス版が担う。ECS版は常に稼働させておく必要がない

## 選択肢
- 常に稼働させる
- 停止(stop)を繰り返す
- 普段は削除し、必要なときに`terraform apply`で再作成する
- ECS版もDynamoDBに変える

## 決定
普段は削除し、必要なときに`terraform apply`で再作成する。

仕組みとして、変数`ecs_enabled`(既定値は`false`)を用意し、RDS、Secretを読むIAMポリシー、タスク定義、ECSサービスの4つに`count`を付けて、まとめて作ったり消したりする。使うときだけ`terraform apply -var ecs_enabled=true`とする。VPCやECR、ECSクラスターなど無料のリソースは残す。

## 理由
- ストレージ代も含めて、待機コストをゼロにできる
- インフラはコード化済みで、ドリフトもない([ADR-0018](0018-terraform-import-vs-recreate.md))。そのため再現できる。「消しても再現できる」ことが、IaCの価値そのものである
- 過去の判断が、再作成を支えている
  - Secrets ManagerのARNを動的参照にしている([ADR-0022](0022-secretsmanager-arn-dynamic-reference.md))。RDSを作り直してmaster user secretのARNが変わっても、コードを変えずに追従できる(ただし、タスク定義も作り直す場合に限る。下記の「実装時の訂正」を参照)
  - アプリの起動時にマイグレーションを適用している([ADR-0008](0008-migration-strategy.md))。空のRDSでも、タスクが起動すればテーブルが作られる
- インターフェース1つに実装2つ(RDBとDynamoDB)を持つ意味は、両方が実際の基盤で動くことにある。ECS版もDynamoDBに変えると、RDB実装はローカルでしか使われなくなり、この意味が薄れる

## 却下した選択肢
- 常に稼働させる: 月約$24かかる。公開用途はサーバーレス版が担うので、見合わない
- 停止を繰り返す: 停止中もストレージ代などで月約$3.5かかる。さらに7日で自動的に再開されるので、毎週止め直す手作業が必要になる
- ECS版もDynamoDBに変える: DBを揃えれば実行基盤だけを比較できるので、比較としては純粋になる。しかし上記のとおり、RDB実装が実際の基盤で使われなくなる

## トレードオフ
- 再作成するたびにデータは消える(TODOのサンプルデータなので許容する)
- 再作成には、RDSの作成時間として10分程度かかる

## 却下した実装方法
- `terraform destroy -target=...`で消し、普段の`apply`で作り直す: コードは変えずに済む。しかし、「落としている」状態がコードのどこにも書かれず、普段の`plan`に毎回「RDSを作成します」と出る。うっかり`apply`するとRDSが復活して課金が始まるうえ、`plan`がNo changesにならない状態が続き、本当に注意すべき差分を見落としやすくなる
- ネットワークとアプリでstateを分ける: 境界ははっきりするが、変更とstateの移し替えが大きすぎる

## 実装時の訂正
決定時には、「SecretのARNは動的参照なので、RDSを作り直しても追従できる」と考えていた。実際には、タスク定義は追従しない。

- 第5段階で、タスク定義に`ignore_changes = [container_definitions]`を付けた([ADR-0027](0027-deploy-outside-terraform.md))。Terraformは、既存のタスク定義の中身の更新を無視する
- そのため、RDSを作り直してARNが変わっても、タスク定義は古いARNを持ったまま残る。GitHub Actionsのデプロイも今のタスク定義をコピーするので、古いARNが引き継がれ、タスクの起動時にSecretを読めずに失敗する
- 2026-09-22にRDSを作り直したとき(ADR-0022)に問題が起きなかったのは、`ignore_changes`を付ける前だったから

`ignore_changes`が無視するのは既存リソースの「更新」だけで、新しく「作る」ときにはコードどおりの値が使われる。そのため、タスク定義もRDSと同じ`ecs_enabled`で作り直す対象に含め、セットで作り直されることをコードで保証した。

あわせて、ECSサービスにも`ignore_changes = [task_definition]`を付けた。付けていなかったため、`terraform apply`のたびに、GitHub Actionsがデプロイしたリビジョンから、Terraformが最初に作ったリビジョン(イメージは`latest`)へ巻き戻る状態になっていた(`plan`で`todo-api-task:5 -> todo-api-task:4`の差分として確認)。

## 補足
削除の仕組みを実装するまでの間は、RDSを一時停止してしのいでいる(2026-09-23に停止。7日後の9月30日ごろに自動で再開される)。それまでに実装するか、もう一度停止する。

使うときの手順:
1. `terraform apply -var ecs_enabled=true`(RDS、タスク定義、サービスができる。タスク定義は新しいSecretのARNと`bootstrap_image_tag`で作られる)
2. GitHub Actionsでデプロイし、イメージを最新に差し替える
3. タスク数を1にする([ADR-0020](0020-ecs-desired-count-default-zero.md))。このとき`-var ecs_enabled=true`を付け忘れると、RDSごと消える点に注意
