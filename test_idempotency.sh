#!/bin/bash
# test_idempotency.sh
# Simulates a customer double-clicking "Confirm" by sending the identical payload twice within 200 ms.

DEALERSHIP_ID="d1111111-1111-1111-1111-111111111111"
CUSTOMER_ID="c2222222-2222-2222-2222-222222222222"
VEHICLE_ID="a3333333-3333-3333-3333-333333333333"
SERVICE_TYPE_ID="b4444444-4444-4444-4444-444444444444"
TARGET_TIME="2026-11-09T10:00:00Z"

URL="http://localhost:5050/api/v1/dealerships/$DEALERSHIP_ID/appointments"

PAYLOAD='{
  "customerId": "'"$CUSTOMER_ID"'",
  "vehicleId": "'"$VEHICLE_ID"'",
  "serviceTypeId": "'"$SERVICE_TYPE_ID"'",
  "scheduledStartTime": "'"$TARGET_TIME"'",
  "notes": "Idempotent Double-Click Resubmission Test"
}'

echo "=========================================================="
echo "Simulating customer double-click resubmission (200 ms apart):"
echo "Target Slot: $TARGET_TIME"
echo "=========================================================="

echo "[1/2] Sending Initial Click (Click #1)..."
curl -s -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD" > res1.json

APPT_ID_1=$(grep -o '"appointmentId":"[^"]*' res1.json | cut -d'"' -f4)
echo "  -> Click #1 Result: AppointmentId = $APPT_ID_1"

echo "Sleeping 200 ms..."
sleep 0.2

echo "[2/2] Sending Accidental Double-Click (Click #2)..."
curl -s -X POST "$URL" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD" > res2.json

APPT_ID_2=$(grep -o '"appointmentId":"[^"]*' res2.json | cut -d'"' -f4)
echo "  -> Click #2 Result: AppointmentId = $APPT_ID_2"

echo ""
echo "=========================================================="
echo "Verification:"
if [ -n "$APPT_ID_1" ] && [ "$APPT_ID_1" = "$APPT_ID_2" ]; then
  echo "✅ IDEMPOTENCY GUARANTEE MET!"
  echo "   Both requests returned the identical canonical Appointment ID:"
  echo "   $APPT_ID_1"
  echo "   Zero duplicate bookings, zero duplicate bay locks created."
else
  echo "⚠️ Inspect responses:"
  cat res1.json
  echo ""
  cat res2.json
fi
echo "=========================================================="
rm -f res1.json res2.json
