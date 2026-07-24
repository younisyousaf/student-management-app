--GetAll
CREATE PROCEDURE usp_Attendance_GetAll 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Attendances; 
END
GO

--GetById
CREATE PROCEDURE usp_Attendance_GetById @Id INT 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Attendances 
    WHERE Id = @Id; 
END
GO

--GetByStudentId
CREATE PROCEDURE usp_Attendance_GetByStudentId @StudentId INT
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Attendances 
    WHERE StudentId = @StudentId 
    ORDER BY Date DESC; 
END
GO

--GetByCourseAndDate
CREATE PROCEDURE usp_Attendance_GetByCourseAndDate @CourseId INT, @Date DATE
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Attendances 
    WHERE CourseId = @CourseId AND Date = @Date; 
END
GO

--GetByStudentCourseAndDate
CREATE PROCEDURE usp_Attendance_GetByStudentCourseAndDate @StudentId INT, @CourseId INT, @Date DATE
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Attendances 
    WHERE StudentId = @StudentId AND CourseId = @CourseId AND Date = @Date; 
END
GO

--Insert
CREATE PROCEDURE usp_Attendance_Insert @StudentId INT, @CourseId INT, @Date DATE, @Status TINYINT, @Remarks NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Attendances (StudentId, CourseId, Date, Status, Remarks)
    VALUES (@StudentId, @CourseId, @Date, @Status, @Remarks);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

--Update
CREATE PROCEDURE usp_Attendance_Update @Id INT, @Status TINYINT, @Remarks NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Attendances SET Status = @Status, Remarks = @Remarks WHERE Id = @Id;
END
GO

--Delete
CREATE PROCEDURE usp_Attendance_Delete @Id INT 
AS 
BEGIN 
    SET NOCOUNT ON; 
    DELETE FROM Attendances 
    WHERE Id = @Id; 
END
GO