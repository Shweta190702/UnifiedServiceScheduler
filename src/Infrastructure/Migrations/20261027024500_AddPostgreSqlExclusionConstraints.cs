using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnifiedServiceScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostgreSqlExclusionConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Enable btree_gist extension for combining scalar equality with range overlap
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // 2. Strict physical exclusion constraint on TechnicianBookings [StartTime, EndTime)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'no_overlapping_technician_bookings'
                    ) THEN
                        ALTER TABLE ""TechnicianBookings""
                        ADD CONSTRAINT ""no_overlapping_technician_bookings""
                        EXCLUDE USING gist (
                            ""TechnicianId"" WITH =,
                            tstzrange(""StartTime"", ""EndTime"", '[)') WITH &&
                        );
                    END IF;
                END $$;");

            // 3. Strict physical exclusion constraint on BayBookings [StartTime, EndTime)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'no_overlapping_bay_bookings'
                    ) THEN
                        ALTER TABLE ""BayBookings""
                        ADD CONSTRAINT ""no_overlapping_bay_bookings""
                        EXCLUDE USING gist (
                            ""BayId"" WITH =,
                            tstzrange(""StartTime"", ""EndTime"", '[)') WITH &&
                        );
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""TechnicianBookings"" DROP CONSTRAINT IF EXISTS ""no_overlapping_technician_bookings"";");
            migrationBuilder.Sql(@"ALTER TABLE ""BayBookings"" DROP CONSTRAINT IF EXISTS ""no_overlapping_bay_bookings"";");
        }
    }
}
