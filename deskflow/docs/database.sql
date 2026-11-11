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
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
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
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
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
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE TABLE [Tickets] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(500) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [Priority] nvarchar(500) NOT NULL,
        [Status] nvarchar(500) NOT NULL,
        [CategoryId] int NOT NULL,
        [RequesterId] int NOT NULL,
        [AssignedToId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Tickets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Tickets_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_Users_AssignedToId] FOREIGN KEY ([AssignedToId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Tickets_Users_RequesterId] FOREIGN KEY ([RequesterId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE TABLE [Comments] (
        [Id] int NOT NULL IDENTITY,
        [TicketId] int NOT NULL,
        [AuthorId] int NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [Internal] bit NOT NULL,
        [At] datetime2 NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comments_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Comments_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] int NOT NULL IDENTITY,
        [TicketId] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [At] datetime2 NOT NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Events_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Comments_AuthorId] ON [Comments] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Comments_TicketId] ON [Comments] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Events_TicketId] ON [Events] ([TicketId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_AssignedToId] ON [Tickets] ([AssignedToId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_CategoryId] ON [Tickets] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Tickets_RequesterId_Status] ON [Tickets] ([RequesterId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040947_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111040947_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

