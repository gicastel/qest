FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

WORKDIR /build
COPY ./sampleDb .
RUN dotnet build . -c Release -o /dacpac

FROM ghcr.io/geims83/qest:latest

ENV DACPAC=sampleDb

WORKDIR /qest
COPY --from=build ./dacpac ./db
COPY ./scripts ./scripts
COPY ./tests ./tests