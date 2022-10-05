FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

LABEL org.opencontainers.image.source=https://github.com/geims83/qest

COPY ./src ./src

RUN dotnet restore /src -r linux-x64

RUN dotnet publish /src/qest/ -o /output --runtime linux-x64 -c Release --self-contained false --no-restore

FROM mcr.microsoft.com/dotnet/runtime:6.0

WORKDIR /app
COPY --from=build /output/ .

ENV PATH="${PATH}:/app"

WORKDIR /

ENTRYPOINT [ "qest" ]