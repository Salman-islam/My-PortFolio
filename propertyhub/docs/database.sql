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
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE TABLE [Audits] (
        [Id] int NOT NULL IDENTITY,
        [ActorId] int NOT NULL,
        [Actor] nvarchar(500) NOT NULL,
        [Action] nvarchar(500) NOT NULL,
        [Detail] nvarchar(500) NOT NULL,
        [At] datetime2 NOT NULL,
        CONSTRAINT [PK_Audits] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE TABLE [Properties] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Properties] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [Email] nvarchar(500) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [Role] nvarchar(500) NOT NULL,
        [Active] bit NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE TABLE [Amenities] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [Name] nvarchar(500) NOT NULL,
        [Capacity] int NOT NULL,
        [Active] bit NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Amenities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Amenities_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE TABLE [Announcements] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [Title] nvarchar(500) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Announcements_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE TABLE [Rooms] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [Number] nvarchar(500) NOT NULL,
        [Type] nvarchar(500) NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Rooms_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE TABLE [Residents] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [PropertyId] int NOT NULL,
        [RoomId] int NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Residents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Residents_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Residents_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Residents_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE TABLE [Bookings] (
        [Id] int NOT NULL IDENTITY,
        [AmenityId] int NOT NULL,
        [ResidentId] int NOT NULL,
        [Start] datetime2 NOT NULL,
        [End] datetime2 NOT NULL,
        [Guests] int NOT NULL,
        [Purpose] nvarchar(500) NOT NULL,
        [Status] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_Amenities_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [Amenities] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Residents_ResidentId] FOREIGN KEY ([ResidentId]) REFERENCES [Residents] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Amenities_PropertyId] ON [Amenities] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Announcements_PropertyId] ON [Announcements] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_AmenityId_Start_End] ON [Bookings] ([AmenityId], [Start], [End]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_ResidentId] ON [Bookings] ([ResidentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Residents_PropertyId] ON [Residents] ([PropertyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Residents_RoomId] ON [Residents] ([RoomId]) WHERE [RoomId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Residents_UserId] ON [Residents] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Rooms_PropertyId_Number] ON [Rooms] ([PropertyId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040948_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111040948_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

