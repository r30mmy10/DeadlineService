package notifier

import (
	"context"
	"notifier/internal/email"
	"notifier/internal/models"
	"notifier/internal/repository"
	"time"

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

// SendNotification генерирует уведомление и отправляет его (пока только логирует + мок-email).
// В будущем здесь будет создание записи в БД и ретраи.
func (n *Notifier) SendNotification(ctx context.Context, taskWithSettings models.TaskWithSettings) error {
	// Генерируем сообщение
	message := GenerateMessage(taskWithSettings.Task)

	// Для каждого канала уведомлений (пока поддерживаем только email)
	for _, setting := range taskWithSettings.Settings {
		if setting.Channel != "email" {
			n.log.Debug("skipping non-email channel", zap.String("channel", setting.Channel))
			continue
		}

		// TODO: Создать запись в таблице notifications (когда она появится)
		// notifID, err := n.repo.CreateNotification(ctx, ...)

		// Отправляем email
		subject := "Дедлайн приближается"
		if err := n.emailSender.Send(ctx, taskWithSettings.UserEmail, subject, message); err != nil {
			n.log.Error("failed to send email", zap.Error(err), zap.String("to", taskWithSettings.UserEmail))
			// TODO: записать ошибку в delivery_history, сделать retry
			return err
		}

		n.log.Info("notification sent successfully",
			zap.Int64("task_id", taskWithSettings.TaskID), // здесь TaskID – это uuid, но zap умеет string
			zap.String("task_title", taskWithSettings.Title),
			zap.String("to", taskWithSettings.UserEmail),
		)
		// TODO: обновить статус уведомления в БД
	}
	return nil
}
