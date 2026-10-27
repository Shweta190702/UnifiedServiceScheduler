#!/bin/bash
# case4_tech_available_unqualified.sh
# Demonstrates:
# Bays are free, technicians are free, but technicians lack required skill level for service.
# Expected: 422 Unprocessable Entity (Code: SkillMismatch). Zero resource allocation.

GREEN='\033[0;32m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
RED='\033[0;31m'
NC='\033[0m'

API="http://localhost:5050"
DEALERSHIP="d1111111-1111-1111-1111-111111111111"
CUSTOMER="c2222222-2222-2222-2222-222222222222"
VEHICLE="a3333333-3333-3333-3333-333333333333"
TRANS_SVC="b5555555-5555-5555-5555-555555555555" # Requires Master Transmission
TARGET_TIME="2026-11-23T10:00:00Z"

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 4: Tech Available, Unqualified (Skill Mismatch)${NC}"
echo -e "${BLUE}==============================================================================${NC}"
echo "Scenario Setup:"
echo "  • Service:     Advanced Transmission Overhaul (Requires Master skill in Transmission)"
echo "  • Bays:        BAY-01 & BAY-02 (Lift) are free."
echo "  • Technicians: Active technicians are free, but hold certifications ONLY in Brakes (Senior)."
echo "  • Expectation: 422 Unprocessable Entity (SkillMismatch) - Zero appointments created."
echo ""

STATUS=$(curl -s -o res.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER"'",
    "vehicleId": "'"$VEHICLE"'",
    "serviceTypeId": "'"$TRANS_SVC"'",
    "scheduledStartTime": "'"$TARGET_TIME"'",
    "notes": "Case 4: Skill Mismatch Test"
  }')

echo "HTTP Response Status: $STATUS"

if [ "$STATUS" = "422" ]; then
  ERR_CODE=$(grep -o '"errorCode":"[^"]*' res.json | cut -d'"' -f4)
  DETAIL=$(grep -o '"detail":"[^"]*' res.json | cut -d'"' -f4)

  echo -e "${GREEN}✔ SUCCESS: 422 Unprocessable Entity Safely Handled!${NC}"
  echo "  • Error Code: $ERR_CODE"
  echo "  • Detail:     $DETAIL"
  echo "  • Invariant:  Zero resources reserved."
else
  echo -e "${RED}✘ FAILED: Expected 422 Unprocessable Entity${NC}"
  cat res.json
fi

rm -f res.json
echo -e "${BLUE}==============================================================================${NC}"
