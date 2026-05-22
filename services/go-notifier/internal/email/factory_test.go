package email

import (
	"testing"

	"github.com/spf13/viper"
	"go.uber.org/zap"
)

func TestNewSenderFromConfig_Mock(t *testing.T) {
	v := viper.New()
	v.Set("email.mode", "mock")
	sender, err := NewSenderFromConfig(v, zap.NewNop())
	if err != nil {
		t.Fatal(err)
	}
	if _, ok := sender.(*MockEmailSender); !ok {
		t.Fatalf("expected MockEmailSender, got %T", sender)
	}
}

func TestNewSenderFromConfig_SMTPRequiresFields(t *testing.T) {
	v := viper.New()
	v.Set("email.mode", "smtp")
	_, err := NewSenderFromConfig(v, zap.NewNop())
	if err == nil {
		t.Fatal("expected error for incomplete smtp config")
	}
}

func TestNewSenderFromConfig_MailpitAllowsNoAuth(t *testing.T) {
	v := viper.New()
	v.Set("email.mode", "smtp")
	v.Set("email.smtp.host", "mailpit")
	v.Set("email.smtp.port", 1025)
	v.Set("email.smtp.from", "test@local")
	sender, err := NewSenderFromConfig(v, zap.NewNop())
	if err != nil {
		t.Fatal(err)
	}
	if _, ok := sender.(*SMTPSender); !ok {
		t.Fatalf("expected SMTPSender, got %T", sender)
	}
}

func TestNewSenderFromConfig_ExternalSMTPAllowed(t *testing.T) {
	v := viper.New()
	v.Set("email.mode", "smtp")
	v.Set("email.smtp.host", "smtp.gmail.com")
	v.Set("email.smtp.port", 587)
	v.Set("email.smtp.from", "a@gmail.com")
	sender, err := NewSenderFromConfig(v, zap.NewNop())
	if err != nil {
		t.Fatal(err)
	}
	if _, ok := sender.(*SMTPSender); !ok {
		t.Fatalf("expected SMTPSender, got %T", sender)
	}
}
