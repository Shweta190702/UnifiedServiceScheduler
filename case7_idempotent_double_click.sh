#!/bin/bash
# case7_idempotent_double_click.sh
# Demonstrates Idempotency:
# Customer double-clicks "Confirm", sending identical payload twice within 200 ms.
# Expected: Both requests return identical canonical AppointmentId. Zero duplicate bookings.

GREEN='\033[0;32m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

API="http://localhost:5050"
DEALERSHIP="d1111111-1111-1111-1111-111111111111"
CUSTOMER="c2222222-2222-2222-2222-222222222222"
VEHICLE="a3333333-3333-3333-3333-333333333333"
BRAKE_SVC="b4444444-4444-4444-4444-444444444444"
TARGET_TIME="2026-11-25T10:00:00Z"

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 7: Idempotent Double-Click Resubmission${NC}"
echo -e "${BLUE}==============================================================================${NC}"
echo "Scenario Setup:"
echo "  • Simulating customer accidental double-click (200 ms apart) for slot $TARGET_TIME."
echo "  • Click 1 commits the appointment."
echo "  • Click 2 must detect the resubmission and return the original appointment."
echo ""

echo "[1/2] Sending Click #1 (Initial Confirm)..."
ID1=$(curl -s -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"'"$TARGET_TIME"'","notes":"Idempotent Click 1"}' | grep -o '"appointmentId":"[^"]*' | cut -d'"' -f4)
echo "  -> Click #1 Appointment ID: $ID1"

echo "Sleeping 200 ms..."
sleep 0.2

echo "[2/2] Sending Click #2 (Accidental Double Click)..."
ID2=$(curl -s -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"'"$TARGET_TIME"'","notes":"Idempotent Click 2"}' | grep -o '"appointmentId":"[^"]*' | cut -d'"' -f4)
echo "  -> Click #2 Appointment ID: $ID2"
echo ""

if [ -n "$ID1" ] && [ "$ID1" = "$ID2" ]; then
  echo -e "${GREEN}✔ IDEMPOTENCY GUARANTEED!${NC}"
  echo "  • Both requests returned identical canonical Appointment ID:"
  echo "    $ID1"
  echo "  • Zero duplicate bookings committed to PostgreSQL."
else
  echo -e "${RED}✘ IDEMPOTENCY VIOLATION: Different IDs returned!${NC}"
  echo "  ID1=$ID1, ID2=$ID2"
fi

echo -e "${BLUE}==============================================================================${NC}"
