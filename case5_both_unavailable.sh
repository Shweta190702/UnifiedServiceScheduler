#!/bin/bash
# case5_both_unavailable.sh
# Demonstrates:
# All compatible bays and all qualified technicians are booked for the window.
# Expected: 409 Conflict (Code: SlotUnavailable).

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
TARGET_TIME="2026-11-04T10:00:00Z"

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 5: Both Resources Unavailable${NC}"
echo -e "${BLUE}==============================================================================${NC}"

# Pre-occupy all bays and all technicians for 2026-11-04 10:00 UTC
curl -s -X POST "$API/api/v1/dev/scenario/setup-both-unavailable" > /dev/null

echo "Scenario Setup:"
echo "  • Slot: $TARGET_TIME (10:00 - 11:00 UTC)"
echo "  • All Lift bays (BAY-01, BAY-02) are 100% booked."
echo "  • All Brake technicians (John, Sarah, Mike) are 100% booked."
echo "  • Expectation: 409 Conflict (SlotUnavailable)."
echo ""

STATUS=$(curl -s -o res.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER"'",
    "vehicleId": "'"$VEHICLE"'",
    "serviceTypeId": "'"$BRAKE_SVC"'",
    "scheduledStartTime": "'"$TARGET_TIME"'",
    "notes": "Case 5: Both Unavailable Test"
  }')

echo "HTTP Response Status: $STATUS"

if [ "$STATUS" = "409" ]; then
  ERR_CODE=$(grep -o '"errorCode":"[^"]*' res.json | cut -d'"' -f4)
  DETAIL=$(grep -o '"detail":"[^"]*' res.json | cut -d'"' -f4)

  echo -e "${GREEN}✔ SUCCESS: 409 Conflict Safely Handled!${NC}"
  echo "  • Error Code: $ERR_CODE"
  echo "  • Detail:     $DETAIL"
else
  echo -e "${RED}✘ FAILED: Expected 409 Conflict${NC}"
  cat res.json
fi

rm -f res.json
echo -e "${BLUE}==============================================================================${NC}"
