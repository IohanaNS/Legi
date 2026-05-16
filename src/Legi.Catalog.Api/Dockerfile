FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Legi.SharedKernel/Legi.SharedKernel.csproj src/Legi.SharedKernel/
COPY src/Legi.Contracts/Legi.Contracts.csproj src/Legi.Contracts/
COPY src/Legi.Messaging/Legi.Messaging.csproj src/Legi.Messaging/
COPY src/Legi.Identity.Domain/Legi.Identity.Domain.csproj src/Legi.Identity.Domain/
COPY src/Legi.Identity.Application/Legi.Identity.Application.csproj src/Legi.Identity.Application/
COPY src/Legi.Identity.Infrastructure/Legi.Identity.Infrastructure.csproj src/Legi.Identity.Infrastructure/
COPY src/Legi.Catalog.Domain/Legi.Catalog.Domain.csproj src/Legi.Catalog.Domain/
COPY src/Legi.Catalog.Application/Legi.Catalog.Application.csproj src/Legi.Catalog.Application/
COPY src/Legi.Catalog.Infrastructure/Legi.Catalog.Infrastructure.csproj src/Legi.Catalog.Infrastructure/
COPY src/Legi.Catalog.Api/Legi.Catalog.Api.csproj src/Legi.Catalog.Api/
RUN dotnet restore src/Legi.Catalog.Api/Legi.Catalog.Api.csproj

COPY src/Legi.SharedKernel/ src/Legi.SharedKernel/
COPY src/Legi.Contracts/ src/Legi.Contracts/
COPY src/Legi.Messaging/ src/Legi.Messaging/
COPY src/Legi.Identity.Domain/ src/Legi.Identity.Domain/
COPY src/Legi.Identity.Application/ src/Legi.Identity.Application/
COPY src/Legi.Identity.Infrastructure/ src/Legi.Identity.Infrastructure/
COPY src/Legi.Catalog.Domain/ src/Legi.Catalog.Domain/
COPY src/Legi.Catalog.Application/ src/Legi.Catalog.Application/
COPY src/Legi.Catalog.Infrastructure/ src/Legi.Catalog.Infrastructure/
COPY src/Legi.Catalog.Api/ src/Legi.Catalog.Api/
RUN find . -maxdepth 4 -type d -name 'bin\\*' -exec rm -rf {} + 2>/dev/null; \
    dotnet publish src/Legi.Catalog.Api/Legi.Catalog.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Legi.Catalog.Api.dll"]
