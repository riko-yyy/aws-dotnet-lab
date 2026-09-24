# ADR-0035: コールドスタート対策はJIT+ReadyToRunにとどめ、計測して記録する

## 論点
サーバーレス版のコールドスタートに、どこまで対策するか。

## 背景
ASP.NET CoreのAPIをそのままLambdaに載せるので([ADR-0031](0031-lambda-hosting-model.md))、ホストの起動(DIコンテナ、ルーティング、ミドルウェア)がコールドスタートに含まれる。

## 選択肢
- A: JIT+ReadyToRun。通常のJITに、ReadyToRun(事前コンパイル)の設定を加える
- B: Native AOT。ネイティブコードにコンパイルする
- C: SnapStart。初期化が終わった状態のスナップショットから起動する

## 決定
Aを採用する。そのうえで、コールドスタートの時間を実際に計測して記録する。

## 理由
- ここまでの判断と矛盾しないのは、Aだけ
  - Bは、コードの共有(ADR-0031)とぶつかる。JSONのシリアライズをソース生成に変えるなど、ECS版と共有しているコードに手を入れる必要がある。EF Coreの部分も、Native AOTとの相性が悪い
  - Cは、コンテナイメージ([ADR-0033](0033-serverless-deploy-and-packaging.md))では使えない。SnapStartが対応しているのは、zip形式のマネージドランタイムだけ。また、.NETでSnapStartを使うと、スナップショットの保持に料金がかかり、待機コストをゼロにする方針([ADR-0028](0028-serverless-database.md))ともぶつかる
- ReadyToRunは、ビルドの設定だけで使える。コードは変えずに、起動時のJITコンパイルを減らせる
- 対策を最大にするより、「どの程度遅いのかを数字で知り、そのうえで何を優先して対策を見送ったのか」を説明できる方が、比較の目的に合う

## 却下した選択肢
- B(Native AOT): コールドスタートは大幅に速くなる。しかし、共有しているコードへの影響が大きい
- C(SnapStart): コードを変えずに速くできる。しかし、コンテナイメージでは使えず、待機コストもかかる

## トレードオフ
- Native AOTほどは速くならない。公開URLへの最初のアクセスでは、待ち時間が目立つ可能性がある
- ReadyToRunはDockerfileのビルドのステージ(ECS用とLambda用で共通)で有効にするので、ECS版のイメージにも反映される。ECS版の起動も少し速くなる代わりに、イメージのサイズが少し大きくなる
- ReadyToRunには、対象のCPUアーキテクチャ(`RuntimeIdentifier`)の指定が必要。下記のとおりx86_64(`linux-x64`)とした

## 実装時の決定:CPUアーキテクチャはx86_64
Lambdaは、x86_64とarm64(Graviton)を選べる。x86_64を選んだ。

- ECS版(タスク定義の`cpu_architecture = "X86_64"`)と揃う。比較のときに、「CPUの違い」という別の要素が混ざらない
- 開発機のIntel Macで、Lambda用のイメージをエミュレーションなしでビルドして試せる
- arm64は実行時間あたりの料金が約20%安いが、この規模では無料枠に収まるため([ADR-0036](0036-public-url-protection.md))、実際の差はほぼない

ECS版でも、FargateのARMを選べば同じく約20%安くなる選択肢はあった。ECS版では、ビルドしたマシン(Intel Mac、GitHub Actions)とFargateの既定値がどちらもx86_64で、自然に揃っていたため、選択肢として意識していなかった。ReadyToRunで対象のCPUを指定する必要が出て、初めて明示的に選んだ。

CPUの指定は、次の3か所で揃える。食い違うと、機械語を実行できずに起動に失敗する(ReadyToRunの機械語が合わない場合は、使われずにJITに戻る)。
- Dockerfileのビルド: `dotnet publish -r linux-x64 -p:PublishReadyToRun=true`
- Dockerfileの最終ステージ: `FROM --platform=linux/amd64 ...`(Apple SiliconのMacでビルドしても、x86_64のイメージになる)
- Lambda関数(Terraform): `architectures = ["x86_64"]`

`RuntimeIdentifier`をcsprojではなくDockerfileの`publish`で指定したのは、csprojに書くとローカル(Mac)での`dotnet run`まで`linux-x64`向けになり、動かなくなるため。

## 計測の方法
コールドスタートの時間は、CloudWatch Logsに出力される`REPORT`行の`Init Duration`で確認できる。ReadyToRunの有無など条件を変えて計測し、結果をjournalかこのADRに追記する。
