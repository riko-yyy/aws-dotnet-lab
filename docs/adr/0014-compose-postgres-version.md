# ADR-0014: PostgreSQLのバージョン固定

## 論点
ローカル用PostgreSQLイメージのバージョンをどう指定するか。

## 選択肢
- `postgres:17`固定
- `postgres:18`固定(Docker公式の[.NETガイド](https://docs.docker.com/guides/dotnet/)の例)
- `latest`

## 決定
`postgres:18`固定。

## 理由
RDS(ADR-0007でPostgreSQLを選定)と同じメジャーバージョンに揃えることで、「ローカルでは動くのに本番(RDS)相当の環境では動かない」というバージョン差異によるトラブルを避ける。

当初、本番のRDSも17だと思い込んで`postgres:17`を選んでいたが、実際に`aws rds describe-db-instances`で確認したところRDSは`18.3`で稼働しており、前提が誤っていた。RDS作成時にバージョンを明示的に指定しなかったため、作成時点のデフォルト(18系)がそのまま採用されていたと考えられる。実態に合わせて`postgres:18`に修正した。

データの保存先(`volumes`のマウント先)も、PostgreSQL 18で`PGDATA`の既定パスが変更されたことに合わせ、`/var/lib/postgresql`に変更した(18未満は`/var/lib/postgresql/data`)。ローカルの既存データは使い捨てのため、移行作業は行わず作り直した。

## 却下した選択肢
- `latest`: メジャーバージョンが上がるタイミングで、既存のボリュームとの互換性が切れる恐れがあるため避けた

## 見直しの条件
RDS側のバージョンを変更する場合は、composeも合わせて変更する。今後RDSのバージョンを確認せずに思い込みで判断しないよう、`aws rds describe-db-instances`で実際の値を確認してから合わせること。
