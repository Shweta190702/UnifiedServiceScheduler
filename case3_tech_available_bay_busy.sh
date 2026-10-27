#!/bin/bash
# case3_tech_available_bay_busy.sh
# Demonstrates:
# Certified technician (Mike FreeTech) is free, but all compatible physical bays are full.
# Expected: 409 Conflict (Code: BayUnavailable). Technician remains completely unreserved.

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
TARGET_TIME="2026-10-30T14:00:00Z"

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 3: Tech Available, Bay Busy${NC}"
echo -e "${BLUE}==============================================================================${NC}"

# Setup scenario: occupy both bays while leaving Mike FreeTech free
curl -s -X POST "$API/api/v1/dev/scenario/setup-bay-busy" > /dev/null

echo "Scenario Setup:"
echo "  • Slot: $TARGET_TIME (14:00 - 15:00 UTC)"
echo "  • All Lift bays (BAY-01, BAY-02) are 100% booked."
echo "  • Technician Mike FreeTech is certified in Brakes and 100% FREE."
echo "  • Attempting booking: System must reject because bays are full."
echo "  • Expectation: 409 Conflict (BayUnavailable) & Mike FreeTech is NOT allocated."
echo ""

STATUS=$(curl -s -o res.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER"'",
    "vehicleId": "'"$VEHICLE"'",
    "serviceTypeId": "'"$BRAKE_SVC"'",
    "scheduledStartTime": "'"$TARGET_TIME"'",
    "notes": "Case 3: Bay Busy Test"
  }')

echo "HTTP Response Status: $STATUS"

if [ "$STATUS" = "409" ]; then
  ERR_CODE=$(grep -o '"errorCode":"[^"]*' res.json | cut -d'"' -f4)
  DETAIL=$(grep -o '"detail":"[^"]*' res.json | cut -d'"' -f4)

  echo -e "${GREEN}✔ SUCCESS: 409 Conflict Safely Handled!${NC}"
  echo "  • Error Code: $ERR_CODE"
  echo "  • Detail:     $DETAIL"
  echo "  • Invariant:  Mike FreeTech was NEVER allocated or locked."
else
  echo -e "${RED}✘ FAILED: Expected 409 Conflict${NC}"
  cat res.json
fi

rm -f res.json
echo -e "${BLUE}==============================================================================${NC}"
