CREATE PROCEDURE usp_Course_GetAll 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Courses; 
END
GO


CREATE PROCEDURE usp_Course_GetById @Id INT 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Courses 
    WHERE Id = @Id; 
END
GO

CREATE PROCEDURE usp_Course_GetByCode @Code NVARCHAR(20) 
AS 
BEGIN 
    SET NOCOUNT ON; 
    SELECT * FROM Courses 
    WHERE Code = @Code; 
END
GO

CREATE PROCEDURE usp_Course_Insert
    @Code NVARCHAR(20), @Name NVARCHAR(150), @Description NVARCHAR(500) = NULL,
    @DurationMonths INT, @FeeAmount DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Courses (Code, Name, Description, DurationMonths, FeeAmount)
    VALUES (@Code, @Name, @Description, @DurationMonths, @FeeAmount);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

CREATE PROCEDURE usp_Course_Update
    @Id INT, @Name NVARCHAR(150), @Description NVARCHAR(500) = NULL,
    @DurationMonths INT, @FeeAmount DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Courses
    SET Name = @Name, Description = @Description, DurationMonths = @DurationMonths, FeeAmount = @FeeAmount
    WHERE Id = @Id;
END
GO

CREATE PROCEDURE usp_Course_Delete @Id INT 
AS 
BEGIN 
    SET NOCOUNT ON; 
    DELETE FROM Courses 
    WHERE Id = @Id; 
END
GO