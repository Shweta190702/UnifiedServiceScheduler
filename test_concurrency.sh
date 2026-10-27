#!/bin/bash
# test_concurrency.sh
# Tests race condition handling by firing two identical booking requests simultaneously for the same resource slot.

DEALERSHIP_ID="d1111111-1111-1111-1111-111111111111"
CUSTOMER_ID="c2222222-2222-2222-2222-222222222222"
VEHICLE_ID="a3333333-3333-3333-3333-333333333333"
SERVICE_TYPE_ID="b4444444-4444-4444-4444-444444444444"
TARGET_TIME="2026-11-05T14:00:00Z"

URL="http://localhost:5050/api/v1/dealerships/$DEALERSHIP_ID/appointments"

PAYLOAD_A='{
  "customerId": "'"$CUSTOMER_ID"'",
  "vehicleId": "'"$VEHICLE_ID"'",
  "serviceTypeId": "'"$SERVICE_TYPE_ID"'",
  "scheduledStartTime": "'"$TARGET_TIME"'",
  "notes": "Simultaneous Contention - Request A"
}'

PAYLOAD_B='{
  "customerId": "'"$CUSTOMER_ID"'",
  "vehicleId": "'"$VEHICLE_ID"'",
  "serviceTypeId": "'"$SERVICE_TYPE_ID"'",
  "scheduledStartTime": "'"$TARGET_TIME"'",
  "notes": "Simultaneous Contention - Request B"
}'

echo "=========================================================="
echo "Firing 2 SIMULTANEOUS requests competing for the LAST remaining bay:"
echo "Target Slot: $TARGET_TIME"
echo "=========================================================="

# Fire both requests concurrently in background subshells at the same millisecond
curl -s -w "\n[Request A HTTP Status]: %{http_code}\n" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD_A" > res1.txt &
PID1=$!

curl -s -w "\n[Request B HTTP Status]: %{http_code}\n" -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD_B" > res2.txt &
PID2=$!

# Wait for both background requests to finish
wait $PID1 $PID2

echo ""
echo "--- Results of Request 1 ---"
cat res1.txt

echo ""
echo "--- Results of Request 2 ---"
cat res2.txt

echo ""
echo "=========================================================="
echo "Checking results:"
if grep -q "201" res1.txt && grep -q "409\|422" res2.txt; then
  echo "✅ RACE CONDITION PREVENTED: Request 1 Won (201 Created), Request 2 was Safely Rejected (Conflict)."
elif grep -q "201" res2.txt && grep -q "409\|422" res1.txt; then
  echo "✅ RACE CONDITION PREVENTED: Request 2 Won (201 Created), Request 1 was Safely Rejected (Conflict)."
else
  echo "⚠️ Check the response bodies above."
fi
echo "=========================================================="
rm -f res1.txt res2.txt
