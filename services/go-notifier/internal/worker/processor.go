package worker

import (
	"context"
	"notifier/internal/notifier"
	"notifier/internal/reminder"
	"notifier/internal/repository"
	"time"

	"go.uber.org/zap"
)

type Processor struct {
	repo     repository.TaskRepository
	notifier notifier.NotificationSender
	log      *zap.Logger
}

func NewProcessor(repo repository.TaskRepository, notifier notifier.NotificationSender, log *zap.Logger) *Processor {
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
		for _, setting := range tw.Settings {
			remindAt := reminder.CalculateRemindAt(tw.DeadlineAt, setting.RemindBeforeValue, setting.RemindBeforeUnit)
			if !reminder.IsDue(now, remindAt) {
				continue
			}

			if err := p.notifier.SendForSetting(ctx, tw, setting, remindAt); err != nil {
				p.log.Error("failed to send notification",
					zap.Error(err),
					zap.String("task", tw.Title),
					zap.String("channel", setting.Channel),
				)
			}
		}
	}
	return nil
}
