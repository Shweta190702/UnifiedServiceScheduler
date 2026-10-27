#!/bin/bash
# case6_simultaneous_contention.sh
# Demonstrates Race Condition Prevention:
# Only 1 bay remains (BAY-02). Requests A & B hit the API concurrently at the exact same millisecond.
# Expected: Exactly ONE request wins (201 Created); the other loses (409 Conflict). Zero double booking.

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
TARGET_TIME="2026-11-05T14:00:00Z"

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 6: Simultaneous Contention (Race Condition on Last Bay)${NC}"
echo -e "${BLUE}==============================================================================${NC}"

# Pre-occupy BAY-01 so ONLY 1 bay remains
curl -s -X POST "$API/api/v1/dev/scenario/setup-last-bay-contention" > /dev/null

echo "Scenario Setup:"
echo "  • Slot: $TARGET_TIME (14:00 - 15:00 UTC)"
echo "  • Only 1 bay remains: BAY-02 (BAY-01 is booked)."
echo "  • Multiple qualified technicians are available."
echo "  • Firing Request A and Request B concurrently in background subshells..."
echo ""

# Fire concurrent subshells
curl -s -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"'"$TARGET_TIME"'","notes":"Contention A"}' > rA.txt &
P1=$!

curl -s -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"'"$TARGET_TIME"'","notes":"Contention B"}' > rB.txt &
P2=$!

wait $P1 $P2

STATUS_A=$(cat rA.txt | grep -o '^[0-9]\{3\}')
STATUS_B=$(cat rB.txt | grep -o '^[0-9]\{3\}')
rm -f rA.txt rB.txt

echo "Request A Status: $STATUS_A"
echo "Request B Status: $STATUS_B"
echo ""

if ([ "$STATUS_A" = "201" ] && [ "$STATUS_B" = "409" ]) || ([ "$STATUS_A" = "409" ] && [ "$STATUS_B" = "201" ]); then
  echo -e "${GREEN}✔ RACE CONDITION FULLY PREVENTED!${NC}"
  echo "  • Exactly ONE request succeeded (201 Created) and claimed BAY-02."
  echo "  • The concurrent request was rejected (409 Conflict)."
  echo "  • Physical double-booking prevented by PostgreSQL GiST exclusion constraints."
else
  echo -e "${RED}✘ UNEXPECTED: Request A=$STATUS_A, Request B=$STATUS_B${NC}"
fi

echo -e "${BLUE}==============================================================================${NC}"
