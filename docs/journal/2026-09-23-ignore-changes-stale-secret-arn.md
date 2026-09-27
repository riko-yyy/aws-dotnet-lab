# 2026-09-23: `ignore_changes`を付けたタスク定義は、RDSを作り直しても古いSecretのARNを持ち続ける

## つまずいた箇所
ECS版のRDSを「普段は削除し、必要なときに`terraform apply`で作り直す」方針にするにあたり([ADR-0029](../adr/0029-ecs-rds-on-demand.md))、当初は「SecretのARNは動的参照にしてあるので([ADR-0022](../adr/0022-secretsmanager-arn-dynamic-reference.md))、RDSを作り直してもコードを変えずに追従できる」と考えていた。前日(2026-09-22)に`terraform apply -replace=aws_db_instance.main`でRDSを作り直したときも、実際に問題なく追従していた。

実装のために`main.tf`を読み直したところ、次にRDSを作り直すと、ECSのタスクが起動時にSecretを読めずに失敗することがわかった。

## 原因
第5段階で、タスク定義に`lifecycle { ignore_changes = [container_definitions] }`を付けていた([ADR-0027](../adr/0027-deploy-outside-terraform.md))。アプリのデプロイをGitHub Actionsに任せるための設定で、Terraformは既存のタスク定義の中身の**更新**を無視する。

- RDSを作り直すと、RDSが管理するSecret(`rds!db-<ランダムなID>-<ランダムな文字>`)も作り直され、ARNが変わる
- 本来ならタスク定義の`secrets`も新しいARNに更新が必要だが、`ignore_changes`で無視されるので古いARNのまま残る
- GitHub Actionsのデプロイは「今のタスク定義をコピーして、イメージだけ差し替える」ので、古いARNはデプロイのたびに引き継がれる
- `apply`の時点ではエラーにならず、**タスクの起動時に**初めて失敗する

前日の作り直しで問題が起きなかったのは、順番の問題だった。

| 時刻(2026-09-22) | 出来事 |
|---|---|
| 19:18ごろ | SecretのARNを動的参照に変更し、RDSを作り直す → タスク定義も新しいARNに更新された |
| 23:04 | 第5段階で`ignore_changes`を追加 → 以降、タスク定義の中身は更新されなくなった |

さらに、読み取り専用の`terraform plan`を実行したところ、別の問題も見つかった。

```
aws_ecs_service.app: task_definition = "todo-api-task:5" -> "todo-api-task:4"
```

ECSサービスの「どのリビジョンを使うか」には`ignore_changes`を付けていなかった。そのため、`terraform apply`のたびに、GitHub Actionsがデプロイしたリビジョン5から、Terraformが最初に作ったリビジョン4(イメージは`latest`)へ巻き戻る状態になっていた。Terraformは、サービスがリビジョン5を使っていることを把握したうえで、コードの記述(リビジョン4)に合わせるべき「ずれ」と判断していた。

## 解決
- 変数`ecs_enabled`を追加し、RDS、Secretを読むIAMポリシー、タスク定義、ECSサービスの4つに`count`を付けて、**セットで**作ったり消したりするようにした。`ignore_changes`が無視するのは既存リソースの「更新」だけで、新しく「作る」ときにはコードどおりの値が使われる。そのため、タスク定義ごと作り直せば、新しいSecretのARNで作られる
- ECSサービスに`ignore_changes = [task_definition]`を追加した。`ecs_enabled=true`での`plan`で、リビジョン5→4への巻き戻しの差分が消えたことを確認した
- `count`でアドレスが`main`から`main[0]`に変わる分は、`moved`ブロックでstateの付け替えだけにした

## 学び
- `ignore_changes`は「この項目は別の担当が変える」という宣言だが、その項目が**別のリソースの値に依存している**場合、依存先が変わっても追従しなくなる。付けるときは「この項目は何を参照しているか」まで確認する
- 「前回うまくいった」は、その後に前提が変わっていれば根拠にならない。今回は、成功した作り直しと`ignore_changes`の追加の間がわずか4時間だった
- Terraformとほかの仕組み(ここではGitHub Actions)で担当を分けるときは、**1つの項目の担当が1つだけ**になっているかを確認する。サービスの「どのリビジョンを使うか」がTerraformとActionsの両方の担当になっていたことが、巻き戻りの原因だった
- 普段から`terraform plan`を実行して「No changes」になることを確かめておくと、今回の巻き戻りのような隠れた問題に気づける
