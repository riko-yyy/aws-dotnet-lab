# ADR-0027: デプロイはTerraform管理外で直接ECSを更新する

## 論点
GitHub ActionsからECSタスク定義を新しいイメージに更新する際、その変更をTerraformの管理下で行うか、管理外で行うか。

## 選択肢
- A案: Terraform管理外で、CIが直接`ecs:RegisterTaskDefinition`と`ecs:UpdateService`を呼ぶ。イメージタグはgit SHAなど一意の値にする
- B案: CIから`terraform apply`を実行し、`main.tf`内のイメージタグを書き換えて適用する

## 決定
A案(Terraform管理外で直接更新)。`aws_ecs_task_definition.app`に`lifecycle { ignore_changes = [container_definitions] }`を追加し、Terraformが`container_definitions`を比較・上書きしないようにする。

## 理由
- Terraformは「インフラの骨格(VPC/ECS/RDSなど)」の管理に専念させ、CI/CDは「アプリのリリース」を担当する、という実務で一般的な責任分担を体験できる
- CIに付与するIAM Roleの権限を、ECR push・ECSタスク登録・サービス更新程度に絞れる(B案だとTerraformが触る全リソースへの強い権限が必要になる)
- デプロイのたびにinfra全体の`terraform plan/apply`が走らないため、手元でのTerraform操作(`desired_count`の切り替えなど、ADR-0020)と衝突するリスクが小さい

## 却下した選択肢
- B案(CIから`terraform apply`): `main.tf`と実際の状態が常に一致するメリットはあるが、CIに広い権限を持たせる必要があり、認証方式(ADR-0025)で狙った最小権限の思想と合わない

## 補足
`ignore_changes`を設定すると、以後`main.tf`の`container_definitions`(環境変数など)を編集しても`terraform plan/apply`では反映されなくなる。反映するには一時的に`ignore_changes`を外すか、変更内容によってはCI側のワークフローに組み込む必要がある。

`ecs:RegisterTaskDefinition`はIAMのリソースレベル権限(ARN指定)に対応していないAPIのため、CIのデプロイ用IAM Roleではこのアクションのみ`resources = ["*"]`とせざるを得ない。ECRへのpushとECSサービスの更新(`ecs:UpdateService`)は対象リソースをARNで絞っている。

`container_definitions`はTerraformの型では1つのJSON文字列として扱われる属性であり、`ignore_changes`はリソースの属性単位でしか指定できない。そのため「imageだけ無視して環境変数などはTerraformで管理し続ける」といった部分的な指定はできず、`container_definitions`全体(image・環境変数・secrets・ログ設定など)が対象になる。`main.tf`上の`container_definitions`は、以後は「最初にこのタスク定義を作った時の初期値」としての意味しか持たない。

この「初期値でしかない」という性質を`main.tf`上で表現するため、imageタグを`:latest`と直書きせず`variable "bootstrap_image_tag"`(デフォルト値`"latest"`)として切り出し、`description`に「初回apply時のみ使われ、以降はGitHub Actionsが管理する」旨を明記した。デフォルト値を`"latest"`のまま(実在しない値に変えない)にしているのは、CIを一度も実行しないまま`desired_count`を1にした場合でも、ECRに実在するイメージをpullできるようにするため。

## 見直しの条件
複数人での開発や、承認ゲートを挟んだ本番運用を試したくなったら、B案寄りの「CIからのterraform apply + 手動承認」構成を再検討する。
