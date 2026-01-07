USE [KlaipedosVandenys]
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE PersonalCode = '40404049996')
BEGIN
    INSERT INTO dbo.Users (PersonalCode, Email, Surname, Phone)
    VALUES ('40404049996', 'test.user@example.com', 'TestSurname', '37060000000');
END
GO
