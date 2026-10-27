#!/bin/bash
# test_back_to_back.sh
# Tests adjacent back-to-back slot scheduling (09:00-10:00 and 10:00-11:00) using half-open [Start, End) intervals.

DEALERSHIP_ID="d1111111-1111-1111-1111-111111111111"
CUSTOMER_ID="c2222222-2222-2222-2222-222222222222"
VEHICLE_ID="a3333333-3333-3333-3333-333333333333"
SERVICE_TYPE_ID="b4444444-4444-4444-4444-444444444444"

URL="http://localhost:5050/api/v1/dealerships/$DEALERSHIP_ID/appointments"

SLOT_1="2026-11-10T09:00:00Z"
SLOT_2="2026-11-10T10:00:00Z"

echo "=========================================================="
echo "Testing Adjacent Back-to-Back Slot Scheduling:"
echo "Slot 1: $SLOT_1 (09:00 - 10:00 UTC)"
echo "Slot 2: $SLOT_2 (10:00 - 11:00 UTC)"
echo "=========================================================="

echo "[1/2] Booking Slot 1 (09:00 - 10:00 UTC)..."
curl -s -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$SERVICE_TYPE_ID"'",
    "scheduledStartTime": "'"$SLOT_1"'",
    "notes": "Back-to-Back: Slot 1 (09:00-10:00)"
  }' > b2b_res1.json

STATUS_1=$(grep -o '"status":"[^"]*' b2b_res1.json | cut -d'"' -f4)
APPT_1=$(grep -o '"appointmentId":"[^"]*' b2b_res1.json | cut -d'"' -f4)
BAY_1=$(grep -o '"bayCode":"[^"]*' b2b_res1.json | cut -d'"' -f4)
TECH_1=$(grep -o '"technicianName":"[^"]*' b2b_res1.json | cut -d'"' -f4)

echo "  -> Slot 1 Result: Status=$STATUS_1, ApptId=$APPT_1, Bay=$BAY_1, Tech=$TECH_1"

echo ""
echo "[2/2] Booking Slot 2 (10:00 - 11:00 UTC) contiguous adjacent slot..."
curl -s -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER_ID"'",
    "vehicleId": "'"$VEHICLE_ID"'",
    "serviceTypeId": "'"$SERVICE_TYPE_ID"'",
    "scheduledStartTime": "'"$SLOT_2"'",
    "notes": "Back-to-Back: Slot 2 (10:00-11:00)"
  }' > b2b_res2.json

STATUS_2=$(grep -o '"status":"[^"]*' b2b_res2.json | cut -d'"' -f4)
APPT_2=$(grep -o '"appointmentId":"[^"]*' b2b_res2.json | cut -d'"' -f4)
BAY_2=$(grep -o '"bayCode":"[^"]*' b2b_res2.json | cut -d'"' -f4)
TECH_2=$(grep -o '"technicianName":"[^"]*' b2b_res2.json | cut -d'"' -f4)

echo "  -> Slot 2 Result: Status=$STATUS_2, ApptId=$APPT_2, Bay=$BAY_2, Tech=$TECH_2"

echo ""
echo "=========================================================="
echo "Verification:"
if [ -n "$APPT_1" ] && [ -n "$APPT_2" ] && [ "$APPT_1" != "$APPT_2" ]; then
  echo "✅ HALF-OPEN INTERVAL SUCCESS: Both contiguous back-to-back bookings SUCCEEDED!"
  echo "   Appointment 1 [09:00, 10:00): $APPT_1 ($TECH_1 in $BAY_1)"
  echo "   Appointment 2 [10:00, 11:00): $APPT_2 ($TECH_2 in $BAY_2)"
  echo "   Zero collision, zero off-by-one errors."
else
  echo "⚠️ One or both bookings did not succeed. Responses:"
  cat b2b_res1.json
  echo ""
  cat b2b_res2.json
fi
echo "=========================================================="
rm -f b2b_res1.json b2b_res2.json
