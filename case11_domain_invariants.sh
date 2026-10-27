#!/bin/bash
# case11_domain_invariants.sh
# Demonstrates Input Validation & Domain Invariants Defense-in-Depth:
# 1) Zero / Negative Duration (TimeSlot constructor invariant End <= Start)
# 2) Nil GUID (FluentValidation blocked at API gate)
# 3) Non-Existent Foreign Entity (Repository returns null -> 404 Not Found)

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
TARGET_TIME="2026-11-30T10:00:00Z"

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 11: Domain Invariants & Validation Defense-in-Depth${NC}"
echo -e "${BLUE}==============================================================================${NC}"

# Seed zero duration service
curl -s -X POST "$API/api/v1/dev/seed-corrupted-zero-duration-service" > /dev/null

echo "[1/3] Zero Duration Invariant (TimeSlot End <= Start)..."
S1=$(curl -s -o v1.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"00000000-0000-0000-0000-000000000001","scheduledStartTime":"'"$TARGET_TIME"'"}')
echo "  -> Status: $S1 | Detail: $(grep -o '"detail":"[^"]*' v1.json | cut -d'"' -f4)"

echo "[2/3] Empty / Nil GUID (00000000-0000-0000-0000-000000000000)..."
S2=$(curl -s -o v2.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"00000000-0000-0000-0000-000000000000","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"'"$TARGET_TIME"'"}')
echo "  -> Status: $S2 | Detail: $(grep -o '"detail":"[^"]*' v2.json | cut -d'"' -f4)"

echo "[3/3] Non-Existent Foreign Entity (ffffffff-ffff-ffff-ffff-ffffffffffff)..."
S3=$(curl -s -o v3.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"ffffffff-ffff-ffff-ffff-ffffffffffff","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"'"$TARGET_TIME"'"}')
echo "  -> Status: $S3 | ErrorCode: $(grep -o '"errorCode":"[^"]*' v3.json | cut -d'"' -f4)"
echo ""

if [ "$S1" = "400" ] && [ "$S2" = "400" ] && [ "$S3" = "404" ]; then
  echo -e "${GREEN}✔ SUCCESS: All domain invariants and validation rules enforced!${NC}"
else
  echo -e "${RED}✘ One or more invariant tests failed.${NC}"
fi

rm -f v1.json v2.json v3.json
echo -e "${BLUE}==============================================================================${NC}"
