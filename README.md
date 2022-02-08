# qest

## What is this?
A simple, cross platform, command line tool to test MSSQL procedures without the needs of a SSDT project or custom procedures / assemblies.

Define your own YAML file with before / after scripts (inline or external text files), input parameters and you can verify:
- Result sets column names, types and row number
- Output parameters value
- Return code
- Custom asserts in the form of a SQL query returning a scalar value

The tool does not implement a transaction logic for the tests: you have to provide your own scripts to delete the test data.
This is by design, to not interfere with the transaction logic that may be implemented in the stored procedures.

## Quickstart
### Local
Run tests in a single file:
```
./qest  --file relative/path/to/file.yml --tcs targetDatabaseConnectionString
```
Run all tests in a folder:
```
./qest --folder relative/path/to/folder --tcs targetDatabaseConnectionString
```
### Container
You need to provide the `dacpac` file of your database.
Run the image binding the `tests`, `scripts` and `db` directories and providing the correct enviroment variables:
```
docker run --rm \
    -v {full/local/path/to/test/folder}:/qest/tests \
    -v {full/local/path/to/scripts/folder}:/qest/scripts \
    -v {full/local/path/to/dacpac/folder}:/qest/db \
    --env DACPAC={filename}.dacpac \
    --env ACCEPT_MSSQL_EULA=Y \
    qest
``` 
## Samples
See [samples folder](samples/HowToRunSamples.md).

## YAML test definition
See [docs](docs/YamlFormat.md).