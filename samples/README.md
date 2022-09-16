# How to run the provided samples

## Step 1 - Download this folder
Clone or download this repo.

## Step 2
### Build your local image...
Go to the root folder of the project and run:
```
docker build -f bundle.dockerfile . -t qest:bundle
```
Then, switch here in the `samples` folder and run:
```
docker build . -t qestsamples
docker run --rm -t qestsamples
```
### ...or run directly from the registry
Build the sample database:
```
dotnet build ./sampleDb/ -c Release -o ./dacpac
```

Then run the command:
```
docker run --rm -t \
    -v {full/local/path}/tests:/tests \
    -v {full/local/path}/scripts:/scripts \
    -v {full/local/path}/dacpac:/db \
    --env DACPAC={filename} \
    ghcr.io/geims83/qest:bundle
``` 

## Step 3: you're done!
The output should look like this:
``` 
Running Test: SampleSP - Ok
Running Before scripts...
Completed.
Checking ResultSet: sampleSpRS1
Result sampleSpRS1: OK
Checking Output Parameter: oldValue
Result oldValue: 0 == 0
Checking Return Code
Return Code: 0 == 0
Assert SELECT COUNT(*) FROM dbo.SampleTable WHERE [Value] = 1: 1 == 1
Running After scripts...
Completed.
Test SampleSP - Ok: OK
``` 

You have run a couple of tests on the stored procedure, and everything looks fine and green!

# ...Wait a minute!
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
Running Before scripts...
Completed.
Checking ResultSet: sampleSpRS1
Result sampleSpRS1: OK
Checking Output Parameter: oldValue
Result oldValue: 0 == 0
Checking Return Code
Return Code: 1 != 0
Assert SELECT COUNT(*) FROM dbo.SampleTable WHERE [IntValue] = 1: 1 == 1
Running After scripts...
Completed.
Test SampleSP - Ok: KO
```
As you see, the first test checked the resultset - good, the output parameter - good , but the return code expected was **0** and we got a **1**.

Now: let's go to the [test definition](tests/sampleSp.yml).<br>
As we expect to update one row when we execute the procedures with these parameters, we have to change row 28 from **0** to **1**.<br>
But wait! We have another test in the definition!<br>
We have to change row 52 too, from **1** to **0** (in the negative test, we don't load the row we are trying to update, so @@ROWCOUNT is expected to be 0).

Now build / run the image again and everything runs smoothly as previously.