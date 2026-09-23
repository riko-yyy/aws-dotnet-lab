# ADR-0031: LambdaにはASP.NET CoreのAPIをそのまま載せる

## 論点
サーバーレス版で、既存のTODO API(`src/Todo.Api`)をLambdaにどう載せるか。

## 選択肢
- A: ASP.NET CoreのAPIをそのまま1つのLambda関数に載せる(`Amazon.Lambda.AspNetCoreServer.Hosting`を使う)
- B: エンドポイントごとにLambda関数を分ける(5関数。`Amazon.Lambda.Annotations`などを使う)

## 決定
Aを採用する。`Program.cs`に`AddAWSLambdaHosting()`を追加して、ECS版と同じエンドポイントのコードをLambdaで動かす。

## 理由
- サーバーレス版の目的は、「同じAPIを別の基盤に載せたら何が変わるか」を示すこと。基盤に依存する部分だけを差し替えたいので、アプリのコード(エンドポイント)はECS版と共有する。データアクセスの差し替え([ADR-0030](0030-extract-todo-store.md))と同じ考え方
- `AddAWSLambdaHosting()`は、Lambdaの外(ローカルやECS)で動いたときには何もせず、通常どおりKestrelで起動する。そのため、1つのコードでECS、Lambda、docker composeのどれでも動く
- 仕組みとしては、LambdaからのイベントをHTTPリクエストに変換して、ASP.NET Coreのパイプラインに渡すアダプタが挟まるだけ。アプリ側は、Lambdaで動いていることをほとんど意識しない

## 却下した選択肢
- B: Lambdaの性質に合わせた作りにはなる。しかし、エンドポイントをLambda用に書き直すことになり、「インフラ層だけを差し替えた」という目的と矛盾する

## トレードオフ(Bを選ばなかったことで手放すもの)
- 関数ごとにIAMの権限を絞れない。1つの関数がCRUDすべてに必要な権限(DynamoDBの読み書き)を持つ
- コールドスタートが重くなる。ASP.NET Coreのホスト(DIコンテナ、ルーティング、ミドルウェア)の起動が、初回の呼び出しに含まれる。対策は、Native AOTを採用するかどうかと合わせて別途判断する
- 入口が渡すイベントの形式と、`AddAWSLambdaHosting()`に指定するイベントの種類をそろえる必要がある。API GatewayのHTTP APIを使うので([ADR-0032](0032-serverless-entry-point.md))、`LambdaEventSource.HttpApi`を指定する
