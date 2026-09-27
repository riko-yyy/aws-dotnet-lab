variable "bootstrap_image_tag" {
  description = "Lambda関数を新しく作るときにだけ使う初期イメージのタグ。以降の差し替えはGitHub Actionsが行う(ADR-0033)"
  type        = string
  default     = "e3519175e864af62a58823ed726ab20a486b2799"
}

locals {
  name       = "todo-api"
  table_name = "todo-api-todos"
}

data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

# ---------- ECR ----------

resource "aws_ecr_repository" "lambda" {
  name = "todo-api-lambda"
}

data "aws_iam_policy_document" "ecr_lambda_pull" {
  statement {
    effect  = "Allow"
    actions = ["ecr:BatchGetImage", "ecr:GetDownloadUrlForLayer", "ecr:SetRepositoryPolicy", "ecr:DeleteRepositoryPolicy", "ecr:GetRepositoryPolicy"]
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
    condition {
      test     = "StringLike"
      variable = "aws:sourceArn"
      values   = ["arn:aws:lambda:${data.aws_region.current.region}:${data.aws_caller_identity.current.account_id}:function:*"]
    }
    sid = "LambdaECRImageRetrievalPolicy"
  }
  version = "2008-10-17"
}

resource "aws_ecr_repository_policy" "lambda" {
  repository = aws_ecr_repository.lambda.name
  policy     = data.aws_iam_policy_document.ecr_lambda_pull.json
}

# ---------- DynamoDB ----------

resource "aws_dynamodb_table" "todos" {
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "id"
  name         = local.table_name
  attribute {
    name = "id"
    type = "S"
  }
  on_demand_throughput {
    max_read_request_units  = 10
    max_write_request_units = 10
  }
  ttl {
    attribute_name = "expiresAt"
    enabled        = true
  }
}

# ---------- Lambda ----------

data "aws_iam_policy_document" "lambda_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
  version = "2012-10-17"
}

resource "aws_iam_role" "lambda" {
  name               = "todo-api-role-oxf1dgyu"
  path               = "/service-role/"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume_role.json
}

data "aws_iam_policy_document" "lambda_basic_execution" {
  statement {
    effect    = "Allow"
    actions   = ["logs:CreateLogGroup"]
    resources = ["arn:aws:logs:${data.aws_region.current.region}:${data.aws_caller_identity.current.account_id}:*"]
  }
  statement {
    effect    = "Allow"
    actions   = ["logs:CreateLogStream", "logs:PutLogEvents"]
    resources = ["${aws_cloudwatch_log_group.lambda.arn}:*"]
  }
  version = "2012-10-17"
}

resource "aws_iam_policy" "lambda_basic_execution" {
  name   = "AWSLambdaBasicExecutionRole-e6f02b6e-4fec-47d3-8cc3-de496ede5e52"
  path   = "/service-role/"
  policy = data.aws_iam_policy_document.lambda_basic_execution.json
}

data "aws_iam_policy_document" "lambda_dynamodb" {
  statement {
    sid       = "TodoTable"
    effect    = "Allow"
    actions   = ["dynamodb:GetItem", "dynamodb:PutItem", "dynamodb:UpdateItem", "dynamodb:DeleteItem", "dynamodb:Scan"]
    resources = [aws_dynamodb_table.todos.arn]
  }
  version = "2012-10-17"
}

resource "aws_iam_role_policy" "lambda_dynamodb" {
  name   = "todo-api-dynamodb-access"
  role   = aws_iam_role.lambda.name
  policy = data.aws_iam_policy_document.lambda_dynamodb.json
}

resource "aws_iam_role_policy_attachment" "lambda_basic_execution" {
  role       = aws_iam_role.lambda.name
  policy_arn = aws_iam_policy.lambda_basic_execution.arn
}

resource "aws_cloudwatch_log_group" "lambda" {
  name              = "/aws/lambda/${local.name}"
  retention_in_days = 14
}

resource "aws_lambda_function" "api" {
  function_name                  = local.name
  role                           = aws_iam_role.lambda.arn
  architectures                  = ["x86_64"]
  image_uri                      = "${aws_ecr_repository.lambda.repository_url}:${var.bootstrap_image_tag}"
  memory_size                    = 512
  package_type                   = "Image"
  reserved_concurrent_executions = 5
  timeout                        = 10
  environment {
    variables = {
      DynamoDb__TableName = aws_dynamodb_table.todos.name
      Storage__Provider   = "DynamoDb"
    }
  }
  logging_config {
    log_format = "Text"
    log_group  = aws_cloudwatch_log_group.lambda.name
  }
  lifecycle {
    ignore_changes = [image_uri]
  }
}

# ---------- API Gateway(HTTP API) ----------

resource "aws_lambda_permission" "apigateway" {
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.api.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.http.execution_arn}/*/$default"
  statement_id  = "67173fe7-7657-5704-91f2-d0582db893a4"
}

resource "aws_apigatewayv2_api" "http" {
  name          = local.name
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_integration" "lambda" {
  api_id                 = aws_apigatewayv2_api.http.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.api.arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "default" {
  api_id    = aws_apigatewayv2_api.http.id
  route_key = "$default"
  target    = "integrations/${aws_apigatewayv2_integration.lambda.id}"
}

resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.http.id
  auto_deploy = true
  name        = "$default"
  default_route_settings {
    throttling_burst_limit = 5
    throttling_rate_limit  = 1
  }
}
