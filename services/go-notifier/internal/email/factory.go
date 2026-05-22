package email

import (
	"fmt"
	"strings"

	"github.com/spf13/viper"
	"go.uber.org/zap"
)

// NewSenderFromConfig создаёт отправитель email по конфигу (mock или smtp).
func NewSenderFromConfig(v *viper.Viper, log *zap.Logger) (Sender, error) {
	mode := strings.ToLower(strings.TrimSpace(v.GetString("email.mode")))
	if mode == "" {
		mode = "mock"
	}

	switch mode {
	case "mock":
		log.Info("email sender: mock mode")
		return NewMockEmailSender(log), nil
	case "smtp":
		host := strings.TrimSpace(v.GetString("email.smtp.host"))
		port := v.GetInt("email.smtp.port")
		from := strings.TrimSpace(v.GetString("email.smtp.from"))
		if host == "" || port == 0 || from == "" {
			return nil, fmt.Errorf("smtp config incomplete: host, port and from are required")
		}

		tlsMode := TLSMode(strings.ToLower(strings.TrimSpace(v.GetString("email.smtp.tls"))))
		if tlsMode == "" {
			tlsMode = defaultTLSForPort(port)
		}

		if isLocalCaptureHost(host) {
			log.Info("email sender: Mailpit (учебный режим)", zap.String("inbox", "http://localhost:8025"))
		} else {
			log.Info("email sender: smtp", zap.String("host", host), zap.Int("port", port))
		}

		return NewSMTPSender(
			host,
			port,
			v.GetString("email.smtp.username"),
			v.GetString("email.smtp.password"),
			from,
			tlsMode,
			log,
		), nil
	default:
		return nil, fmt.Errorf("unknown email.mode: %s (use mock or smtp)", mode)
	}
}

func isLocalCaptureHost(host string) bool {
	h := strings.ToLower(host)
	return h == "mailpit" || h == "localhost" || h == "127.0.0.1"
}
