USE [KlaipedosVandenys]
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users]
    (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PersonalCode] NVARCHAR(50) NOT NULL,
        [Email] NVARCHAR(256) NOT NULL,
        [Surname] NVARCHAR(100) NOT NULL,
        [Phone] NVARCHAR(50) NULL
    );

    CREATE INDEX [IX_Users_PersonalCode] ON [dbo].[Users]([PersonalCode]);
    CREATE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
    CREATE INDEX [IX_Users_Surname] ON [dbo].[Users]([Surname]);
    CREATE INDEX [IX_Users_Phone] ON [dbo].[Users]([Phone]);
END
GO
