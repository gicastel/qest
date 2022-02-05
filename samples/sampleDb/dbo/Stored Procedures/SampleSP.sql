CREATE PROCEDURE dbo.SampleSP
(
    @name NVARCHAR(50),
    @newValue INT,
    @oldValue INT = NULL OUTPUT
)
AS
BEGIN
    DECLARE @rc TINYINT = 1

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SampleTable
        WHERE [Name] = @name
        )
    BEGIN
        RETURN @rc
    END


    SELECT @oldValue = [Value]
    FROM dbo.SampleTable
    WHERE [Name] = @name

    UPDATE dbo.SampleTable
    SET [Value] = @newValue
    WHERE [Name] = @name
    
    SET @rc = 0

    SELECT [Name], [Value]
    FROM dbo.SampleTable
    WHERE [Name] = @name

    RETURN @rc
END