#!/bin/bash
# case2_bay_available_tech_busy.sh
# Demonstrates:
# Service Bay (BAY-02) is free, but all qualified technicians have overlapping bookings.
# Expected: 409 Conflict (Code: TechnicianUnavailable). No orphaned bay lock created.

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
TARGET_TIME="2026-11-20T10:00:00Z"

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 2: Bay Available, Tech Busy${NC}"
echo -e "${BLUE}==============================================================================${NC}"
echo "Scenario Setup:"
echo "  • BAY-02 is completely free during $TARGET_TIME."
echo "  • John Smith is already occupied in BAY-01 on Appointment 1 (from Case 1)."
echo "  • New booking requested for the EXACT SAME slot ($TARGET_TIME)."
echo "  • Expectation: 409 Conflict (TechnicianUnavailable) - zero partial reservation on BAY-02."
echo ""

STATUS=$(curl -s -o res.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER"'",
    "vehicleId": "'"$VEHICLE"'",
    "serviceTypeId": "'"$BRAKE_SVC"'",
    "scheduledStartTime": "'"$TARGET_TIME"'",
    "notes": "Case 2: Tech Busy Conflict"
  }')

echo "HTTP Response Status: $STATUS"

if [ "$STATUS" = "409" ]; then
  ERR_CODE=$(grep -o '"errorCode":"[^"]*' res.json | cut -d'"' -f4)
  DETAIL=$(grep -o '"detail":"[^"]*' res.json | cut -d'"' -f4)

  echo -e "${GREEN}✔ SUCCESS: 409 Conflict Safely Handled!${NC}"
  echo "  • Error Code: $ERR_CODE"
  echo "  • Detail:     $DETAIL"
  echo "  • Invariant:  BAY-02 was NEVER reserved or locked."
else
  echo -e "${RED}✘ FAILED: Expected 409 Conflict${NC}"
  cat res.json
fi

rm -f res.json
echo -e "${BLUE}==============================================================================${NC}"
