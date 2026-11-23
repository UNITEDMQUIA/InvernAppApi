using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SoilHumidityMinimum = table.Column<double>(type: "float", nullable: false),
                    SoilHumidityTarget = table.Column<double>(type: "float", nullable: false),
                    MaximumWateringSeconds = table.Column<int>(type: "int", nullable: false),
                    MaximumTemperature = table.Column<double>(type: "float", nullable: false),
                    TemperatureHysteresis = table.Column<double>(type: "float", nullable: false),
                    CriticalTemperature = table.Column<double>(type: "float", nullable: false),
                    MinimumLuminosity = table.Column<double>(type: "float", nullable: false),
                    TargetLuminosity = table.Column<double>(type: "float", nullable: false),
                    WateringEnabled = table.Column<bool>(type: "bit", nullable: false),
                    VentilationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LightingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                    table.CheckConstraint("CK_Configurations_Luminosity", "[MinimumLuminosity] >= 0 AND [TargetLuminosity] <= 100 AND [MinimumLuminosity] < [TargetLuminosity]");
                    table.CheckConstraint("CK_Configurations_SingleRow", "[Id] = 1");
                    table.CheckConstraint("CK_Configurations_SoilHumidity", "[SoilHumidityMinimum] >= 0 AND [SoilHumidityTarget] <= 100 AND [SoilHumidityMinimum] < [SoilHumidityTarget]");
                    table.CheckConstraint("CK_Configurations_Temperature", "[TemperatureHysteresis] > 0 AND [CriticalTemperature] > [MaximumTemperature]");
                    table.CheckConstraint("CK_Configurations_WateringTime", "[MaximumWateringSeconds] > 0");
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.UniqueConstraint("UQ_Devices_DeviceId", x => x.DeviceId);
                });

            migrationBuilder.CreateTable(
                name: "ActuatorStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Actuator = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsOn = table.Column<bool>(type: "bit", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActuatorStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActuatorStates_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Commands",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AckMessage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commands", x => x.Id);
                    table.CheckConstraint("CK_Commands_Expiration", "[ExpiresAt] > [CreatedAt]");
                    table.ForeignKey(
                        name: "FK_Commands_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    AirHumidity = table.Column<double>(type: "float", nullable: false),
                    SoilHumidity = table.Column<double>(type: "float", nullable: true),
                    IsSoilHumiditySimulated = table.Column<bool>(type: "bit", nullable: false),
                    Luminosity = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.CheckConstraint("CK_Measurements_AirHumidity", "[AirHumidity] >= 0 AND [AirHumidity] <= 100");
                    table.CheckConstraint("CK_Measurements_Luminosity", "[Luminosity] >= 0 AND [Luminosity] <= 100");
                    table.CheckConstraint("CK_Measurements_SoilHumidity", "[SoilHumidity] IS NULL OR ([SoilHumidity] >= 0 AND [SoilHumidity] <= 100)");
                    table.CheckConstraint("CK_Measurements_Temperature", "[Temperature] >= -40 AND [Temperature] <= 80");
                    table.ForeignKey(
                        name: "FK_Measurements_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CommandId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Commands_CommandId",
                        column: x => x.CommandId,
                        principalTable: "Commands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Events_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Configurations",
                columns: new[] { "Id", "CriticalTemperature", "LightingEnabled", "MaximumTemperature", "MaximumWateringSeconds", "MinimumLuminosity", "SoilHumidityMinimum", "SoilHumidityTarget", "TargetLuminosity", "TemperatureHysteresis", "UpdatedAt", "VentilationEnabled", "WateringEnabled" },
                values: new object[] { 1, 35.0, true, 30.0, 30, 30.0, 30.0, 45.0, 45.0, 2.0, new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Utc), true, false });

            migrationBuilder.CreateIndex(
                name: "UX_ActuatorStates_DeviceId_Actuator",
                table: "ActuatorStates",
                columns: new[] { "DeviceId", "Actuator" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commands_DeviceId_Status_CreatedAt",
                table: "Commands",
                columns: new[] { "DeviceId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_CommandId",
                table: "Events",
                column: "CommandId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedAt",
                table: "Events",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Events_DeviceId_CreatedAt",
                table: "Events",
                columns: new[] { "DeviceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_DeviceId_Timestamp",
                table: "Measurements",
                columns: new[] { "DeviceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "UX_Measurements_DeviceId_SequenceNumber",
                table: "Measurements",
                columns: new[] { "DeviceId", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActuatorStates");

            migrationBuilder.DropTable(
                name: "Configurations");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "Commands");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
