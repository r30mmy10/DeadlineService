package notifier

import (
	"strings"
	"testing"
	"time"

	"notifier/internal/models"

	"github.com/google/uuid"
)

func TestGenerateMessage(t *testing.T) {
	task := models.Task{
		Id:         uuid.New(),
		Title:      "Отчёт",
		DeadlineAt: time.Date(2026, 12, 31, 15, 30, 0, 0, time.UTC),
	}

	msg := GenerateMessage(task)
	if !strings.Contains(msg, "Отчёт") {
		t.Fatalf("message should contain task title: %q", msg)
	}
	if !strings.Contains(msg, "31.12.2026") {
		t.Fatalf("message should contain formatted deadline: %q", msg)
	}
}
