package worker

import (
	"context"
	"testing"
	"time"

	"notifier/internal/models"

	"github.com/google/uuid"
	"go.uber.org/zap"
)

type stubRepo struct {
	tasks []models.TaskWithSettings
}

func (s *stubRepo) GetTasksNeedingNotification(ctx context.Context) ([]models.TaskWithSettings, error) {
	return s.tasks, nil
}

func (s *stubRepo) CreateNotification(ctx context.Context, notif models.Notification) (uuid.UUID, error) {
	return uuid.New(), nil
}

func (s *stubRepo) UpdateNotificationStatus(ctx context.Context, id uuid.UUID, status string, sentAt *time.Time) error {
	return nil
}

func (s *stubRepo) LogDeliveryAttempt(ctx context.Context, notifId uuid.UUID, attempt int, status string) error {
	return nil
}

func (s *stubRepo) IsAlreadyNotified(ctx context.Context, taskId uuid.UUID, channel string, since time.Time) (bool, error) {
	return false, nil
}

type recordingNotifier struct {
	calls int
}

func (r *recordingNotifier) SendForSetting(ctx context.Context, tw models.TaskWithSettings, setting models.NotificationSetting, remindAt time.Time) error {
	r.calls++
	return nil
}

func TestProcessor_SendsWhenReminderDue(t *testing.T) {
	deadline := time.Now().Add(2 * time.Hour)
	tasks := []models.TaskWithSettings{{
		Task: models.Task{
			Id:         uuid.New(),
			Title:      "Due soon",
			DeadlineAt: deadline,
		},
		Settings: []models.NotificationSetting{{
			Channel:           "email",
			RemindBeforeValue: 3,
			RemindBeforeUnit:  "Hours",
		}},
	}}

	notif := &recordingNotifier{}
	p := NewProcessor(&stubRepo{tasks: tasks}, notif, zap.NewNop())
	if err := p.Process(context.Background()); err != nil {
		t.Fatal(err)
	}
	if notif.calls != 1 {
		t.Fatalf("expected 1 send call, got %d", notif.calls)
	}
}

func TestProcessor_SkipsWhenReminderNotDue(t *testing.T) {
	deadline := time.Now().Add(48 * time.Hour)
	tasks := []models.TaskWithSettings{{
		Task: models.Task{
			Id:         uuid.New(),
			DeadlineAt: deadline,
		},
		Settings: []models.NotificationSetting{{
			Channel:           "email",
			RemindBeforeValue: 1,
			RemindBeforeUnit:  "Hours",
		}},
	}}

	notif := &recordingNotifier{}
	p := NewProcessor(&stubRepo{tasks: tasks}, notif, zap.NewNop())
	if err := p.Process(context.Background()); err != nil {
		t.Fatal(err)
	}
	if notif.calls != 0 {
		t.Fatalf("expected 0 send calls, got %d", notif.calls)
	}
}
