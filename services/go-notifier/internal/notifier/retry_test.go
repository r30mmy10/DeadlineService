package notifier

import (
	"context"
	"errors"
	"testing"
	"time"
)

func TestSendWithRetry_SuccessFirstAttempt(t *testing.T) {
	attempts := 0
	err := sendWithRetry(
		context.Background(),
		func(ctx context.Context) error {
			attempts++
			return nil
		},
		nil,
		retryConfig{maxAttempts: 3, intervals: nil, sleep: func(time.Duration) {}},
	)
	if err != nil || attempts != 1 {
		t.Fatalf("err=%v attempts=%d", err, attempts)
	}
}

func TestSendWithRetry_RetriesThenSucceeds(t *testing.T) {
	attempts := 0
	logged := 0
	err := sendWithRetry(
		context.Background(),
		func(ctx context.Context) error {
			attempts++
			if attempts < 3 {
				return errors.New("temporary")
			}
			return nil
		},
		func(attempt int, status string) {
			logged++
			if status != "failed" && attempt < 3 {
				// last success
			}
		},
		retryConfig{
			maxAttempts: 3,
			intervals:   []time.Duration{0, 0},
			sleep:       func(time.Duration) {},
		},
	)
	if err != nil || attempts != 3 || logged != 3 {
		t.Fatalf("err=%v attempts=%d logged=%d", err, attempts, logged)
	}
}

func TestSendWithRetry_AllFail(t *testing.T) {
	err := sendWithRetry(
		context.Background(),
		func(ctx context.Context) error { return errors.New("smtp down") },
		nil,
		retryConfig{maxAttempts: 2, intervals: []time.Duration{0}, sleep: func(time.Duration) {}},
	)
	if err == nil {
		t.Fatal("expected error")
	}
}
