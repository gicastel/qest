FROM mcr.microsoft.com/mssql/server:2019-latest AS db
USER root

ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=sampleDbSecurePassword27!
ENV DB_NAME=sampleDb

RUN apt-get update \
    && apt-get install -y apt-transport-https \
    && apt-get update \
    && apt-get install -y dotnet-sdk-6.0

WORKDIR /sqlTests
COPY ./sampleDb ./sampleDb
COPY ./testData ./testData
COPY ./tests ./tests
COPY ./TestRunner .
RUN chmod a+x ./TestRunner


# Launch SQL Server, confirm startup is complete, deploy the DACPAC, then terminate SQL Server.
# See https://stackoverflow.com/a/51589787/488695
RUN ( /opt/mssql/bin/sqlservr & ) | grep -q "Service Broker manager has started" \
    && dotnet publish ./sampleDb /p:TargetServerName=. /p:TargetDatabaseName=$DB_NAME /p:TargetUser=sa /p:TargetPassword=$SA_PASSWORD \
    && pkill sqlservr 

# Launch SQL Server, confirm startup is complete, run the tests.
# See https://stackoverflow.com/a/51589787/488695
RUN ( /opt/mssql/bin/sqlservr & ) | grep -q "Service Broker manager has started" \
    && ./TestRunner --folder tests --tcs "Server=localhost,1433;Initial Catalog=${DB_NAME};User Id=sa;Password=${SA_PASSWORD}"