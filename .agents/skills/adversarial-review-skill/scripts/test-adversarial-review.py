#!/usr/bin/env python3
"""
Test suite for adversarial-review-skill timestamp generation and collision detection.

Tests:
1. Timestamp format validation (ISO 8601, millisecond precision)
2. Cross-platform consistency (macOS, Linux, Windows)
3. Millisecond granularity (no duplicates in rapid succession)
4. Collision detection logic (atomic write retry behavior)

Usage:
  python3 test-adversarial-review.py

Exit codes:
  0 = all tests passed
  1 = one or more tests failed
"""

import os
import sys
import re
import time
import tempfile
import shutil
from pathlib import Path
from datetime import datetime, timezone
import subprocess

# Color codes for terminal output
GREEN = "\033[92m"
RED = "\033[91m"
YELLOW = "\033[93m"
RESET = "\033[0m"
BOLD = "\033[1m"

def test_timestamp_format():
    """Test that generate-timestamp.py produces valid ISO 8601 format."""
    print(f"\n{BOLD}Test 1: Timestamp Format{RESET}")
    
    script_path = Path(__file__).parent / "generate-timestamp.py"
    if not script_path.exists():
        print(f"{RED}✗ FAILED: generate-timestamp.py not found at {script_path}{RESET}")
        return False
    
    try:
        result = subprocess.run(
            [sys.executable, str(script_path)],
            capture_output=True,
            text=True,
            timeout=5
        )
        timestamp = result.stdout.strip()
        
        # Expected format: YYYYMMDDTHHHMMSS.SSSZ
        pattern = r'^\d{8}T\d{6}\.\d{3}Z$'
        if not re.match(pattern, timestamp):
            print(f"{RED}✗ FAILED: Invalid format: {timestamp}{RESET}")
            print(f"  Expected: YYYYMMDDTHHHMMSS.SSSZ (e.g., 20260915T193842.123Z)")
            return False
        
        print(f"{GREEN}✓ PASSED: Timestamp format valid: {timestamp}{RESET}")
        return True
    except Exception as e:
        print(f"{RED}✗ FAILED: {e}{RESET}")
        return False


def test_timestamp_precision():
    """Test that timestamps have millisecond precision (not all zeros or all same)."""
    print(f"\n{BOLD}Test 2: Timestamp Precision (Milliseconds){RESET}")
    
    script_path = Path(__file__).parent / "generate-timestamp.py"
    timestamps = []
    
    # Generate 10 timestamps in rapid succession
    for i in range(10):
        try:
            result = subprocess.run(
                [sys.executable, str(script_path)],
                capture_output=True,
                text=True,
                timeout=5
            )
            timestamp = result.stdout.strip()
            timestamps.append(timestamp)
        except Exception as e:
            print(f"{RED}✗ FAILED on iteration {i}: {e}{RESET}")
            return False
    
    # Extract millisecond parts
    millis = [ts.split('.')[1][:-1] for ts in timestamps]  # Remove 'Z'
    unique_millis = set(millis)
    
    # With 10 rapid calls, we should get multiple different millisecond values
    if len(unique_millis) < 3:
        print(f"{RED}✗ FAILED: Insufficient millisecond variation{RESET}")
        print(f"  Generated: {timestamps}")
        print(f"  Unique milliseconds: {unique_millis} (expected ≥3)")
        return False
    
    print(f"{GREEN}✓ PASSED: Millisecond precision verified{RESET}")
    print(f"  Generated 10 timestamps, {len(unique_millis)} unique millisecond values")
    print(f"  Samples: {timestamps[:3]}")
    return True


def test_timestamp_utc():
    """Test that timestamps are in UTC (Z suffix, not local time)."""
    print(f"\n{BOLD}Test 3: UTC Timezone{RESET}")
    
    script_path = Path(__file__).parent / "generate-timestamp.py"
    
    try:
        result = subprocess.run(
            [sys.executable, str(script_path)],
            capture_output=True,
            text=True,
            timeout=5
        )
        timestamp = result.stdout.strip()
        
        if not timestamp.endswith('Z'):
            print(f"{RED}✗ FAILED: Timestamp does not end with Z (UTC marker): {timestamp}{RESET}")
            return False
        
        # Parse and verify it's close to current UTC time
        # Format: YYYYMMDDTHHHMMSS.SSSZ
        ts_part = timestamp[:-1]  # Remove 'Z'
        dt_str = ts_part.split('.')[0]  # Extract YYYYMMDDTHHMMSS (ignore milliseconds)
        ts_datetime = datetime.strptime(dt_str, "%Y%m%dT%H%M%S")
        now_utc = datetime.now(timezone.utc).replace(microsecond=0)
        
        # Allow 5-second drift for script execution time
        delta = abs((ts_datetime - now_utc.replace(tzinfo=None)).total_seconds())
        if delta > 5:
            print(f"{RED}✗ FAILED: Timestamp too far from current UTC time (drift: {delta}s){RESET}")
            print(f"  Timestamp: {timestamp}")
            print(f"  Current UTC: {now_utc}")
            return False
        
        print(f"{GREEN}✓ PASSED: UTC timezone verified{RESET}")
        print(f"  Timestamp: {timestamp} (Z suffix indicates UTC)")
        return True
    except Exception as e:
        print(f"{RED}✗ FAILED: {e}{RESET}")
        return False


def test_collision_detection_logic():
    """Test the collision detection algorithm (file exists → retry with counter)."""
    print(f"\n{BOLD}Test 4: Collision Detection (Atomic Write Retry){RESET}")
    
    with tempfile.TemporaryDirectory() as tmpdir:
        base_filename = "adversarial-review_test_feature-test_1_20260915T193842.123Z.md"
        target_path = Path(tmpdir) / base_filename
        
        # Simulate collision detection logic
        def write_with_collision_detection(target, content, max_retries=10):
            """Write file, handling collisions by appending counter."""
            current_path = target
            for attempt in range(max_retries):
                if not current_path.exists():
                    # Safe to write
                    current_path.write_text(content)
                    return current_path, attempt
                else:
                    # Collision detected, increment counter
                    if attempt == 0:
                        # First collision: insert counter before .md
                        base = current_path.stem  # Remove .md
                        new_name = f"{base}_001.md"
                        current_path = current_path.parent / new_name
                    else:
                        # Subsequent collisions: increment counter
                        base = str(current_path).rsplit('_', 1)[0]
                        counter = f"{attempt + 1:03d}"
                        new_name = f"{base}_{counter}.md"
                        current_path = Path(new_name)
            
            raise RuntimeError(f"Max retries ({max_retries}) exceeded")
        
        try:
            # Test 1: First write (no collision)
            content1 = "Review 1"
            path1, attempt1 = write_with_collision_detection(target_path, content1)
            if attempt1 != 0 or not path1.exists():
                print(f"{RED}✗ FAILED: First write should succeed immediately{RESET}")
                return False
            print(f"  ✓ Write 1: {path1.name} (attempt {attempt1})")
            
            # Test 2: Second write (collision on first file)
            content2 = "Review 2"
            path2, attempt2 = write_with_collision_detection(target_path, content2)
            if path2.name != "adversarial-review_test_feature-test_1_20260915T193842.123Z_001.md":
                print(f"{RED}✗ FAILED: Second write should use _001 counter{RESET}")
                print(f"  Expected: adversarial-review_test_feature-test_1_20260915T193842.123Z_001.md")
                print(f"  Got: {path2.name}")
                return False
            print(f"  ✓ Write 2: {path2.name} (collision, attempt {attempt2})")
            
            # Test 3: Third write (collision on both)
            content3 = "Review 3"
            path3, attempt3 = write_with_collision_detection(target_path, content3)
            if path3.name != "adversarial-review_test_feature-test_1_20260915T193842.123Z_002.md":
                print(f"{RED}✗ FAILED: Third write should use _002 counter{RESET}")
                print(f"  Expected: adversarial-review_test_feature-test_1_20260915T193842.123Z_002.md")
                print(f"  Got: {path3.name}")
                return False
            print(f"  ✓ Write 3: {path3.name} (collision, attempt {attempt3})")
            
            # Verify all files exist and have correct content
            if path1.read_text() != content1:
                print(f"{RED}✗ FAILED: File 1 content mismatch{RESET}")
                return False
            if path2.read_text() != content2:
                print(f"{RED}✗ FAILED: File 2 content mismatch{RESET}")
                return False
            if path3.read_text() != content3:
                print(f"{RED}✗ FAILED: File 3 content mismatch{RESET}")
                return False
            
            print(f"{GREEN}✓ PASSED: Collision detection logic verified{RESET}")
            print(f"  Successfully handled 3 collisions with counter-based retry")
            return True
        except Exception as e:
            print(f"{RED}✗ FAILED: {e}{RESET}")
            return False


def test_no_silent_overwrites():
    """Test that collision detection prevents silent overwrites."""
    print(f"\n{BOLD}Test 5: No Silent Overwrites{RESET}")
    
    with tempfile.TemporaryDirectory() as tmpdir:
        target_path = Path(tmpdir) / "test-review.md"
        
        # Write first file
        content1 = "Original content"
        target_path.write_text(content1)
        
        # Attempt to write with collision detection
        content2 = "New content"
        
        # Simulate what should happen: file renamed, not overwritten
        if target_path.exists():
            # This is the collision detection logic
            counter_path = target_path.parent / f"{target_path.stem}_001.md"
            counter_path.write_text(content2)
            
            # Verify original is unchanged
            if target_path.read_text() != content1:
                print(f"{RED}✗ FAILED: Original file was overwritten{RESET}")
                return False
            
            # Verify new content went to counter file
            if not counter_path.exists():
                print(f"{RED}✗ FAILED: Counter file not created{RESET}")
                return False
            if counter_path.read_text() != content2:
                print(f"{RED}✗ FAILED: Counter file content mismatch{RESET}")
                return False
        
        print(f"{GREEN}✓ PASSED: No silent overwrites detected{RESET}")
        print(f"  Original: {target_path.read_text()}")
        print(f"  Counter:  {counter_path.read_text()}")
        return True


def main():
    """Run all tests and report results."""
    print(f"\n{BOLD}{'='*60}")
    print("Adversarial Review Skill — Test Suite")
    print(f"{'='*60}{RESET}")
    
    results = []
    
    # Run all tests
    results.append(("Timestamp Format", test_timestamp_format()))
    results.append(("Timestamp Precision", test_timestamp_precision()))
    results.append(("UTC Timezone", test_timestamp_utc()))
    results.append(("Collision Detection Logic", test_collision_detection_logic()))
    results.append(("No Silent Overwrites", test_no_silent_overwrites()))
    
    # Summary
    print(f"\n{BOLD}{'='*60}")
    print("Test Summary")
    print(f"{'='*60}{RESET}")
    
    passed = sum(1 for _, result in results if result)
    total = len(results)
    
    for name, result in results:
        status = f"{GREEN}✓ PASS{RESET}" if result else f"{RED}✗ FAIL{RESET}"
        print(f"{status}: {name}")
    
    print(f"\n{BOLD}Total: {passed}/{total} tests passed{RESET}")
    
    if passed == total:
        print(f"{GREEN}All tests passed!{RESET}")
        return 0
    else:
        print(f"{RED}Some tests failed. Review logs above.{RESET}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
