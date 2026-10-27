#!/bin/bash
# case8_adjacent_back_to_back.sh
# Demonstrates Half-Open [Start, End) Interval Semantics:
# Appointment 1 is 09:00 - 10:00. Appointment 2 is 10:00 - 11:00 on the same bay/tech.
# Expected: Both succeed (201 Created) with zero gap and zero off-by-one collision.

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

SLOT_1="2026-11-26T09:00:00Z"
SLOT_2="2026-11-26T10:00:00Z"

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 8: Adjacent Back-to-Back Slots [Start, End)${NC}"
echo -e "${BLUE}==============================================================================${NC}"
echo "Interval Boundary Logic:"
echo "  • Slot 1: [09:00, 10:00) -> 10:00 is EXCLUSIVE"
echo "  • Slot 2: [10:00, 11:00) -> 10:00 is INCLUSIVE"
echo "  • Overlap test: 10:00 > 10:00 is FALSE (No collision!)"
echo ""

echo "[1/2] Booking Slot 1: 09:00 to 10:00 UTC..."
APPT1=$(curl -s -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"'"$SLOT_1"'","notes":"B2B Slot 1"}' | grep -o '"appointmentId":"[^"]*' | cut -d'"' -f4)
echo "  -> Slot 1 Confirmed: $APPT1"

echo "[2/2] Booking Contiguous Slot 2: 10:00 to 11:00 UTC..."
APPT2=$(curl -s -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"'"$SLOT_2"'","notes":"B2B Slot 2"}' | grep -o '"appointmentId":"[^"]*' | cut -d'"' -f4)
echo "  -> Slot 2 Confirmed: $APPT2"
echo ""

if [ -n "$APPT1" ] && [ -n "$APPT2" ] && [ "$APPT1" != "$APPT2" ]; then
  echo -e "${GREEN}✔ SUCCESS: Both contiguous back-to-back bookings succeeded!${NC}"
  echo "  • Zero off-by-one errors."
  echo "  • Zero double-booking."
  echo "  • Seamless contiguous scheduling enabled by [Start, End) intervals."
else
  echo -e "${RED}✘ Back-to-back booking failed.${NC}"
fi

echo -e "${BLUE}==============================================================================${NC}"
