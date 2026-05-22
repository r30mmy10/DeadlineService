package notifier

import "strings"

func normalizeChannel(channel string) string {
	ch := strings.ToLower(strings.TrimSpace(channel))
	switch ch {
	case "in-app", "inapp", "system", "internal":
		return "in_app"
	default:
		return ch
	}
}

func isEmailChannel(channel string) bool {
	return normalizeChannel(channel) == "email"
}

func isInAppChannel(channel string) bool {
	return normalizeChannel(channel) == "in_app"
}
