package worker

import (
	"context"
	"time"

	"go.uber.org/zap"
)

type Scheduler struct {
	interval  time.Duration
	processor *Processor
	log       *zap.Logger
}

func NewScheduler(intervalSeconds int, processor *Processor, log *zap.Logger) *Scheduler {
	return &Scheduler{
		interval:  time.Duration(intervalSeconds) * time.Second,
		processor: processor,
		log:       log,
	}
}

func (s *Scheduler) Run(ctx context.Context) {
	ticker := time.NewTicker(s.interval)
	defer ticker.Stop()

	s.log.Info("scheduler started", zap.Duration("interval", s.interval))

	for {
		select {
		case <-ticker.C:
			if err := s.processor.Process(ctx); err != nil {
				s.log.Error("process iteration failed", zap.Error(err))
			}
		case <-ctx.Done():
			s.log.Info("scheduler stopped")
			return
		}
	}
}
