resource "aws_vpc" "main" {
  cidr_block = "10.0.0.0/16"

  tags = {
    Name = "todo-api-vpc"
  }
}

resource "aws_subnet" "public_1a" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.1.0/24"
  availability_zone = "ap-northeast-1a"

  tags = {
    Name = "todo-api-public-1a"
  }
}

resource "aws_subnet" "private_1a" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.11.0/24"
  availability_zone = "ap-northeast-1a"

  tags = {
    Name = "todo-api-private-1a"
  }
}

resource "aws_subnet" "private_1c" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.12.0/24"
  availability_zone = "ap-northeast-1c"

  tags = {
    Name = "todo-api-private-1c"
  }
}

resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name = "todo-api-igw"
  }
}

resource "aws_route_table" "private" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name = "todo-api-private-rt"
  }
}

resource "aws_route_table_association" "private_1a" {
  subnet_id      = aws_subnet.private_1a.id
  route_table_id = aws_route_table.private.id
}

resource "aws_route_table_association" "private_1c" {
  subnet_id      = aws_subnet.private_1c.id
  route_table_id = aws_route_table.private.id
}

resource "aws_default_route_table" "main" {
  default_route_table_id = aws_vpc.main.default_route_table_id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.main.id
  }
}

resource "aws_route_table_association" "public_1a" {
  subnet_id      = aws_subnet.public_1a.id
  route_table_id = aws_default_route_table.main.id
}

resource "aws_security_group" "app" {
  name        = "todo-api-sg"
  description = "TODO API access"
  vpc_id      = aws_vpc.main.id

  ingress {
    from_port   = 8080
    to_port     = 8080
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "db" {
  name        = "todo-api-db-sg"
  description = "RDS access from ECS"
  vpc_id      = aws_vpc.main.id

  ingress {
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.app.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_ecr_repository" "app" {
  name = "todo-api"
}

resource "aws_ecs_cluster" "main" {
  name = "todo-api-cluster"

  configuration {
    execute_command_configuration {
      logging = "DEFAULT"
    }
  }
}

data "aws_caller_identity" "current" {}

data "aws_iam_policy_document" "ecs_task_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "task_execution" {
  name               = "todo-api-task-execution-role"
  description        = "Allows ECS tasks to call AWS services on your behalf."
  assume_role_policy = data.aws_iam_policy_document.ecs_task_assume_role.json
}

data "aws_iam_policy_document" "db_secret_read" {
  statement {
    sid       = "VisualEditor0"
    effect    = "Allow"
    actions   = ["secretsmanager:GetSecretValue"]
    resources = [aws_db_instance.main.master_user_secret[0].secret_arn]
  }
}

resource "aws_iam_role_policy" "db_secret_read" {
  name   = "todo-api-db-secret-read"
  role   = aws_iam_role.task_execution.id
  policy = data.aws_iam_policy_document.db_secret_read.json
}

resource "aws_ecs_task_definition" "app" {
  family                   = "todo-api-task"
  requires_compatibilities = ["FARGATE"]
  network_mode             = "awsvpc"
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.task_execution.arn

  runtime_platform {
    cpu_architecture        = "X86_64"
    operating_system_family = "LINUX"
  }

  container_definitions = jsonencode([
    {
      name      = "todo-api"
      image     = "${data.aws_caller_identity.current.account_id}.dkr.ecr.ap-northeast-1.amazonaws.com/todo-api:latest"
      essential = true

      portMappings = [
        {
          containerPort = 8080
          hostPort      = 8080
          protocol      = "tcp"
          name          = "todo-api-8080-tcp"
          appProtocol   = "http"
        }
      ]

      environment = [
        { name = "Db__Port", value = "5432" },
        { name = "Db__Host", value = "todo-api-db.cdwgqdzz9nbe.ap-northeast-1.rds.amazonaws.com" },
        { name = "Db__Name", value = "tododb" },
      ]
      environmentFiles = []
      mountPoints      = []
      volumesFrom      = []

      secrets = [
        {
          name      = "Db__Username"
          valueFrom = "${aws_db_instance.main.master_user_secret[0].secret_arn}:username::"
        },
        {
          name      = "Db__Password"
          valueFrom = "${aws_db_instance.main.master_user_secret[0].secret_arn}:password::"
        },
      ]
      ulimits        = []
      systemControls = []

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = "/ecs/todo-api-task"
          "awslogs-region"        = "ap-northeast-1"
          "awslogs-stream-prefix" = "ecs"
          "awslogs-create-group"  = "true"
        }
        secretOptions = []
      }
    }
  ])
}

resource "aws_ecs_service" "app" {
  name                    = "todo-api-service"
  cluster                 = aws_ecs_cluster.main.id
  task_definition         = "${aws_ecs_task_definition.app.family}:${aws_ecs_task_definition.app.revision}"
  desired_count           = 0
  enable_ecs_managed_tags = true
  wait_for_steady_state   = false

  capacity_provider_strategy {
    capacity_provider = "FARGATE"
    weight            = 1
    base              = 0
  }

  network_configuration {
    subnets          = [aws_subnet.public_1a.id]
    security_groups  = [aws_security_group.app.id]
    assign_public_ip = true
  }

  deployment_maximum_percent         = 200
  deployment_minimum_healthy_percent = 100

  deployment_circuit_breaker {
    enable   = true
    rollback = true
  }
}

resource "aws_db_subnet_group" "main" {
  name        = "todo-api-db-subnet-group"
  description = "todo-api RDS subnet group"
  subnet_ids  = [aws_subnet.private_1a.id, aws_subnet.private_1c.id]
}

resource "aws_db_instance" "main" {
  identifier     = "todo-api-db"
  engine         = "postgres"
  engine_version = "18.3"
  instance_class = "db.t4g.micro"

  allocated_storage     = 20
  max_allocated_storage = 1000
  storage_type          = "gp2"
  storage_encrypted     = true

  db_name  = "tododb"
  username = "postgres"

  manage_master_user_password = true

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.db.id]
  availability_zone      = "ap-northeast-1a"
  multi_az               = false
  publicly_accessible    = false
  network_type           = "IPV4"

  parameter_group_name = "default.postgres18"

  backup_retention_period = 1
  backup_window           = "19:25-19:55"
  maintenance_window      = "thu:13:32-thu:14:02"

  auto_minor_version_upgrade = true
  deletion_protection        = false
  copy_tags_to_snapshot      = true

  performance_insights_enabled          = true
  performance_insights_retention_period = 7

  skip_final_snapshot = true
}
