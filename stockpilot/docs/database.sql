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
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
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
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE TABLE [Parties] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [Email] nvarchar(500) NOT NULL,
        [Kind] nvarchar(500) NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Parties] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [Sku] nvarchar(500) NOT NULL,
        [Category] nvarchar(500) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Stock] int NOT NULL,
        [ReorderLevel] int NOT NULL,
        [Active] bit NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
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
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [Kind] nvarchar(500) NOT NULL,
        [PartyId] int NOT NULL,
        [Status] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [PostedAt] datetime2 NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Parties_PartyId] FOREIGN KEY ([PartyId]) REFERENCES [Parties] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE TABLE [Lines] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Lines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Lines_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Lines_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE TABLE [Movements] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [OrderId] int NULL,
        [Delta] int NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [At] datetime2 NOT NULL,
        CONSTRAINT [PK_Movements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Movements_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Movements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Lines_OrderId_ProductId] ON [Lines] ([OrderId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Lines_ProductId] ON [Lines] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Movements_OrderId] ON [Movements] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Movements_ProductId] ON [Movements] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_PartyId] ON [Orders] ([PartyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040946_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111040946_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

