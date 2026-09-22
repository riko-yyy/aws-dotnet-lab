# ADR-0025: GitHub ActionsのAWS認証はOIDC連携(IAM Role)にする

## 論点
第5段階(GitHub Actionsによる自動デプロイ)で、GitHub ActionsからAWSを操作する際の認証方式をどうするか。

## 選択肢
- A案: IAM Identity Provider(OIDC)を作成し、GitHub Actionsが一時的な認証情報でIAM Roleを引き受ける
- B案: IAMユーザーを作成し、長期のアクセスキーを発行してGitHub Secretsに保存する

## 決定
A案(OIDC連携)。

## 理由
- B案は長期の認証情報をリポジトリの外(GitHub Secrets)に置くことになり、ADR-0004でローカル開発の認証方式としてIAM Identity Center(一時認証情報)を選び、長期アクセスキーを避けた判断と矛盾する
- OIDC連携はAWS・GitHub双方が現在推奨する方式であり、実務でも主流になりつつあるため学習価値が高い
- ワークフロー実行のたびに短期間だけ有効な認証情報が発行されるため、万が一ログや設定が漏れても被害が限定的

## 却下した選択肢
- B案(IAMユーザー+長期アクセスキー): セットアップは簡単だが、ADR-0004と同様の理由(長期認証情報の漏洩リスク)で見送り

## 補足
信頼ポリシーの条件は、GitHubのOIDCトークンに含まれる`sub`クレームを`repo:riko-yyy/aws-dotnet-lab:ref:refs/heads/main`に限定する。手動実行(`workflow_dispatch`、ADR-0026)であっても、実行時にどのブランチを選んだかが`sub`に反映されるため、mainブランチ以外から実行してもロールを引き受けられないようにする多重の防御。
