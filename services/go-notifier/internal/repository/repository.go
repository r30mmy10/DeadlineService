package repository

import (
	"context"
	"notifier/internal/models"
	"time"

	"github.com/google/uuid"
)

// TaskRepository — контракт для работы с задачами и уведомлениями (удобно для тестов).
type TaskRepository interface {
	GetTasksNeedingNotification(ctx context.Context) ([]models.TaskWithSettings, error)
	CreateNotification(ctx context.Context, notif models.Notification) (uuid.UUID, error)
	UpdateNotificationStatus(ctx context.Context, id uuid.UUID, status string, sentAt *time.Time) error
	LogDeliveryAttempt(ctx context.Context, notifId uuid.UUID, attempt int, status string) error
	IsAlreadyNotified(ctx context.Context, taskId uuid.UUID, channel string, since time.Time) (bool, error)
}
