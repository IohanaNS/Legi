FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Legi.SharedKernel/Legi.SharedKernel.csproj src/Legi.SharedKernel/
COPY src/Legi.Identity.Domain/Legi.Identity.Domain.csproj src/Legi.Identity.Domain/
COPY src/Legi.Identity.Application/Legi.Identity.Application.csproj src/Legi.Identity.Application/
COPY src/Legi.Identity.Infrastructure/Legi.Identity.Infrastructure.csproj src/Legi.Identity.Infrastructure/
COPY src/Legi.Identity.Api/Legi.Identity.Api.csproj src/Legi.Identity.Api/
RUN dotnet restore src/Legi.Identity.Api/Legi.Identity.Api.csproj

COPY src/Legi.SharedKernel/ src/Legi.SharedKernel/
COPY src/Legi.Identity.Domain/ src/Legi.Identity.Domain/
COPY src/Legi.Identity.Application/ src/Legi.Identity.Application/
COPY src/Legi.Identity.Infrastructure/ src/Legi.Identity.Infrastructure/
COPY src/Legi.Identity.Api/ src/Legi.Identity.Api/
RUN find . -maxdepth 4 -type d -name 'bin\\*' -exec rm -rf {} + 2>/dev/null; \
    dotnet publish src/Legi.Identity.Api/Legi.Identity.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Legi.Identity.Api.dll"]
