## How to run the provided samples

First things first, you have to build the tool.
From this directory:
```
dotnet build ..\src\TestRunner\.
dotnet publish ..\src\TestRunner\. -o .  --runtime linux-x64 -c Release --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true
```

Now you are ready to start the testing!
Just run:
```
docker build .
```
aaaaaaaaaaaand that's all folks!
Everything runs smoothly and we have now tested the DB.


## ...Wait a minute!
Ok, let's assume you want to be sure that everything is actually being tested - let's go brake things.
Go to [the stored procedure definition](sampleDb/dbo/Stored%20Procedures/SampleSP.sql) and change a little bit of logic: let's say that we want to know how many rows are updated during the operation.

At line 9, start by declaring **@rc** as 0:
```
DECLARE @rc TINYINT = 0:
```

And at line 29:
```
SET @rc = @@ROWCOUNT
```
Now build the image again: you should get an error, and a log like:
```
Running Test: SampleSP - Ok
Loading data...
Loaded data
Checking ResultSet: sampleSpRS1
Result sampleSpRS1: OK
Checking Output Parameter: oldValue
Result oldValue: 0
Checking Return Value: rc
Return Code rc: 1 != 0
Deleting data...
Deleted data
```
As you see, the first test checked the resultset - good, the output parameter - good , but the return code expected was **0** and we got a **1**.

Now: let's go to the [test definition](tests/sampleSp.yml).
As we expect to update one row when we execute the procedures with these parameters, we have to change row 30 from **0** to **1**.

But wait! We have another test in the definition!
We have to change row 56 too, from **1** to **0** (in the negative test, we don't load the row we are trying to update, so @@ROWCOUNT is expected to be 0).

Now build the image again and everything runs smoothly as previously.
