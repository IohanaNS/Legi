FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Legi.SharedKernel/Legi.SharedKernel.csproj src/Legi.SharedKernel/
COPY src/Legi.Contracts/Legi.Contracts.csproj src/Legi.Contracts/
COPY src/Legi.Messaging/Legi.Messaging.csproj src/Legi.Messaging/
COPY src/Legi.Identity.Domain/Legi.Identity.Domain.csproj src/Legi.Identity.Domain/
COPY src/Legi.Identity.Application/Legi.Identity.Application.csproj src/Legi.Identity.Application/
COPY src/Legi.Identity.Infrastructure/Legi.Identity.Infrastructure.csproj src/Legi.Identity.Infrastructure/
COPY src/Legi.Catalog.Domain/Legi.Catalog.Domain.csproj src/Legi.Catalog.Domain/
COPY src/Legi.Library.Domain/Legi.Library.Domain.csproj src/Legi.Library.Domain/
COPY src/Legi.Library.Application/Legi.Library.Application.csproj src/Legi.Library.Application/
COPY src/Legi.Library.Infrastructure/Legi.Library.Infrastructure.csproj src/Legi.Library.Infrastructure/
COPY src/Legi.Library.Api/Legi.Library.Api.csproj src/Legi.Library.Api/
RUN dotnet restore src/Legi.Library.Api/Legi.Library.Api.csproj

COPY src/Legi.SharedKernel/ src/Legi.SharedKernel/
COPY src/Legi.Contracts/ src/Legi.Contracts/
COPY src/Legi.Messaging/ src/Legi.Messaging/
COPY src/Legi.Identity.Domain/ src/Legi.Identity.Domain/
COPY src/Legi.Identity.Application/ src/Legi.Identity.Application/
COPY src/Legi.Identity.Infrastructure/ src/Legi.Identity.Infrastructure/
COPY src/Legi.Catalog.Domain/ src/Legi.Catalog.Domain/
COPY src/Legi.Library.Domain/ src/Legi.Library.Domain/
COPY src/Legi.Library.Application/ src/Legi.Library.Application/
COPY src/Legi.Library.Infrastructure/ src/Legi.Library.Infrastructure/
COPY src/Legi.Library.Api/ src/Legi.Library.Api/
RUN find . -maxdepth 4 -type d -name 'bin\\*' -exec rm -rf {} + 2>/dev/null; \
    dotnet publish src/Legi.Library.Api/Legi.Library.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Legi.Library.Api.dll"]
