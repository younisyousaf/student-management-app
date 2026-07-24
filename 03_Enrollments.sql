--GetAll
CREATE PROCEDURE usp_Enrollment_GetAll 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Enrollments; 
END
GO

--GetById
CREATE PROCEDURE usp_Enrollment_GetById @Id INT 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Enrollments 
    WHERE Id = @Id; 
END
GO

--GetByStudentId
CREATE PROCEDURE usp_Enrollment_GetByStudentId @StudentId INT 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Enrollments 
    WHERE StudentId = @StudentId; 
END
GO

--IsAlreadyEnrolled
CREATE PROCEDURE usp_Enrollment_IsAlreadyEnrolled @StudentId INT, @CourseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CASE WHEN EXISTS (
        SELECT 1 FROM Enrollments 
        WHERE StudentId = @StudentId AND CourseId = @CourseId AND Status = 'Active'
    ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) 
    END;
END
GO

--Insert
CREATE PROCEDURE usp_Enrollment_Insert @StudentId INT, @CourseId INT, @EnrollDate DATETIME, @Status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Enrollments (StudentId, CourseId, EnrollDate, Status)
    VALUES (@StudentId, @CourseId, @EnrollDate, @Status);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

--UpdateStatus
CREATE PROCEDURE usp_Enrollment_UpdateStatus @Id INT, @Status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Enrollments SET Status = @Status 
    WHERE Id = @Id;
END
GO

--Delete
CREATE PROCEDURE usp_Enrollment_Delete @Id INT 
AS 
BEGIN 
    SET NOCOUNT ON; 
    DELETE FROM Enrollments 
    WHERE Id = @Id; 
END
GO