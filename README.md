# DbTester

## What is this?
A simple, cross platform, command line tool to test MSSQL procedures without the needs of a SSDT project or custom procedures / assemblies.

Define your own YAML file with custom before / after scripts (inline or external text files), your input parameters and you can verify:
- Result sets column types and row number
- Output parameters value
- Return code
- Custom asserts in the form of a SQL query returning a scalar value

The tool does not implement a transaction logic for the tests: you have to provide your own scripts to delete the test data.
This is by design, to not interfere with the transaction logic that may be implemented in the stored procedures.

## Quickstart

Run tests in a single file:
```
./TestRunner  --file relative/path/to/file.yml --tcs targetDatabaseConnectionString
```

Run all tests in a folder:
```
./TestRunner --folder relative/path/to/folder --tcs targetDatabaseConnectionString
```
  
## Samples
See [samples folder](samples/Readme.md).
