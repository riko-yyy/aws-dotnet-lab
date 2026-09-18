# 2026-09-18: IAM Identity Centerの「AWSアカウント」表示をユーザーと勘違いした

## つまずいた箇所
IAM Identity Centerの「AWS アカウント」画面で、対象アカウントの行に表示名(`riko_yyy`)とメールアドレス(rootアカウントの登録メール)が表示されており、これを「rootユーザーに権限を割り当てようとしている」と誤解した。

## 原因
「AWS アカウント」一覧のアカウント表示名・メールアドレスが、rootアカウントの登録メールと同じ見た目になるため、行そのものが「ユーザー」であるかのように見えてしまった。実際には以下の2つは完全に別物。
- `riko_yyy`: AWSアカウントそのものの表示名(割り当て**先**)
- `riko-user`: IAM Identity Centerのユーザー(割り当てる**対象**)

IAM Identity Centerのユーザーディレクトリにrootは存在せず、「Identity Center経由でrootに権限を割り当てる」ことはそもそも構造上あり得ない。

## 解決
「AWS アカウント」画面のチェックボックスは割り当て先アカウントの選択であり、その後の「ユーザーまたはグループを割り当て」で別途ユーザー一覧(`riko-user`)から選ぶ、という2段階の操作であることを理解して解消。実際に `riko-user` に `AdministratorAccess` を割り当て、`aws sts get-caller-identity` で `assumed-role/AWSReservedSSO_AdministratorAccess_.../riko-user` を確認できた。
