-- Create database if not exists
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'MRBSystemDb')
BEGIN
    CREATE DATABASE [MRBSystemDb];
END
GO

USE [MRBSystemDb];
GO
