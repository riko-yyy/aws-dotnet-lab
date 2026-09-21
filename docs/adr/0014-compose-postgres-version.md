# ADR-0014: PostgreSQLのバージョン固定

## 論点
ローカル用PostgreSQLイメージのバージョンをどう指定するか。

## 選択肢
- `postgres:17`固定(現状)
- `postgres:18`固定(Docker公式の[.NETガイド](https://docs.docker.com/guides/dotnet/)の例)
- `latest`

## 決定
`postgres:17`固定のまま。

## 理由
RDS(ADR-0007でPostgreSQLを選定)と同じメジャーバージョンに揃えることで、「ローカルでは動くのに本番(RDS)相当の環境では動かない」というバージョン差異によるトラブルを避ける。データの保存先(`volumes`のマウント先)も、PostgreSQL 17時点のパス`/var/lib/postgresql/data`のままにしている。

## 却下した選択肢
- `latest`: メジャーバージョンが上がるタイミングで、既存のボリューム(ADR的にはdocs/journal参照)との互換性が切れる恐れがあるため避けた

## 見直しの条件
RDS側のバージョンを変更する場合は、composeも合わせて変更する。もし18に上げる場合、公式ガイドの例ではデータのマウント先が`/var/lib/postgresql`に変わっている点にも注意する。
