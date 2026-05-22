package email

import (
	"context"

	"go.uber.org/zap"
)

type MockEmailSender struct {
	log *zap.Logger
}

func NewMockEmailSender(log *zap.Logger) *MockEmailSender {
	return &MockEmailSender{log: log}
}

func (m *MockEmailSender) Send(ctx context.Context, to, subject, body string) error {
	if err := ctx.Err(); err != nil {
		return err
	}
	m.log.Info("MOCK EMAIL SENT",
		zap.String("to", to),
		zap.String("subject", subject),
		zap.String("body", body),
	)
	return nil
}
