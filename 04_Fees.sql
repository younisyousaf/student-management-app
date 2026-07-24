--GetAll
CREATE PROCEDURE usp_Fee_GetAll 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Fees; 
END
GO

--GetById
CREATE PROCEDURE usp_Fee_GetById @Id INT 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Fees 
    WHERE Id = @Id; 
END
GO

--GetByStudentAndCourse
CREATE PROCEDURE usp_Fee_GetByStudentAndCourse @StudentId INT, @CourseId INT
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Fees 
    WHERE StudentId = @StudentId AND CourseId = @CourseId; 
END
GO

--Insert
CREATE PROCEDURE usp_Fee_Insert @StudentId INT, @CourseId INT, @AmountDue DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Fees (StudentId, CourseId, AmountDue, AmountPaid, PaymentDate, Remarks)
    VALUES (@StudentId, @CourseId, @AmountDue, 0, NULL, NULL);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

--Update
CREATE PROCEDURE usp_Fee_Update @Id INT, @AmountPaid DECIMAL(10,2), @PaymentDate DATETIME, @Remarks NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Fees SET AmountPaid = @AmountPaid, PaymentDate = @PaymentDate, Remarks = @Remarks 
    WHERE Id = @Id;
END
GO