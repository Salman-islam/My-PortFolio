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
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
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
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
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
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [DepartmentId] int NOT NULL,
        [JobTitle] nvarchar(500) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [AnnualAllowance] int NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE TABLE [Attendance] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Day] datetime2 NOT NULL,
        [CheckIn] datetime2 NOT NULL,
        [CheckOut] datetime2 NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Attendance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Attendance_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE TABLE [Leaves] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Start] datetime2 NOT NULL,
        [End] datetime2 NOT NULL,
        [Days] int NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [Status] nvarchar(500) NOT NULL,
        [DecisionNote] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Version] bigint NOT NULL,
        CONSTRAINT [PK_Leaves] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Leaves_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Attendance_EmployeeId_Day] ON [Attendance] ([EmployeeId], [Day]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_UserId] ON [Employees] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Leaves_EmployeeId_Start] ON [Leaves] ([EmployeeId], [Start]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111040945_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111040945_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

