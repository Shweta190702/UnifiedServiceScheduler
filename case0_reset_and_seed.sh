#!/bin/bash
# case0_reset_and_seed.sh
# Wipes all tables and resets PostgreSQL to a pristine baseline state.

GREEN='\033[0;32m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${BLUE}==============================================================================${NC}"
echo -e "${CYAN}▶ CASE 0: Reset Database & Seed Clean Baseline Entities${NC}"
echo -e "${BLUE}==============================================================================${NC}"

RESP=$(curl -s -X POST http://localhost:5050/api/v1/dev/reset-and-seed)

echo -e "${GREEN}✔ Database reset and seeded successfully!${NC}"
echo ""
echo "Seeded Baseline Data:"
echo "  • Dealership:  Downtown Automotive Center (Open Mon-Fri 08:00–18:00 UTC)"
echo "  • Customer:    Jane Doe (c2222222-2222-2222-2222-222222222222)"
echo "  • Vehicle:     2024 Tesla Model Y (a3333333-3333-3333-3333-333333333333)"
echo "  • ServiceBays: BAY-01 (Lift), BAY-02 (Lift), BAY-03 (Standard)"
echo "  • Technicians: John Smith (Senior Brakes), Sarah Connor (Senior Brakes), Mike FreeTech (Senior Brakes)"
echo "  • Services:    Standard Brake Service (1h), HV Battery Diagnostics (2h), Advanced Transmission (2h)"
echo ""
echo "Inspect tables at: http://localhost:5050/api/v1/dev/tables"
