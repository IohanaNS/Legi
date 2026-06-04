FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Legi.SharedKernel/Legi.SharedKernel.csproj src/Legi.SharedKernel/
COPY src/Legi.Contracts/Legi.Contracts.csproj src/Legi.Contracts/
COPY src/Legi.Messaging/Legi.Messaging.csproj src/Legi.Messaging/
COPY src/Legi.Identity.Domain/Legi.Identity.Domain.csproj src/Legi.Identity.Domain/
COPY src/Legi.Identity.Application/Legi.Identity.Application.csproj src/Legi.Identity.Application/
COPY src/Legi.Identity.Infrastructure/Legi.Identity.Infrastructure.csproj src/Legi.Identity.Infrastructure/
COPY src/Legi.Social.Domain/Legi.Social.Domain.csproj src/Legi.Social.Domain/
COPY src/Legi.Social.Application/Legi.Social.Application.csproj src/Legi.Social.Application/
COPY src/Legi.Social.Infrastructure/Legi.Social.Infrastructure.csproj src/Legi.Social.Infrastructure/
COPY src/Legi.Social.Api/Legi.Social.Api.csproj src/Legi.Social.Api/
RUN dotnet restore src/Legi.Social.Api/Legi.Social.Api.csproj

COPY src/Legi.SharedKernel/ src/Legi.SharedKernel/
COPY src/Legi.Contracts/ src/Legi.Contracts/
COPY src/Legi.Messaging/ src/Legi.Messaging/
COPY src/Legi.Identity.Domain/ src/Legi.Identity.Domain/
COPY src/Legi.Identity.Application/ src/Legi.Identity.Application/
COPY src/Legi.Identity.Infrastructure/ src/Legi.Identity.Infrastructure/
COPY src/Legi.Social.Domain/ src/Legi.Social.Domain/
COPY src/Legi.Social.Application/ src/Legi.Social.Application/
COPY src/Legi.Social.Infrastructure/ src/Legi.Social.Infrastructure/
COPY src/Legi.Social.Api/ src/Legi.Social.Api/
RUN find . -maxdepth 4 -type d -name 'bin\\*' -exec rm -rf {} + 2>/dev/null; \
    dotnet publish src/Legi.Social.Api/Legi.Social.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Legi.Social.Api.dll"]
