using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnifiedServiceScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleMake = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VehicleModel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VehicleYear = table.Column<int>(type: "integer", nullable: false),
                    VehicleVin = table.Column<string>(type: "character(17)", fixedLength: true, maxLength: 17, nullable: false),
                    VehicleLicensePlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceBayId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActualStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActualEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dealerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealerships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    BayCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Capability = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsUnderMaintenance = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaseDuration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    RequiredSkillLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequiredBayCapability = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Make = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Vin = table.Column<string>(type: "character(17)", fixedLength: true, maxLength: 17, nullable: false),
                    LicensePlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BayBookings",
                columns: table => new
                {
                    BayId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BayBookings", x => new { x.BayId, x.AppointmentId });
                    table.ForeignKey(
                        name: "FK_BayBookings_ServiceBays_BayId",
                        column: x => x.BayId,
                        principalTable: "ServiceBays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianBookings",
                columns: table => new
                {
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianBookings", x => new { x.TechnicianId, x.AppointmentId });
                    table.ForeignKey(
                        name: "FK_TechnicianBookings_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianCertifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IssuedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicianCertifications_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DealershipId_Status",
                table: "Appointments",
                columns: new[] { "DealershipId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ServiceBayId",
                table: "Appointments",
                column: "ServiceBayId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TechnicianId",
                table: "Appointments",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_BayBookings_StartTime_EndTime",
                table: "BayBookings",
                columns: new[] { "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBays_DealershipId_BayCode",
                table: "ServiceBays",
                columns: new[] { "DealershipId", "BayCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBays_DealershipId_Capability_IsUnderMaintenance",
                table: "ServiceBays",
                columns: new[] { "DealershipId", "Capability", "IsUnderMaintenance" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypes_ServiceCategory",
                table: "ServiceTypes",
                column: "ServiceCategory");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianBookings_StartTime_EndTime",
                table: "TechnicianBookings",
                columns: new[] { "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianCertifications_TechnicianId_ServiceCategory",
                table: "TechnicianCertifications",
                columns: new[] { "TechnicianId", "ServiceCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_DealershipId_IsActive",
                table: "Technicians",
                columns: new[] { "DealershipId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_EmployeeNumber",
                table: "Technicians",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OwnerId",
                table: "Vehicles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Vin",
                table: "Vehicles",
                column: "Vin",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "BayBookings");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Dealerships");

            migrationBuilder.DropTable(
                name: "ServiceTypes");

            migrationBuilder.DropTable(
                name: "TechnicianBookings");

            migrationBuilder.DropTable(
                name: "TechnicianCertifications");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "ServiceBays");

            migrationBuilder.DropTable(
                name: "Technicians");
        }
    }
}
