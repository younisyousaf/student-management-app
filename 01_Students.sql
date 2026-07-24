CREATE PROCEDURE usp_Student_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Students;
END
GO

CREATE PROCEDURE usp_Student_GetById @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Students WHERE Id = @Id;
END
GO

CREATE PROCEDURE usp_Student_GetByRollNumber @RollNumber NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Students WHERE RollNumber = @RollNumber;
END
GO

CREATE PROCEDURE usp_Student_GetByEmail @Email NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Students WHERE Email = @Email;
END
GO

CREATE PROCEDURE usp_Student_Insert
    @RollNumber NVARCHAR(50), @FirstName NVARCHAR(100), @LastName NVARCHAR(100),
    @Email NVARCHAR(100), @Phone NVARCHAR(20) = NULL, @Address NVARCHAR(255) = NULL,
    @DateOfBirth DATE, @AdmissionDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Students (RollNumber, FirstName, LastName, Email, Phone, Address, DateOfBirth, AdmissionDate)
    VALUES (@RollNumber, @FirstName, @LastName, @Email, @Phone, @Address, @DateOfBirth, @AdmissionDate);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

CREATE PROCEDURE usp_Student_Update
    @Id INT, @FirstName NVARCHAR(100), @LastName NVARCHAR(100),
    @Email NVARCHAR(100), @Phone NVARCHAR(20) = NULL, @Address NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Students
    SET FirstName = @FirstName, LastName = @LastName, Email = @Email, Phone = @Phone, Address = @Address
    WHERE Id = @Id;
END
GO

CREATE PROCEDURE usp_Student_Delete @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM Students WHERE Id = @Id;
END
GO