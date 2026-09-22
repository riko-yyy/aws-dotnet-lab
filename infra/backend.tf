terraform {
  backend "s3" {
    bucket       = "todo-api-terraform-state-737816144781"
    key          = "todo-api/terraform.tfstate"
    region       = "ap-northeast-1"
    profile      = "dotnet-lab"
    use_lockfile = true
  }
}
