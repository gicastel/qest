INSERT INTO dbo.SampleTable (
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
        [Time])
VALUES (
        'SampleName',
        1,
        42,
        7777,
        0,
        1210000000,
        3.14159265,
        1.1235813,
        21.22, --deci
        0.00, --money
        '1985-10-26 09:00:00',
        '2015-10-21 07:28:00',
        '1955-11-12 06:38:00',
        '1900-01-01',
        '09:40:00'
);