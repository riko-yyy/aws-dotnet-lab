# ADR-0024: IAM Access Analyzerは外部アクセス分析のみ、手動で有効化する

## 論点1
IAM Access Analyzerをどの範囲で有効にするか。

## 選択肢1
- 外部アクセス分析のみ
- 外部アクセス分析 + 未使用アクセス分析(Unused Access Analyzer)

## 決定1
外部アクセス分析のみ。

## 理由1
外部アクセス分析は無料。未使用アクセス分析は90日間のみ無料で、以降は分析したリソース数に応じた課金が発生するため、学習用ラボの範囲では見送る。

## 見直しの条件1
IAMロールが増えて棚卸しが必要になったら、未使用アクセス分析の導入を再検討する。

## 論点2
Access Analyzer(アカウント単位のリソース)を、プロジェクト単位のTerraform(todo-api-lab)に含めるか、それとも別の方法で管理するか。

## 選択肢2
1. todo-api-labのTerraformに含める(`aws_accessanalyzer_analyzer`リソースを`main.tf`などに追加)
2. アカウント共通のリソースとして、別のTerraform構成(別のstate)を用意する
3. Terraformを使わず、AWSコンソールまたはCLIで手動有効化する

## 決定2
3(コンソール/CLIで手動有効化)。

## 理由2
Access Analyzer(`type = "ACCOUNT"`)はAWSアカウント全体を対象とするリソースであり、プロジェクト単位のtodo-api-labのTerraform stateに含めると、ラボを畳んで`terraform destroy`した際に一緒に削除されてしまう。かといって、このためだけに別のTerraform構成(別バックエンド・別state)を用意するのは、現段階の学習目的(第4段階: todo-apiのインフラのコード化)から外れる。

## 却下した選択肢2
- 選択肢1(todo-api-labに含める): 上記の理由で却下。
- 選択肢2(アカウント共通のTerraform構成): 将来の選択肢として保留。

## 見直しの条件2
今後、アカウント共通のリソース(CloudTrail、AWS Configなど)を複数コード化したくなったら、そのタイミングで専用のTerraform構成を用意する。

## 作業メモ
有効化コマンドの例。

```
aws accessanalyzer create-analyzer \
  --analyzer-name todo-api-external-access \
  --type ACCOUNT \
  --profile dotnet-lab
```

`--profile`は`provider.tf`で使用しているプロファイル(`dotnet-lab`)と揃える。同じサービスが複数のAWSアカウントに分かれない限り、他のプロジェクト用に追加で作成する必要はない(アカウント単位で1つあれば足りる)。
