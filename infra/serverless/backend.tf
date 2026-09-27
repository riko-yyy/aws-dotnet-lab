terraform {
  # 共通の値は infra/backend.hcl から渡す(ADR-0039)
  #   terraform init -backend-config=../backend.hcl
  backend "s3" {
    key = "todo-api-serverless/terraform.tfstate"
  }
}
