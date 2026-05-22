package notifier

import "testing"

func TestNormalizeChannel(t *testing.T) {
	cases := map[string]string{
		"email":    "email",
		"EMAIL":    "email",
		"in-app":   "in_app",
		"system":   "in_app",
		"internal": "in_app",
	}

	for in, want := range cases {
		if got := normalizeChannel(in); got != want {
			t.Fatalf("%q => %q, want %q", in, got, want)
		}
	}
}
