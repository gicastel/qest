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


    SELECT @oldValue = [IntValue]
    FROM dbo.SampleTable
    WHERE [Name] = @name

    UPDATE dbo.SampleTable
    SET [IntValue] = @newValue
    WHERE [Name] = @name
    
    SET @rc = 0

    SELECT    
        [Name],
        [BitValue],
        [TinyIntValue],
        [SmallintValue],
        [IntValue],
        [BigIntValue],
        [FloatValue],
        [RealValue],
        [DecimalValue],
        [MoneyValue],
        [DateTimeValue],
        [DateTime2Value],
        [DateTimeOffsetValue],
        [DateValue],
        [Time],
        NULL AS [NullValue]
    FROM dbo.SampleTable
    WHERE [Name] = @name

    RETURN @rc
END