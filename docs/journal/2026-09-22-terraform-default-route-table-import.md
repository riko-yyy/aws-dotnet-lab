# 2026-09-22: `aws_default_route_table`のimportはルートテーブルIDではなくVPC IDを渡す

## つまずいた箇所
VPC作成時に自動生成される「メイン」ルートテーブル(`rtb-0a63aeec87f4e660e`)を`aws_default_route_table`としてimportしようとしたところ、以下のエラーになった。
```
terraform import aws_default_route_table.main rtb-0a63aeec87f4e660e
...
Error: empty result
```
`aws_route_table`や他のリソースと同じ感覚で、ルートテーブルのID(`rtb-...`)をそのまま渡していた。

## 原因
`TF_LOG=DEBUG`で実際のAPIリクエストを確認したところ、以下のようになっていた。
```
Action=DescribeRouteTables&Filter.1.Name=association.main&Filter.1.Value.1=true&Filter.2.Name=vpc-id&Filter.2.Value.1=rtb-0a63aeec87f4e660e
```
`vpc-id`というフィルタに、ルートテーブルのID(`rtb-...`)がそのまま渡されてしまっていた。つまり`aws_default_route_table`のimportは、**渡した値をルートテーブルIDではなくVPC IDとして扱う**仕様だった。「このVPCの、現在のメインルートテーブルを引き取る」という考え方のリソースなので、ルートテーブル側からではなくVPC側から検索する動きになっている。

## 解決
VPCのIDを渡すことで解決した。
```bash
terraform import aws_default_route_table.main vpc-08cb69c9fdf1219fc
```

## 学び
- `aws_route_table`(自分で作る通常のルートテーブル)と`aws_default_route_table`(VPC作成時に自動生成されるメインルートテーブルを引き取る特殊なリソース)は、似た名前・似たスキーマでもimport IDの意味が異なる
- エラーメッセージ(`empty result`)だけでは原因がわからず、`TF_LOG=DEBUG`で実際のAPIリクエスト内容を見て初めて「渡した値がどのパラメータとして使われているか」が判明した。挙動が読めないTerraformのエラーに遭遇したら、まずデバッグログで実際のAPI呼び出しを見るのが有効
