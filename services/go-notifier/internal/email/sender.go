package email

import "context"

// Sender отправляет email-сообщения.
type Sender interface {
	Send(ctx context.Context, to, subject, body string) error
}
