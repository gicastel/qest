FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /build
COPY ./sampleDb .
RUN dotnet build . -c Release -o /dacpac

FROM qest:bundle-dev

ENV DACPAC=sampleDb

WORKDIR /qest
COPY --from=build ./dacpac ./db
COPY ./scripts ./scripts
COPY ./tests ./tests