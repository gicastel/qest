# qest

## What is qest?
A simple, cross platform, command line tool to test MSSQL procedures without the needs of a SSDT project or custom procedures / assemblies.

Define your own YAML file with before / after scripts (inline or external text files), input parameters and you can verify:
- Result sets column names, column order, column types and row number
- Output parameters type and value
- Return code value
- Custom asserts in the form of a SQL query returning a scalar value

The tool does not implement a transaction logic for the tests: you have to provide your own scripts to delete the test data.
This is by design, to not interfere with the transaction logic that may be implemented in the stored procedures.

## Quickstart
### Local binary
Run tests in a single file:
```
./qest run --file relative/path/to/file.yml --tcs targetDatabaseConnectionString
```
Run all tests in a folder:
```
./qest run --folder relative/path/to/folder --tcs targetDatabaseConnectionString
```
Generate templates from the database into an output directory:
```
./qest run --folder relative/path/to/folder --tcs targetDatabaseConnectionString
```
### Local container, provided database server
Same options as the local binary version, but with a provided runtime.
```
docker run --rm -t \
    -v {full/local/path/to/test/folder}:/tests \
    -v {full/local/path/to/scripts/folder}:/scripts \
    qest:standalone \
    run --folder tests --tcs targetDatabaseConnectionString
```
or:
```
docker run --rm -t \
    -v {full/local/path/to/template/folder}:/templates \
    qest:standalone \
    generate --folder templates --tcs targetDatabaseConnectionString
```

### Bundle container
This container contains (;)) Microsoft SQL Server 2019 *and* qest executables, so you can deploy the database and run tests in a pristine environment.
You need to provide:
- the `dacpac` file of your database: the folder containing it wil be mounted on the `/quest/db` container folder
- the YAML files: the folder containing them wil be mounted on the `/quest/tests` container folder
- the scripts: the folder containing them wil be mounted on the `/quest/scripts` container folder


Please note: for this default image to work, YAML files have to reference the _File_ scripts in the `scripts/{filename}` form. See [docs](docs/YamlFormat.md#script).

Run the image binding the `tests`, `scripts` and `db` directories and providing the correct environment variables:
```
docker run --rm -t \
    -v {full/local/path/to/test/folder}:/tests \
    -v {full/local/path/to/scripts/folder}:/scripts \
    -v {full/local/path/to/dacpac/folder}:/db \
    --env DACPAC={filenameWithoutExtension} \
    ghcr.io/geims83/qest:bundle
``` 
## Samples
See [samples folder](samples/README.md).

## YAML test definition
See [docs](docs/YamlFormat.md).