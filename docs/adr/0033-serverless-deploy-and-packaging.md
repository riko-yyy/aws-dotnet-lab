# ADR-0033: サーバーレス版もTerraformで管理し、Lambdaはコンテナイメージで動かす

## 論点
サーバーレス版を何でデプロイするか。また、Lambdaにはどの形式でコードを渡すか。

## 選択肢
デプロイの手段:
- Terraform(ECS版と同じ道具。stateは分ける)
- SAM(CloudFormationをベースにした、Lambda専用のツール)
- CDK(インフラをC#で書く)

コードの形式:
- zip+マネージドランタイム(`dotnet10`)
- コンテナイメージ(ECRからpullする)

## 決定
- Terraformで管理する。stateはECS版と分ける
- Lambdaはコンテナイメージで動かす。ECRのリポジトリは、ECS版とは別に用意する(`todo-api-lambda`)
- アプリのリリースは、ECS版と同じようにTerraformの管理外で、CIが行う([ADR-0027](0027-deploy-outside-terraform.md))。CIはイメージをECRにpushし、`aws lambda update-function-code --image-uri`でLambdaを更新する。Terraform側は、イメージの変更を無視する設定にする

## 理由
- デプロイの方式を、できる範囲でECS版と同じにする。違いを実行基盤だけに絞るためで、[ADR-0030](0030-extract-todo-store.md)、[ADR-0031](0031-lambda-hosting-model.md)と同じ考え方
  - IaCの道具は、ECS版と同じTerraform
  - 成果物は、ECS版と同じコンテナイメージ。ビルドして、ECRにpushし、git SHAでタグを付けるところまで同じで、最後の更新コマンドだけが違う
  - 役割分担も同じ。Terraformはインフラの骨格を、CIはアプリのリリースを担う
- コンテナイメージでは`runtime`を指定しない。そのため、「Terraformのaws providerが`dotnet10`を認識しない」という問題は、そもそも関係がなくなる(なお、使っているprovider 6.65.0は`dotnet10`に対応していることを確認済み)
- ECRのリポジトリを分けるのは、ECS用とLambda用のイメージを取り違えないため。2つのイメージはベースイメージが違い、互いの基盤では動かない

## 却下した選択肢
- SAM: `sam build`でパッケージ化まで行え、Lambdaの環境をローカルで再現できる。しかし、1つのリポジトリにIaCの道具が2つになり、stateの置き場所もCloudFormationのスタックに分かれる。ローカルでの動作確認は、ASP.NET Coreとして動かせるので(ADR-0031)、SAMでなくても困らない
- CDK: .NETエンジニアには馴染みやすいが、3つ目の道具になり、基盤だけを比べるという目的から外れる
- zip+マネージドランタイム: 「ECSはコンテナ、Lambdaはマネージドランタイム」という対比にはなる。しかし、デプロイの流れがECS版と大きく変わる

## トレードオフ
- ECRのストレージ代がかかる(数百MBで月に数円程度)。待機コストがゼロという方針からわずかに外れるが、許容する
- Lambda用のベースイメージが必要なので、Dockerfileに手を入れる([ADR-0034](0034-dockerfile-multi-target.md))
- ECS版の`bootstrap_image_tag`と同じ「鶏と卵」の問題がある。コンテナイメージのLambdaは、作成するときにECRにイメージが存在している必要がある。初回の`terraform apply`の前に、イメージを1つpushしておく必要がある。具体的な手順は実装するときに決める
- Lambdaは、`update-function-code`を実行した時点でタグをイメージのダイジェストに変換して固定する。同じタグにpushし直しても、Lambdaは古いイメージのまま動く。そのため、ECS版と同じようにgit SHAなど一意のタグを使う([ADR-0006](0006-ecr-tag-mutability.md))
