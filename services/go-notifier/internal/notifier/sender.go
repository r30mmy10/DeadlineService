package notifier

import (
	"context"
	"notifier/internal/email"
	"notifier/internal/models"
	"notifier/internal/repository"
	"time"

	"github.com/google/uuid"
	"go.uber.org/zap"
)

// NotificationSender — контракт для worker (мокается в тестах).
type NotificationSender interface {
	SendForSetting(ctx context.Context, taskWithSettings models.TaskWithSettings, setting models.NotificationSetting, remindAt time.Time) error
}

type Notifier struct {
	repo        repository.TaskRepository
	emailSender email.Sender
	log         *zap.Logger
	retry       retryConfig
}

func NewNotifier(repo repository.TaskRepository, emailSender email.Sender, log *zap.Logger) *Notifier {
	return &Notifier{
		repo:        repo,
		emailSender: emailSender,
		log:         log,
		retry:       defaultRetryConfig(),
	}
}

// SendForSetting создаёт уведомление для одной настройки и доставляет по каналу.
func (n *Notifier) SendForSetting(ctx context.Context, taskWithSettings models.TaskWithSettings, setting models.NotificationSetting, remindAt time.Time) error {
	channel := normalizeChannel(setting.Channel)

	already, err := n.repo.IsAlreadyNotified(ctx, taskWithSettings.Id, channel, remindAt)
	if err != nil {
		n.log.Warn("failed to check already notified", zap.Error(err))
	}
	if already {
		n.log.Debug("notification already sent for this reminder", zap.String("task_id", taskWithSettings.Id.String()), zap.String("channel", channel))
		return nil
	}

	message := GenerateMessage(taskWithSettings.Task)
	notif := models.Notification{
		TaskId:         taskWithSettings.Id,
		UserId:         taskWithSettings.UserId,
		Message:        message,
		Channel:        channel,
		DeliveryStatus: "pending",
	}

	notifId, err := n.repo.CreateNotification(ctx, notif)
	if err != nil {
		return err
	}

	sentAt := time.Now()

	switch {
	case isInAppChannel(setting.Channel):
		if err := n.repo.UpdateNotificationStatus(ctx, notifId, "sent", &sentAt); err != nil {
			return err
		}
		n.log.Info("in-app notification created", zap.String("notification_id", notifId.String()), zap.String("task_id", taskWithSettings.Id.String()))
		return nil

	case isEmailChannel(setting.Channel):
		err = n.sendEmailWithRetry(ctx, notifId, taskWithSettings.UserEmail, message)
		if err == nil {
			_ = n.repo.UpdateNotificationStatus(ctx, notifId, "sent", &sentAt)
			n.log.Info("email notification sent", zap.String("notification_id", notifId.String()), zap.String("to", taskWithSettings.UserEmail))
		} else {
			_ = n.repo.UpdateNotificationStatus(ctx, notifId, "failed", &sentAt)
			n.log.Error("email notification failed", zap.String("notification_id", notifId.String()), zap.Error(err))
		}
		return err

	default:
		_ = n.repo.UpdateNotificationStatus(ctx, notifId, "failed", &sentAt)
		n.log.Warn("unsupported notification channel", zap.String("channel", setting.Channel))
		return nil
	}
}

func (n *Notifier) sendEmailWithRetry(ctx context.Context, notifId uuid.UUID, toEmail, message string) error {
	subject := "Дедлайн приближается"
	return sendWithRetry(
		ctx,
		func(ctx context.Context) error {
			return n.emailSender.Send(ctx, toEmail, subject, message)
		},
		func(attempt int, status string) {
			if err := n.repo.LogDeliveryAttempt(ctx, notifId, attempt, status); err != nil {
				n.log.Warn("failed to log delivery attempt", zap.Error(err))
			}
		},
		n.retry,
	)
}
