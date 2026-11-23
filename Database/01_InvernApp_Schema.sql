IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE TABLE [Configurations] (
        [Id] int NOT NULL,
        [SoilHumidityMinimum] float NOT NULL,
        [SoilHumidityTarget] float NOT NULL,
        [MaximumWateringSeconds] int NOT NULL,
        [MaximumTemperature] float NOT NULL,
        [TemperatureHysteresis] float NOT NULL,
        [CriticalTemperature] float NOT NULL,
        [MinimumLuminosity] float NOT NULL,
        [TargetLuminosity] float NOT NULL,
        [WateringEnabled] bit NOT NULL,
        [VentilationEnabled] bit NOT NULL,
        [LightingEnabled] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Configurations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Configurations_Luminosity] CHECK ([MinimumLuminosity] >= 0 AND [TargetLuminosity] <= 100 AND [MinimumLuminosity] < [TargetLuminosity]),
        CONSTRAINT [CK_Configurations_SingleRow] CHECK ([Id] = 1),
        CONSTRAINT [CK_Configurations_SoilHumidity] CHECK ([SoilHumidityMinimum] >= 0 AND [SoilHumidityTarget] <= 100 AND [SoilHumidityMinimum] < [SoilHumidityTarget]),
        CONSTRAINT [CK_Configurations_Temperature] CHECK ([TemperatureHysteresis] > 0 AND [CriticalTemperature] > [MaximumTemperature]),
        CONSTRAINT [CK_Configurations_WateringTime] CHECK ([MaximumWateringSeconds] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE TABLE [Devices] (
        [Id] int NOT NULL IDENTITY,
        [DeviceId] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [RegisteredAt] datetime2 NOT NULL,
        [LastSeenAt] datetime2 NULL,
        CONSTRAINT [PK_Devices] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Devices_DeviceId] UNIQUE ([DeviceId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE TABLE [ActuatorStates] (
        [Id] int NOT NULL IDENTITY,
        [DeviceId] nvarchar(50) NOT NULL,
        [Actuator] nvarchar(20) NOT NULL,
        [IsOn] bit NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ActuatorStates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActuatorStates_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([DeviceId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE TABLE [Commands] (
        [Id] bigint NOT NULL IDENTITY,
        [DeviceId] nvarchar(50) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Reason] nvarchar(200) NULL,
        [AckMessage] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [AcknowledgedAt] datetime2 NULL,
        CONSTRAINT [PK_Commands] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Commands_Expiration] CHECK ([ExpiresAt] > [CreatedAt]),
        CONSTRAINT [FK_Commands_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([DeviceId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE TABLE [Measurements] (
        [Id] bigint NOT NULL IDENTITY,
        [DeviceId] nvarchar(50) NOT NULL,
        [SequenceNumber] bigint NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [ReceivedAt] datetime2 NOT NULL,
        [Temperature] float NOT NULL,
        [AirHumidity] float NOT NULL,
        [SoilHumidity] float NULL,
        [IsSoilHumiditySimulated] bit NOT NULL,
        [Luminosity] float NOT NULL,
        CONSTRAINT [PK_Measurements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Measurements_AirHumidity] CHECK ([AirHumidity] >= 0 AND [AirHumidity] <= 100),
        CONSTRAINT [CK_Measurements_Luminosity] CHECK ([Luminosity] >= 0 AND [Luminosity] <= 100),
        CONSTRAINT [CK_Measurements_SoilHumidity] CHECK ([SoilHumidity] IS NULL OR ([SoilHumidity] >= 0 AND [SoilHumidity] <= 100)),
        CONSTRAINT [CK_Measurements_Temperature] CHECK ([Temperature] >= -40 AND [Temperature] <= 80),
        CONSTRAINT [FK_Measurements_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([DeviceId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] bigint NOT NULL IDENTITY,
        [DeviceId] nvarchar(50) NULL,
        [Type] nvarchar(40) NOT NULL,
        [Severity] nvarchar(20) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [CommandId] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Events_Commands_CommandId] FOREIGN KEY ([CommandId]) REFERENCES [Commands] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Events_Devices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [Devices] ([DeviceId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CriticalTemperature', N'LightingEnabled', N'MaximumTemperature', N'MaximumWateringSeconds', N'MinimumLuminosity', N'SoilHumidityMinimum', N'SoilHumidityTarget', N'TargetLuminosity', N'TemperatureHysteresis', N'UpdatedAt', N'VentilationEnabled', N'WateringEnabled') AND [object_id] = OBJECT_ID(N'[Configurations]'))
        SET IDENTITY_INSERT [Configurations] ON;
    EXEC(N'INSERT INTO [Configurations] ([Id], [CriticalTemperature], [LightingEnabled], [MaximumTemperature], [MaximumWateringSeconds], [MinimumLuminosity], [SoilHumidityMinimum], [SoilHumidityTarget], [TargetLuminosity], [TemperatureHysteresis], [UpdatedAt], [VentilationEnabled], [WateringEnabled])
    VALUES (1, 35.0E0, CAST(1 AS bit), 30.0E0, 30, 30.0E0, 30.0E0, 45.0E0, 45.0E0, 2.0E0, ''2026-11-22T00:00:00.0000000Z'', CAST(1 AS bit), CAST(0 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CriticalTemperature', N'LightingEnabled', N'MaximumTemperature', N'MaximumWateringSeconds', N'MinimumLuminosity', N'SoilHumidityMinimum', N'SoilHumidityTarget', N'TargetLuminosity', N'TemperatureHysteresis', N'UpdatedAt', N'VentilationEnabled', N'WateringEnabled') AND [object_id] = OBJECT_ID(N'[Configurations]'))
        SET IDENTITY_INSERT [Configurations] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ActuatorStates_DeviceId_Actuator] ON [ActuatorStates] ([DeviceId], [Actuator]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Commands_DeviceId_Status_CreatedAt] ON [Commands] ([DeviceId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Events_CommandId] ON [Events] ([CommandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Events_CreatedAt] ON [Events] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Events_DeviceId_CreatedAt] ON [Events] ([DeviceId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Measurements_DeviceId_Timestamp] ON [Measurements] ([DeviceId], [Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Measurements_DeviceId_SequenceNumber] ON [Measurements] ([DeviceId], [SequenceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123001639_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123001639_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

