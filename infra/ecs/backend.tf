terraform {
  # バケット名などの共通の値は、infra/backend.hcl から渡す(partial configuration)。
  #   terraform init -backend-config=../backend.hcl
  # backendブロックでは変数を使えないため、この仕組みで外から渡している。キーだけはstateごとにここで決める
  backend "s3" {
    key = "todo-api/terraform.tfstate"
  }
}
