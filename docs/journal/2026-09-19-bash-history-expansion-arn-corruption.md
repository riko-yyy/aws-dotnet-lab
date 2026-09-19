# 2026-09-19: bashのヒストリ展開でSecrets ManagerのARNが壊れた

## つまずいた箇所
ECSタスク定義にSecrets ManagerのARNを埋め込むJSONを、bashのヒートドキュメント経由でPythonスクリプトに渡して生成したところ、生成されたJSONの一部が破損していた。
```
"valueFrom": "ARN:AWS:SECRETSMANAGER:AP-NORTHEAST-1:...:SECRET:RDS!DB-...-58GHARsername::"
```
本来は `arn:aws:secretsmanager:...:secret:rds!db-...-58GHaR:username::` になるはずが、大文字化された上に `username` が `sername` に欠損していた。

## 原因
RDSが自動生成するSecrets ManagerのARNには `rds!db-...` のように **`!`(感嘆符)** が含まれる。bashのヒストリ展開(history expansion)機能は `!文字列` を「ヒストリ内で直近に実行した、その文字列で始まるコマンド」に置き換えようとする。ヒアドキュメント内で変数展開された結果に `!db-...` という並びが現れたことで、意図せずこの機能が反応し、文字列が別のものに置き換わってしまったと考えられる。

## 解決
1. `set +H` でヒストリ展開機能自体を無効化する
2. `$SECRET_ARN` のようなbash変数展開でPythonスクリプトの中身に直接値を埋め込むのではなく、**環境変数として渡してPython側の`os.environ`で読み込む**(`os.environ["SECRET_ARN"]`)ことで、bashによる文字列解釈・置換を経由させないようにした

## 学び
`!` を含む文字列(今回のようなAWSの自動生成ID、パスワードなど)をbashのヒアドキュメントや二重引用符内で扱う際は、ヒストリ展開に巻き込まれるリスクがある。シェルの文字列展開を経由させず、環境変数経由でプログラムに値を渡すことで安全に回避できる。
