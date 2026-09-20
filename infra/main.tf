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
  vpc_id = aws_vpc.main.id
  cidr_block = "10.0.11.0/24"
  availability_zone = "ap-northeast-1a"

  tags = {
    Name = "todo-api-private-1a"
  }
}

resource "aws_subnet" "private_1c" {
  vpc_id = aws_vpc.main.id
    cidr_block = "10.0.12.0/24"
  availability_zone = "ap-northeast-1c"

  tags = {
    Name = "todo-api-private-1c"
  }
}

resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id
}
