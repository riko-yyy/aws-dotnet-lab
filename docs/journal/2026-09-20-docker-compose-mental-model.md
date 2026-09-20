# 2026-09-20: docker-compose.ymlの各項目についての学び

## きっかけ
RDSがプライベートサブネットにあり、ローカルの`dotnet run`から接続できないため、`docker-compose.yml`でローカル用のAPI+PostgreSQLをまとめて起動できるようにした(ADR-0010)。その中身を1項目ずつ理解した。

## 学んだこと

### `build`と`image`の使い分け
- `build:`(api側): 自分たちが書いたソースコードから、独自にイメージをビルドする必要がある場合に使う
- `image:`(db側): 既に公開されている出来合いのイメージ(`postgres:17`)をそのままpullして使う場合に使う。中身を自分でカスタマイズする必要が無ければビルド不要

### `environment`の変数名は「誰が決めたか」で変わる
- `api`側の`Db__Host`などは、自分たちのコード(`Program.cs`)が独自に決めた変数名
- `db`側の`POSTGRES_PASSWORD`/`POSTGRES_DB`は、postgres公式イメージの起動スクリプトが認識するよう決められた変数名(Docker Hubの公式ページに一覧がある)
- 同じ`environment`という仕組みでも、実際に使える変数名は「そのイメージが何を認識するか」次第

### `depends_on` + `condition: service_healthy`
- 単に「dbより後にapiを起動する」だけでは不十分(PostgreSQLはコンテナが起動してもすぐには接続を受け付けられないことがある)
- `condition: service_healthy`を付けることで、healthcheckが「Healthy」と判定するまでapiの起動を待たせられる
- ファイル内での定義順序(上下)は関係なく、サービス名で依存関係を解決する

### `healthcheck`の仕組み
- `test`: 実際の確認コマンド(`pg_isready`はPostgreSQL公式が用意する「接続可能か確認する」専用コマンド)
- `interval`/`timeout`/`retries`: どのくらいの頻度・何回失敗したら不健全と判定するか

### `volumes`によるデータ永続化
- コンテナは使い捨てが前提で、コンテナを消すと中のファイルも消える
- 名前付きボリューム(`db-data`)をコンテナ内のパス(`/var/lib/postgresql/data`)にマウントすることで、コンテナを作り直してもデータは残る
- ファイル下部の`volumes:`セクションで宣言し、サービス内の`volumes:`でマウント先を指定する、という2段構え
- `docker compose down`だけではボリュームは消えない。完全初期化したい場合は`-v`オプションが必要

### ローカル用パスワードをgitに平文で置いても良い理由
- パスワードの安全性は「それが何を守っているか」で判断する
- このパスワードはローカルの使い捨てPostgreSQLコンテナしか守っておらず、本物のRDS(Secrets Manager管理)には一切通用しない
- 守る価値のあるものを守っていない鍵なので、平文コミットのリスクは実質ゼロ

### ECRへのpushとComposeは無関係
- `docker compose up --build`はローカル開発用にComposeが独自の名前でイメージをビルドするだけ
- ECRへのpushは`docker build`/`docker push`を直接叩いており、Composeを経由しない
- 両者が共有しているのは元になる`Dockerfile`だけ
