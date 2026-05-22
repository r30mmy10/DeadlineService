package notifier

import (
	"context"
	"time"
)

type sleeper func(time.Duration)

// retryConfig настройки повторных попыток отправки email.
type retryConfig struct {
	maxAttempts int
	intervals   []time.Duration
	sleep       sleeper
}

func defaultRetryConfig() retryConfig {
	return retryConfig{
		maxAttempts: 3,
		intervals:   []time.Duration{1 * time.Minute, 3 * time.Minute, 5 * time.Minute},
		sleep:       time.Sleep,
	}
}

func sendWithRetry(
	ctx context.Context,
	send func(ctx context.Context) error,
	onAttempt func(attempt int, status string),
	cfg retryConfig,
) error {
	var lastErr error

	for attempt := 1; attempt <= cfg.maxAttempts; attempt++ {
		if err := ctx.Err(); err != nil {
			return err
		}

		err := send(ctx)
		status := "sent"
		if err != nil {
			status = "failed"
			lastErr = err
		} else {
			lastErr = nil
		}

		if onAttempt != nil {
			onAttempt(attempt, status)
		}

		if err == nil {
			return nil
		}
		if attempt < cfg.maxAttempts && attempt-1 < len(cfg.intervals) {
			cfg.sleep(cfg.intervals[attempt-1])
		}
	}
	return lastErr
}
