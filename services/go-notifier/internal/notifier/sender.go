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

type Notifier struct {
	repo        *repository.TaskRepo
	emailSender email.Sender
	log         *zap.Logger
}

func NewNotifier(repo *repository.TaskRepo, emailSender email.Sender, log *zap.Logger) *Notifier {
	return &Notifier{
		repo:        repo,
		emailSender: emailSender,
		log:         log,
	}
}

// SendNotification генерирует уведомление, сохраняет в БД, отправляет email с повторными попытками
func (n *Notifier) SendNotification(ctx context.Context, taskWithSettings models.TaskWithSettings) error {
	message := GenerateMessage(taskWithSettings.Task)

	for _, setting := range taskWithSettings.Settings {
		if setting.Channel != "email" {
			n.log.Debug("skipping non-email channel", zap.String("channel", setting.Channel))
			continue
		}

		// Проверяем, не было ли уже уведомления за последние 6 часов
		already, err := n.repo.IsAlreadyNotifiedForTaskSetting(ctx, taskWithSettings.Id, setting.Id)
		if err != nil {
			n.log.Warn("failed to check already notified", zap.Error(err))
		}
		if already {
			n.log.Debug("notification already sent recently", zap.String("task_id", taskWithSettings.Id.String()))
			continue
		}

		// Создаём запись в notifications со статусом pending
		notif := models.Notification{
			TaskId:         taskWithSettings.Id,
			UserId:         taskWithSettings.UserId,
			Message:        message,
			Channel:        "email",
			DeliveryStatus: "pending",
		}
		notifId, err := n.repo.CreateNotification(ctx, notif)
		if err != nil {
			n.log.Error("failed to create notification", zap.Error(err))
			continue
		}

		// Отправляем email с ретраями
		err = n.sendWithRetry(ctx, notifId, taskWithSettings.UserEmail, message)
		sentAt := time.Now()
		if err == nil {
			_ = n.repo.UpdateNotificationStatus(ctx, notifId, "sent", &sentAt)
			n.log.Info("notification sent successfully", zap.String("notification_id", notifId.String()), zap.String("to", taskWithSettings.UserEmail))
		} else {
			_ = n.repo.UpdateNotificationStatus(ctx, notifId, "failed", &sentAt)
			n.log.Error("notification failed after retries", zap.String("notification_id", notifId.String()), zap.Error(err))
		}
	}
	return nil
}

// sendWithRetry отправляет email с повторными попытками (3 раза, интервалы 1,3,5 минут)
func (n *Notifier) sendWithRetry(ctx context.Context, notifId uuid.UUID, toEmail, message string) error {
	maxAttempts := 3
	intervals := []time.Duration{1 * time.Minute, 3 * time.Minute, 5 * time.Minute}
	var lastErr error

	for attempt := 1; attempt <= maxAttempts; attempt++ {
		subject := "Дедлайн приближается"
		err := n.emailSender.Send(ctx, toEmail, subject, message)
		status := "sent"
		if err != nil {
			status = "failed"
			lastErr = err
			n.log.Warn("attempt failed", zap.Int("attempt", attempt), zap.Error(err))
		} else {
			lastErr = nil
		}

		// Логируем попытку в истории
		_ = n.repo.LogDeliveryAttempt(ctx, notifId, attempt, status)

		if err == nil {
			return nil
		}
		if attempt < maxAttempts {
			time.Sleep(intervals[attempt-1])
		}
	}
	return lastErr
}
