#!/bin/bash
# case10_boundary_rejections.sh
# Demonstrates Operating Hours & Timestamp Boundary Rejections:
# 1) Spill-Over Past Closing Hours (17:30 UTC for 1h service when closing is 18:00 UTC)
# 2) Outside Operating Hours / Closed Days (Sunday)
# 3) Past Timestamp / Stale Request
# Expected: All 3 rejected with 400 Bad Request.

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

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 10: Operating Hours & Timestamp Boundary Rejections${NC}"
echo -e "${BLUE}==============================================================================${NC}"

echo "[1/3] Spill-Over Past Closing (17:30 UTC for 1h service; Closes at 18:00 UTC)..."
S1=$(curl -s -o b1.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"2026-11-16T17:30:00Z"}')
echo "  -> Status: $S1 | ErrorCode: $(grep -o '"errorCode":"[^"]*' b1.json | cut -d'"' -f4)"

echo "[2/3] Closed Days (Sunday 2026-11-15)..."
S2=$(curl -s -o b2.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"2026-11-15T10:00:00Z"}')
echo "  -> Status: $S2 | ErrorCode: $(grep -o '"errorCode":"[^"]*' b2.json | cut -d'"' -f4)"

echo "[3/3] Past Timestamp / Stale Request (2020-01-01)..."
S3=$(curl -s -o b3.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"2020-01-01T10:00:00Z"}')
echo "  -> Status: $S3 | ErrorCode: $(grep -o '"errorCode":"[^"]*' b3.json | cut -d'"' -f4)"
echo ""

if [ "$S1" = "400" ] && [ "$S2" = "400" ] && [ "$S3" = "400" ]; then
  echo -e "${GREEN}✔ SUCCESS: All boundary edge cases rejected with 400 Bad Request!${NC}"
else
  echo -e "${RED}✘ One or more boundary tests failed.${NC}"
fi

rm -f b1.json b2.json b3.json
echo -e "${BLUE}==============================================================================${NC}"
