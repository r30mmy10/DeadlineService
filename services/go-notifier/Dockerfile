
FROM golang:1.25.5-alpine AS builder

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .

# Сборка образа падает, если unit-тесты не проходят
RUN go test ./...

RUN CGO_ENABLED=0 go build -o notifier ./cmd/notifier

FROM alpine:latest

RUN apk --no-cache add ca-certificates

WORKDIR /root/

COPY --from=builder /app/notifier .

COPY config.yaml .

CMD ["./notifier"]