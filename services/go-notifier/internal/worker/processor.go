package worker

import (
	"context"
	"notifier/internal/notifier"
	"notifier/internal/repository"
	"time"

	"go.uber.org/zap"
)

type Processor struct {
	repo     *repository.TaskRepo
	notifier *notifier.Notifier
	log      *zap.Logger
}

func NewProcessor(repo *repository.TaskRepo, notifier *notifier.Notifier, log *zap.Logger) *Processor {
	return &Processor{
		repo:     repo,
		notifier: notifier,
		log:      log,
	}
}

// Process выполняет одну итерацию проверки дедлайнов.
func (p *Processor) Process(ctx context.Context) error {
	tasks, err := p.repo.GetTasksNeedingNotification(ctx)
	if err != nil {
		return err
	}

	now := time.Now()
	for _, tw := range tasks {
		// Проверяем каждую настройку напоминания
		for _, setting := range tw.Settings {
			remindAt := calculateRemindAt(tw.DeadlineAt, setting.RemindBeforeValue, setting.RemindBeforeUnit)
			if remindAt.IsZero() {
				continue
			}
			if now.After(remindAt) || now.Equal(remindAt) {
				// Отправляем уведомление
				if err := p.notifier.SendNotification(ctx, tw); err != nil {
					p.log.Error("failed to send notification", zap.Error(err), zap.String("task", tw.Title))
				}
				// После отправки можно break, чтобы не слать несколько уведомлений за раз по разным настройкам одной задачи
				break
			}
		}
	}
	return nil
}

func calculateRemindAt(deadline time.Time, value int, unit string) time.Time {
	switch unit {
	case "minutes":
		return deadline.Add(-time.Duration(value) * time.Minute)
	case "hours":
		return deadline.Add(-time.Duration(value) * time.Hour)
	case "days":
		return deadline.Add(-time.Duration(value) * 24 * time.Hour)
	default:
		return time.Time{}
	}
}
