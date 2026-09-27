terraform {
  required_version = ">= 1.10"
  required_providers {
    aws = {
      source = "hashicorp/aws"
      # DynamoDBのオンデマンドの最大スループット(on_demand_throughput)などを使うため、動作を確認したバージョン以上に固定する
      version = ">= 6.65"
    }
  }
}

provider "aws" {
  region  = "ap-northeast-1"
  profile = "dotnet-lab"
}
