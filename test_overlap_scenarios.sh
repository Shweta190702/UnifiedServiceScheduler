#!/bin/bash
# test_overlap_scenarios.sh
# Tests all 3 mathematical interval overlap cases:
# 1) Head Collision (09:00-10:00 vs 09:30-10:30)
# 2) Tail Collision (09:30-10:30 vs 09:00-10:00)
# 3) Enclosing Window (09:00-11:00 vs 09:30-10:00)

DEALERSHIP_ID="d1111111-1111-1111-1111-111111111111"
CUSTOMER_ID="c2222222-2222-2222-2222-222222222222"
VEHICLE_ID="a3333333-3333-3333-3333-333333333333"
BRAKE_SERVICE_ID="b4444444-4444-4444-4444-444444444444"
EV_SERVICE_ID="e5555555-5555-5555-5555-555555555555"

URL="http://localhost:5050/api/v1/dealerships/$DEALERSHIP_ID/appointments"

echo "=========================================================="
echo "TESTING ALL 3 MATHEMATICAL INTERVAL OVERLAP CASES"
echo "=========================================================="

# Case 1: Head Collision
# Existing appointment: 09:30 - 10:30. New requested: 09:00 - 10:00 (Shared: 09:30 - 10:00)
echo ""
echo "[Test 1/3] Head Collision (Existing: 09:30-10:30 | Request: 09:00-10:00)..."
STATUS1=$(curl -s -o res1.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$BRAKE_SERVICE_ID"'",
    "scheduledStartTime": "2026-11-11T09:00:00Z",
    "notes": "Testing Head Collision Overlap"
  }')
echo "  -> HTTP Status: $STATUS1"
cat res1.json | grep -o '"detail":"[^"]*' || cat res1.json
echo ""

# Case 2: Tail Collision
# Existing appointment: 09:00 - 10:00. New requested: 09:30 - 10:30 (Shared: 09:30 - 10:00)
echo ""
echo "[Test 2/3] Tail Collision (Existing: 09:00-10:00 | Request: 09:30-10:30)..."
STATUS2=$(curl -s -o res2.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$BRAKE_SERVICE_ID"'",
    "scheduledStartTime": "2026-11-12T09:30:00Z",
    "notes": "Testing Tail Collision Overlap"
  }')
echo "  -> HTTP Status: $STATUS2"
cat res2.json | grep -o '"detail":"[^"]*' || cat res2.json
echo ""

# Case 3: Enclosing / Nested Window
# Existing appointment: 09:30 - 10:00 (30 min). New requested: 09:00 - 11:00 (2 hrs)
echo ""
echo "[Test 3/3] Enclosing / Nested Window (Existing: 09:30-10:00 | Request: 09:00-11:00)..."
STATUS3=$(curl -s -o res3.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$BRAKE_SERVICE_ID"'",
    "scheduledStartTime": "2026-11-13T09:00:00Z",
    "notes": "Testing Enclosing Window Overlap"
  }')
echo "  -> HTTP Status: $STATUS3"
cat res3.json | grep -o '"detail":"[^"]*' || cat res3.json
echo ""

echo "=========================================================="
echo "Summary Verification:"
if [ "$STATUS1" = "409" ] && [ "$STATUS2" = "409" ] && [ "$STATUS3" = "409" ]; then
  echo "✅ ALL 3 TEMPORAL OVERLAP CASES REJECTED WITH 409 CONFLICT!"
  echo "   1) Head Collision:      409 Conflict (Overlap on 09:30 - 10:00)"
  echo "   2) Tail Collision:      409 Conflict (Overlap on 09:30 - 10:00)"
  echo "   3) Enclosing Window:    409 Conflict (2hr request swallows 30min appointment)"
else
  echo "⚠️ Review non-409 statuses above."
fi
echo "=========================================================="
rm -f res1.json res2.json res3.json
