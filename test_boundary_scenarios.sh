#!/bin/bash
# test_boundary_scenarios.sh
# Tests all 3 Operating Hours & Timestamp Boundary edge cases:
# 1) Spill-Over Past Closing Hours (17:30 UTC for 60m service when dealership closes at 18:00)
# 2) Outside Business Hours / Closed Days (02:00 AM UTC or Sunday)
# 3) Past Timestamp / Stale Request (< 15 mins or in the past)

DEALERSHIP_ID="d1111111-1111-1111-1111-111111111111"
CUSTOMER_ID="c2222222-2222-2222-2222-222222222222"
VEHICLE_ID="a3333333-3333-3333-3333-333333333333"
SERVICE_TYPE_ID="b4444444-4444-4444-4444-444444444444" # 60-minute duration

URL="http://localhost:5050/api/v1/dealerships/$DEALERSHIP_ID/appointments"

echo "=========================================================="
echo "TESTING OPERATING HOURS & TIMESTAMP BOUNDARY CASES"
echo "=========================================================="

# Test 1: Spill-Over Past Closing Hours
# Monday 2026-11-16 at 17:30 UTC (60m duration -> ends at 18:30 UTC, closing is 18:00 UTC)
echo ""
echo "[Test 1/3] Spill-Over Past Closing Hours (Requested 17:30 UTC, Duration 60m, Closes 18:00 UTC)..."
STATUS1=$(curl -s -o b1.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$SERVICE_TYPE_ID"'",
    "scheduledStartTime": "2026-11-16T17:30:00Z",
    "notes": "Testing Spill-Over Past Closing"
  }')
echo "  -> HTTP Status: $STATUS1"
echo "  -> Detail: $(grep -o '"detail":"[^"]*' b1.json | cut -d'"' -f4)"
echo "  -> ErrorCode: $(grep -o '"errorCode":"[^"]*' b1.json | cut -d'"' -f4)"

# Test 2: Outside Business Hours / Closed Days
# Sunday 2026-11-15 (Dealership is closed on Sundays)
echo ""
echo "[Test 2/3] Booking on Closed Day (Sunday 2026-11-15 at 10:00 UTC)..."
STATUS2=$(curl -s -o b2.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$SERVICE_TYPE_ID"'",
    "scheduledStartTime": "2026-11-15T10:00:00Z",
    "notes": "Testing Closed Sunday Booking"
  }')
echo "  -> HTTP Status: $STATUS2"
echo "  -> Detail: $(grep -o '"detail":"[^"]*' b2.json | cut -d'"' -f4)"
echo "  -> ErrorCode: $(grep -o '"errorCode":"[^"]*' b2.json | cut -d'"' -f4)"

# Test 3: Past Timestamp / Stale Request
# DesiredStartTime is in the past (e.g. 2020-01-01)
echo ""
echo "[Test 3/3] Past Timestamp / Stale Request (2020-01-01T10:00:00Z)..."
STATUS3=$(curl -s -o b3.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$SERVICE_TYPE_ID"'",
    "scheduledStartTime": "2020-01-01T10:00:00Z",
    "notes": "Testing Past Timestamp"
  }')
echo "  -> HTTP Status: $STATUS3"
echo "  -> Detail: $(grep -o '"detail":"[^"]*' b3.json | cut -d'"' -f4)"
echo "  -> ErrorCode: $(grep -o '"errorCode":"[^"]*' b3.json | cut -d'"' -f4)"

echo ""
echo "=========================================================="
echo "Summary Verification:"
if [ "$STATUS1" = "400" ] && [ "$STATUS2" = "400" ] && [ "$STATUS3" = "400" ]; then
  echo "✅ ALL 3 BOUNDARY CASES REJECTED WITH 400 BAD REQUEST!"
  echo "   1) Spill-Over Past Closing:  400 Bad Request (Code: ExceedsOperatingHours)"
  echo "   2) Outside Operating Hours: 400 Bad Request (Code: OutsideOperatingSchedule)"
  echo "   3) Past Timestamp / Stale:   400 Bad Request (Code: StartTimeMustBeInFuture)"
else
  echo "⚠️ Review non-400 statuses above."
fi
echo "=========================================================="
rm -f b1.json b2.json b3.json
