# ADR-0024: SecretsManagerのARNとAWSアカウントIDをハードコードせず動的参照にする

## 論点
RDSのmaster user secretのARN、およびECRイメージURIに含まれるAWSアカウントIDを、コード上でどう指定するか。

## 選択肢
1. ARN・アカウントIDともにベタ書き(旧状態。ARNは同じ値が3箇所に重複、アカウントIDはARN内とECRイメージURIに重複)
2. `aws_db_instance.main.master_user_secret[0].secret_arn`と`data.aws_caller_identity.current.account_id`で動的参照

## 決定
2(動的参照)に加えて、RDSインスタンスを作り直し、旧ARNの値自体を無効化する。

## 理由
ベタ書きは同一のARNが3箇所(IAMポリシー、`Db__Username`、`Db__Password`)に重複しており、RDSを作り直した場合に手動で3箇所を修正する必要がある。動的参照にすればTerraformが自動的に追従する。副次的に、ARNに含まれるAWSアカウントIDがコード上にハードコードされなくなる。

ただし、動的参照に直しても、旧ARNの値自体は既に公開リポジトリのコミット履歴に残ってしまっている。コードを直すだけでは値は変わらないため、RDSインスタンスを作り直し(`terraform apply -replace=aws_db_instance.main`など)、ARNの値そのものを無効化する。

## 却下した選択肢
- ベタ書きのまま維持する案。保守性の観点で採用しなかった。
- コミット履歴からのARN削除(`git filter-repo`等)。ARN単体では実害が小さく(IAMの権限チェックが別途かかるため、ARNを知っているだけではシークレットを読めない)、履歴書き換えのコストに見合わないと判断した。

## 補足
アカウントID自体は、IAMの権限がなければ単体で実害のある情報ではない(パスワードやアクセスキーとは扱いのレベルが異なる)。ARNのように値の無効化(作り直し)までは不要で、コードの書き方を動的参照に揃える対応にとどめる。

気づき: [ADR-0004](0004-iam-authentication.md)でIAM Identity Center(一時認証情報)を採用していたため、今回のようなうっかりコミットが起きても「長期アクセスキーの漏洩」という最も致命的なパターンには至らなかった。もしA案(IAMユーザー+長期アクセスキー)を選んでいたら、同じ「うっかりコミット」でも被害の性質がまったく異なっていた可能性が高い。認証方式の設計判断が、コードのミスの被害範囲を左右した一例。

## 作業メモ
- `master_user_secret`はリストとして返る属性のため`[0]`のインデックス指定が必要。`terraform plan`で変更差分が出ないこと(参照先が同じ値であること)を確認してから`apply`する。
- アカウントIDは`data "aws_caller_identity" "current" {}`を追加し、ECRイメージURIを`"${data.aws_caller_identity.current.account_id}.dkr.ecr.ap-northeast-1.amazonaws.com/todo-api:latest"`に置き換える。
- RDS作り直しの影響: `skip_final_snapshot = true`のため、`tododb`のデータは全て消える(学習用ダミーデータのため想定内)。パスワードも`manage_master_user_password = true`により新しく生成される。ECSタスク定義に新しいARNが反映されるよう再デプロイが必要。作り直しには数分程度かかる。
