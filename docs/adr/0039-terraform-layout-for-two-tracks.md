# ADR-0039: TerraformはECS版とサーバーレス版でディレクトリとstateを分け、backendの共通の値は外から渡す

## 論点
サーバーレス版をTerraformで管理するにあたり([ADR-0033](0033-serverless-deploy-and-packaging.md))、ECS版とどう分けるか。また、backendのバケット名などを直接書いている今の形をどうするか。

## 背景
- ECS版のstateは、S3バケット`todo-api-terraform-state-<アカウントID>`のキー`todo-api/terraform.tfstate`にある([ADR-0019](0019-terraform-state-backend.md))
- `backend.tf`には、バケット名、リージョン、プロファイルを直接書いていた。backendブロックでは変数を使えないため
- READMEのライセンスの節では、「他の人が使う場合は、これらを書き換える必要がある」と注意書きで対応していた(PR #13)。そのときに、「値を外から渡す形にするのは、backendの設定が2つになるサーバーレス版のときにやる」と決めていた

## 決定
1. ディレクトリ: `infra/ecs/`(今の`infra/`を移動)と`infra/serverless/`に分ける
2. state: 同じバケットの別のキーにする。ECS版のキー(`todo-api/terraform.tfstate`)は変えず、サーバーレス版は`todo-api-serverless/terraform.tfstate`とする
3. backendの値: バケット名、リージョン、プロファイル、ロックの設定は`infra/backend.hcl`の1か所に書き、`terraform init -backend-config=../backend.hcl`で渡す(partial configuration)。キーだけは、それぞれの`backend.tf`に書く。`backend.hcl`はgit管理外とし、見本の`backend.hcl.example`をコミットする

## 理由
- ディレクトリを対等に並べることで、「同じAPIを2つの基盤に載せた」構成がそのまま見える。READMEを2本立てにする方針とも合う
- ディレクトリを移動しても、stateはS3にあるので影響しない。移動先で`init`し直せばよい。移動後に`terraform plan`がNo changesになることを確認した
- ECS版のキーを変えると、stateの移行が必要になる。移行の手間とリスクに見合う利点がないので、変えない
- stateを分けることで、片方の`plan`や`apply`が、もう片方のリソースに触れない。サーバーレス版の作業中に、ECS版のリソースを誤って変える心配がない
- backendの共通の値を1か所にまとめると、2つのstateで値がずれることがない。他の人が使うときも、`backend.hcl`を1つ書くだけで済む

## 却下した選択肢
- `infra/`(ECS版)はそのままにして、`infra-serverless/`を追加する: 既存のものに触らずに済むが、片方だけが特別扱いに見える
- 1つのstateにまとめる: ディレクトリを分けても、stateが1つだと片方の変更がもう片方の`plan`に出る。引き継ぎの時点で「stateは分ける」と決めていた
- backendの値を、それぞれの`backend.tf`に直接書いたままにする: 同じ値を2か所に書くことになり、片方だけ直すおそれがある

## トレードオフ
- `terraform init`のたびに`-backend-config=../backend.hcl`を付ける必要がある。付け忘れると、バケット名が分からずにエラーになる(誤ったstateに接続することはない)
- `backend.hcl`はgit管理外なので、手元にしかない。消えた場合は`backend.hcl.example`から作り直す
- 値を外に出したのはbackendだけで、`provider.tf`のプロファイルとリージョン、OIDCの`sub`の条件は、まだ直接書いている。これらはbackendと違って変数や環境変数で扱えるので、必要になったら個別に対応する

## 補足:2つのstateにまたがる依存
GitHub ActionsのOIDCプロバイダー(`aws_iam_openid_connect_provider`)は、同じURLに対してアカウントに1つしか作れない。今はECS版のstateが管理している。

サーバーレス版のデプロイ用のIAMロールもこれを使うので、サーバーレス版からは`data`ブロックで参照するだけにする。そのため、ECS版を`terraform destroy`すると、OIDCプロバイダーも削除され、サーバーレス版のデプロイも動かなくなる。

普段のECS版は`ecs_enabled`で一部を落とすだけで([ADR-0029](0029-ecs-rds-on-demand.md))、OIDCプロバイダーは残るので、この依存は問題にならない。ECS版をまるごと`destroy`することになったら、先にOIDCプロバイダーをサーバーレス版か、共通のstateに移す必要がある。
