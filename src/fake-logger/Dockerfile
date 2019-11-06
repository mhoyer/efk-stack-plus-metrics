FROM microsoft/dotnet:2.2-sdk AS build
    WORKDIR /app
    COPY ./*.cs* ./
    ENTRYPOINT [ "dotnet", "run", "-c", "Release" ]
