#!/usr/bin/env python3
"""
Generate a collision-safe timestamp in ISO 8601 format with millisecond precision.
Works on macOS, Linux, and Windows.

Usage:
  python3 generate-timestamp.sh

Output:
  20260915T193842.123Z (UTC, ISO 8601 with milliseconds)
"""

from datetime import datetime, timezone

# Get current UTC time with microsecond precision
now = datetime.now(timezone.utc)

# Format as ISO 8601 with millisecond precision (3 digits)
# Example: 2026-09-15T19:38:42.123Z
timestamp = now.strftime("%Y%m%dT%H%M%S") + f".{now.microsecond // 1000:03d}Z"

print(timestamp)
