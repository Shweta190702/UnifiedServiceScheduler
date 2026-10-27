#!/bin/bash
# case1_happy_path.sh
# Demonstrates a normal successful booking:
# Bay is free, certified technician is free, time is within operating hours.

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
echo -e "${CYAN}▶ CASE 1: Normal Happy Path Booking${NC}"
echo -e "${BLUE}==============================================================================${NC}"
echo "Request Details:"
echo "  • Dealership:   Downtown Automotive Center ($DEALERSHIP)"
echo "  • Requested:    $TARGET_TIME (10:00 - 11:00 UTC)"
echo "  • Service:      Standard Brake Service (1h, requires Lift bay + Journeyman tech)"
echo "  • Expectation:  201 Created with canonical Location header, assigned bay & tech"
echo ""

STATUS=$(curl -s -o res.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "'"$CUSTOMER"'",
    "vehicleId": "'"$VEHICLE"'",
    "serviceTypeId": "'"$BRAKE_SVC"'",
    "scheduledStartTime": "'"$TARGET_TIME"'",
    "notes": "Case 1: Happy Path Booking"
  }')

echo "HTTP Response Status: $STATUS"

if [ "$STATUS" = "201" ]; then
  APPT_ID=$(grep -o '"appointmentId":"[^"]*' res.json | cut -d'"' -f4)
  TECH=$(grep -o '"technicianName":"[^"]*' res.json | cut -d'"' -f4)
  BAY=$(grep -o '"bayCode":"[^"]*' res.json | cut -d'"' -f4)
  START=$(grep -o '"scheduledStartTime":"[^"]*' res.json | cut -d'"' -f4)
  END=$(grep -o '"scheduledEndTime":"[^"]*' res.json | cut -d'"' -f4)

  echo -e "${GREEN}✔ SUCCESS: 201 Created${NC}"
  echo "  • Appointment ID: $APPT_ID"
  echo "  • Assigned Tech:  $TECH"
  echo "  • Assigned Bay:   $BAY"
  echo "  • Scheduled Time: $START to $END"
else
  echo -e "${RED}✘ FAILED: Expected 201 Created${NC}"
  cat res.json
fi

rm -f res.json
echo -e "${BLUE}==============================================================================${NC}"
