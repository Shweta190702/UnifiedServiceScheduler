#!/bin/bash
# test_invariants_and_validation.sh
# Tests:
# 1) Zero or Negative Duration (TimeSlot invariant exception End <= Start)
# 2) Empty / Nil GUIDs (FluentValidation blocks before hitting domain handler)
# 3) Non-Existent Foreign Entity (Repository lookup returns null -> 404 Not Found)

DEALERSHIP_ID="d1111111-1111-1111-1111-111111111111"
CUSTOMER_ID="c2222222-2222-2222-2222-222222222222"
VEHICLE_ID="a3333333-3333-3333-3333-333333333333"
BRAKE_SERVICE_ID="b4444444-4444-4444-4444-444444444444"
TARGET_TIME="2026-11-17T10:00:00Z"

URL="http://localhost:5050/api/v1/dealerships/$DEALERSHIP_ID/appointments"

echo "=========================================================="
echo "TESTING VALIDATION & DOMAIN INVARIANT FAILURE CASES"
echo "=========================================================="

# Test 1: Zero or Negative Duration
# Seeds corrupted service type (00:00:00 duration) then requests booking
echo ""
echo "[Test 1/3] Zero / Negative Duration Invariant (TimeSlot End <= Start)..."
curl -s -X POST http://localhost:5050/api/v1/dev/seed-corrupted-zero-duration-service > /dev/null

STATUS1=$(curl -s -o v1.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "00000000-0000-0000-0000-000000000001",
    "scheduledStartTime": "'"$TARGET_TIME"'",
    "notes": "Testing Zero Duration Invariant"
  }')
echo "  -> HTTP Status: $STATUS1"
echo "  -> Detail: $(grep -o '"detail":"[^"]*' v1.json | cut -d'"' -f4)"

# Test 2: Empty / Nil GUIDs
# CustomerId is Guid.Empty (00000000-0000-0000-0000-000000000000)
echo ""
echo "[Test 2/3] Empty / Nil GUID (00000000-0000-0000-0000-000000000000)..."
STATUS2=$(curl -s -o v2.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "00000000-0000-0000-0000-000000000000",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$BRAKE_SERVICE_ID"'",
    "scheduledStartTime": "'"$TARGET_TIME"'",
    "notes": "Testing Empty GUID"
  }')
echo "  -> HTTP Status: $STATUS2"
echo "  -> Detail: $(grep -o '"detail":"[^"]*' v2.json | cut -d'"' -f4)"

# Test 3: Non-Existent Foreign Entity
# VehicleId is valid GUID format, but does not exist in DB
echo ""
echo "[Test 3/3] Non-Existent Foreign Entity (Vehicle ffffffff-ffff-ffff-ffff-ffffffffffff)..."
STATUS3=$(curl -s -o v3.json -w "%{http_code}" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "ffffffff-ffff-ffff-ffff-ffffffffffff",
    "serviceTypeId": "'"$BRAKE_SERVICE_ID"'",
    "scheduledStartTime": "'"$TARGET_TIME"'",
    "notes": "Testing Non-Existent Vehicle"
  }')
echo "  -> HTTP Status: $STATUS3"
echo "  -> Detail: $(grep -o '"detail":"[^"]*' v3.json | cut -d'"' -f4)"
echo "  -> ErrorCode: $(grep -o '"errorCode":"[^"]*' v3.json | cut -d'"' -f4)"

echo ""
echo "=========================================================="
echo "Summary Verification:"
if [ "$STATUS1" = "400" ] && [ "$STATUS2" = "400" ] && [ "$STATUS3" = "404" ]; then
  echo "✅ ALL 3 VALIDATION & INVARIANT CASES PASSED!"
  echo "   1) Zero Duration:       400 Bad Request (Domain Invariant: duration must be positive)"
  echo "   2) Empty / Nil GUID:    400 Bad Request (FluentValidation blocked payload at API gate)"
  echo "   3) Non-Existent Entity: 404 Not Found   (Code: VEHICLE_NOT_FOUND)"
else
  echo "⚠️ Review status codes above."
fi
echo "=========================================================="
rm -f v1.json v2.json v3.json
