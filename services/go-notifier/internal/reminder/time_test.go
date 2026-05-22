package reminder

import (
	"testing"
	"time"
)

func TestCalculateRemindAt_HoursCaseInsensitive(t *testing.T) {
	deadline := time.Date(2026, 5, 22, 18, 0, 0, 0, time.UTC)
	got := CalculateRemindAt(deadline, 2, "Hours")
	want := deadline.Add(-2 * time.Hour)
	if !got.Equal(want) {
		t.Fatalf("got %v, want %v", got, want)
	}
}

func TestCalculateRemindAt_MinutesAndDays(t *testing.T) {
	deadline := time.Date(2026, 5, 22, 18, 0, 0, 0, time.UTC)

	minutes := CalculateRemindAt(deadline, 30, "minutes")
	if want := deadline.Add(-30 * time.Minute); !minutes.Equal(want) {
		t.Fatalf("minutes: got %v, want %v", minutes, want)
	}

	days := CalculateRemindAt(deadline, 1, "Days")
	if want := deadline.Add(-24 * time.Hour); !days.Equal(want) {
		t.Fatalf("days: got %v, want %v", days, want)
	}
}

func TestCalculateRemindAt_Invalid(t *testing.T) {
	deadline := time.Now()
	if !CalculateRemindAt(deadline, 1, "weeks").IsZero() {
		t.Fatal("expected zero time for unknown unit")
	}
	if !CalculateRemindAt(deadline, 0, "hours").IsZero() {
		t.Fatal("expected zero time for non-positive value")
	}
}

func TestIsDue(t *testing.T) {
	remindAt := time.Date(2026, 5, 22, 10, 0, 0, 0, time.UTC)

	if IsDue(remindAt.Add(-time.Minute), remindAt) {
		t.Fatal("should not be due before remindAt")
	}
	if !IsDue(remindAt, remindAt) {
		t.Fatal("should be due exactly at remindAt")
	}
	if !IsDue(remindAt.Add(time.Hour), remindAt) {
		t.Fatal("should be due after remindAt")
	}
}
