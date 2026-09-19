# ADR-0007: RDSのDBエンジン選定

## 論点
第3段階でTODO管理APIにRDSを接続するにあたり、どのDBエンジンを使うか。

## 選択肢
- PostgreSQL: OSSコミュニティ主体、標準SQL準拠度が高い、EF Core用ドライバ(Npgsql)がドライバ開発元自身によりメンテナンスされている
- MySQL: Oracle社が所有、Web開発で伝統的に広く使われる、EF Core用プロバイダ(Pomelo)はサードパーティ製
- SQL Server: .NET/Microsoftとの親和性は最も高いが、ライセンスコストがあり学習用途にはオーバースペック

## 決定
PostgreSQLを採用する。

## 理由
- EF Core用ドライバ(`Npgsql.EntityFrameworkCore.PostgreSQL`)がドライバ開発元自身でメンテナンスされており、新しいEF Coreバージョンへの追従が速い(MySQLは非公式のPomeloプロジェクトに依存する)
- AWS RDSでの利用実績・ドキュメントが豊富
- ユーザー自身にPostgreSQL/MySQLどちらも強い経験が無く、決め手となる既存知識が無かったため、上記の技術的な理由を優先した
- 今回の規模(TODO API)であれば、PostgreSQL/MySQLどちらでも機能的な支障は無い

## 却下した選択肢
- MySQL: 上記の通りEF Coreサポートが非公式ドライバ経由になる点以外は大きな差が無く、今回は僅差でPostgreSQLを優先
- SQL Server: ライセンスコストがあり、個人の学習用ラボとしてはオーバースペック
