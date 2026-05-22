package email

import (
	"context"
	"crypto/tls"
	"fmt"
	"net"
	"net/smtp"
	"strings"

	"go.uber.org/zap"
)

// TLSMode — как подключаться к SMTP (Gmail/Yandex: starttls на 587, ssl на 465).
type TLSMode string

const (
	TLSNone      TLSMode = "none"
	TLSStartTLS  TLSMode = "starttls"
	TLSSSL       TLSMode = "ssl"
)

// SMTPSender отправляет письма на реальный адрес получателя (Users.Email) через SMTP relay.
type SMTPSender struct {
	host     string
	port     int
	username string
	password string
	from     string
	tls      TLSMode
	log      *zap.Logger
}

func NewSMTPSender(host string, port int, username, password, from string, tls TLSMode, log *zap.Logger) *SMTPSender {
	if tls == "" {
		tls = defaultTLSForPort(port)
	}
	return &SMTPSender{
		host:     host,
		port:     port,
		username: username,
		password: password,
		from:     from,
		tls:      tls,
		log:      log,
	}
}

func defaultTLSForPort(port int) TLSMode {
	switch port {
	case 587:
		return TLSStartTLS
	case 465:
		return TLSSSL
	default:
		return TLSNone
	}
}

func (s *SMTPSender) Send(ctx context.Context, to, subject, body string) error {
	if err := ctx.Err(); err != nil {
		return err
	}

	to = strings.TrimSpace(to)
	if to == "" {
		return fmt.Errorf("recipient email is empty")
	}

	headers := []string{
		fmt.Sprintf("From: %s", s.from),
		fmt.Sprintf("To: %s", to),
		fmt.Sprintf("Subject: %s", subject),
		"MIME-Version: 1.0",
		"Content-Type: text/plain; charset=UTF-8",
		"",
		body,
	}
	msg := []byte(strings.Join(headers, "\r\n"))

	var auth smtp.Auth
	if s.username != "" {
		auth = smtp.PlainAuth("", s.username, s.password, s.host)
	}

	addr := fmt.Sprintf("%s:%d", s.host, s.port)
	if err := s.send(addr, auth, s.from, []string{to}, msg); err != nil {
		return fmt.Errorf("smtp send to %s: %w", to, err)
	}

	s.log.Info("email delivered via SMTP",
		zap.String("to", to),
		zap.String("subject", subject),
		zap.String("host", s.host),
	)
	return nil
}

func (s *SMTPSender) send(addr string, auth smtp.Auth, from string, to []string, msg []byte) error {
	switch s.tls {
	case TLSSSL:
		return s.sendSSL(addr, auth, from, to, msg)
	case TLSStartTLS:
		return s.sendStartTLS(addr, auth, from, to, msg)
	default:
		return smtp.SendMail(addr, auth, from, to, msg)
	}
}

func (s *SMTPSender) sendStartTLS(addr string, auth smtp.Auth, from string, to []string, msg []byte) error {
	host, _, err := net.SplitHostPort(addr)
	if err != nil {
		return err
	}

	conn, err := net.Dial("tcp", addr)
	if err != nil {
		return err
	}
	defer conn.Close()

	client, err := smtp.NewClient(conn, host)
	if err != nil {
		return err
	}
	defer client.Close()

	if ok, _ := client.Extension("STARTTLS"); ok {
		if err = client.StartTLS(&tls.Config{ServerName: host}); err != nil {
			return err
		}
	}

	return s.submit(client, auth, from, to, msg)
}

func (s *SMTPSender) sendSSL(addr string, auth smtp.Auth, from string, to []string, msg []byte) error {
	host, _, err := net.SplitHostPort(addr)
	if err != nil {
		return err
	}

	conn, err := tls.Dial("tcp", addr, &tls.Config{ServerName: host})
	if err != nil {
		return err
	}
	defer conn.Close()

	client, err := smtp.NewClient(conn, host)
	if err != nil {
		return err
	}
	defer client.Close()

	return s.submit(client, auth, from, to, msg)
}

func (s *SMTPSender) submit(client *smtp.Client, auth smtp.Auth, from string, to []string, msg []byte) error {
	if auth != nil {
		if ok, _ := client.Extension("AUTH"); ok {
			if err := client.Auth(auth); err != nil {
				return err
			}
		}
	}

	if err := client.Mail(from); err != nil {
		return err
	}
	for _, rcpt := range to {
		if err := client.Rcpt(rcpt); err != nil {
			return err
		}
	}

	w, err := client.Data()
	if err != nil {
		return err
	}
	if _, err = w.Write(msg); err != nil {
		return err
	}
	if err = w.Close(); err != nil {
		return err
	}
	return client.Quit()
}
