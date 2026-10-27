#!/bin/bash
# case9_temporal_overlaps.sh
# Demonstrates Mathematical Interval Overlap Detection:
# 1) Head Collision (09:00-10:00 vs 09:30-10:30)
# 2) Tail Collision (09:30-10:30 vs 09:00-10:00)
# 3) Enclosing Window (09:00-11:00 vs 09:30-10:00)
# Expected: All 3 rejected with 409 Conflict.

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
echo -e "${CYAN}▶ CASE 9: Temporal Overlap Collisions (Head, Tail, Enclosing)${NC}"
echo -e "${BLUE}==============================================================================${NC}"

# Seed baseline appointments
curl -s -X POST "$API/api/v1/dev/scenario/setup-overlap-scenarios" > /dev/null

echo "[1/3] Head Collision: Existing is 09:30–10:30; Requesting 09:00–10:00..."
S1=$(curl -s -o o1.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"2026-11-11T09:00:00Z"}')
echo "  -> Status: $S1 ($(grep -o '"detail":"[^"]*' o1.json | cut -d'"' -f4))"

echo "[2/3] Tail Collision: Existing is 09:00–10:00; Requesting 09:30–10:30..."
S2=$(curl -s -o o2.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"2026-11-12T09:30:00Z"}')
echo "  -> Status: $S2 ($(grep -o '"detail":"[^"]*' o2.json | cut -d'"' -f4))"

echo "[3/3] Enclosing Window: Existing is 09:30–10:00 (30m); Requesting 09:00–11:00 (2h)..."
S3=$(curl -s -o o3.json -w "%{http_code}" -X POST "$API/api/v1/dealerships/$DEALERSHIP/appointments" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"'"$CUSTOMER"'","vehicleId":"'"$VEHICLE"'","serviceTypeId":"'"$BRAKE_SVC"'","scheduledStartTime":"2026-11-13T09:00:00Z"}')
echo "  -> Status: $S3 ($(grep -o '"detail":"[^"]*' o3.json | cut -d'"' -f4))"
echo ""

if [ "$S1" = "409" ] && [ "$S2" = "409" ] && [ "$S3" = "409" ]; then
  echo -e "${GREEN}✔ SUCCESS: All 3 temporal overlap geometries rejected with 409 Conflict!${NC}"
else
  echo -e "${RED}✘ One or more overlap tests failed.${NC}"
fi

rm -f o1.json o2.json o3.json
echo -e "${BLUE}==============================================================================${NC}"
