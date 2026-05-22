package notifier

import (
	"context"
	"errors"
	"testing"
	"time"

	"notifier/internal/email"
	"notifier/internal/models"

	"github.com/google/uuid"
	"go.uber.org/zap"
)

type mockRepo struct {
	created       []models.Notification
	already       bool
	deliveryLogs  []int
	statusUpdates []string
}

func (m *mockRepo) GetTasksNeedingNotification(ctx context.Context) ([]models.TaskWithSettings, error) {
	return nil, nil
}

func (m *mockRepo) CreateNotification(ctx context.Context, notif models.Notification) (uuid.UUID, error) {
	m.created = append(m.created, notif)
	return uuid.New(), nil
}

func (m *mockRepo) UpdateNotificationStatus(ctx context.Context, id uuid.UUID, status string, sentAt *time.Time) error {
	m.statusUpdates = append(m.statusUpdates, status)
	return nil
}

func (m *mockRepo) LogDeliveryAttempt(ctx context.Context, notifId uuid.UUID, attempt int, status string) error {
	m.deliveryLogs = append(m.deliveryLogs, attempt)
	return nil
}

func (m *mockRepo) IsAlreadyNotified(ctx context.Context, taskId uuid.UUID, channel string, since time.Time) (bool, error) {
	return m.already, nil
}

type failingSender struct{}

func (f failingSender) Send(ctx context.Context, to, subject, body string) error {
	return errors.New("send failed")
}

type successSender struct{}

func (s successSender) Send(ctx context.Context, to, subject, body string) error {
	return nil
}

func testTaskWithSettings() models.TaskWithSettings {
	return models.TaskWithSettings{
		Task: models.Task{
			Id:         uuid.New(),
			UserId:     uuid.New(),
			Title:      "Test",
			DeadlineAt: time.Now().Add(24 * time.Hour),
		},
		UserEmail: "user@example.com",
	}
}

func TestSendForSetting_EmailSuccess(t *testing.T) {
	repo := &mockRepo{}
	n := NewNotifier(repo, successSender{}, zap.NewNop())
	n.retry = retryConfig{maxAttempts: 1, sleep: func(time.Duration) {}}

	setting := models.NotificationSetting{Channel: "email", RemindBeforeValue: 1, RemindBeforeUnit: "Hours"}
	err := n.SendForSetting(context.Background(), testTaskWithSettings(), setting, time.Now())
	if err != nil {
		t.Fatal(err)
	}
	if len(repo.created) != 1 || repo.created[0].Channel != "email" {
		t.Fatalf("unexpected created: %+v", repo.created)
	}
	if len(repo.statusUpdates) != 1 || repo.statusUpdates[0] != "sent" {
		t.Fatalf("status updates: %v", repo.statusUpdates)
	}
}

func TestSendForSetting_InApp(t *testing.T) {
	repo := &mockRepo{}
	n := NewNotifier(repo, failingSender{}, zap.NewNop())

	setting := models.NotificationSetting{Channel: "in-app"}
	err := n.SendForSetting(context.Background(), testTaskWithSettings(), setting, time.Now())
	if err != nil {
		t.Fatal(err)
	}
	if repo.created[0].Channel != "in_app" {
		t.Fatalf("channel: %s", repo.created[0].Channel)
	}
	if repo.statusUpdates[0] != "sent" {
		t.Fatalf("in-app should be sent immediately, got %v", repo.statusUpdates)
	}
}

func TestSendForSetting_SkipIfAlreadyNotified(t *testing.T) {
	repo := &mockRepo{already: true}
	n := NewNotifier(repo, successSender{}, zap.NewNop())

	setting := models.NotificationSetting{Channel: "email"}
	if err := n.SendForSetting(context.Background(), testTaskWithSettings(), setting, time.Now()); err != nil {
		t.Fatal(err)
	}
	if len(repo.created) != 0 {
		t.Fatal("should not create notification")
	}
}

var _ email.Sender = successSender{}
