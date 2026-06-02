FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/Teryaq.API/Teryaq.API.csproj", "src/Teryaq.API/"]
COPY ["src/Teryaq.Application/Teryaq.Application.csproj", "src/Teryaq.Application/"]
COPY ["src/Teryaq.Domain/Teryaq.Domain.csproj", "src/Teryaq.Domain/"]
COPY ["src/Teryaq.Infrastructure/Teryaq.Infrastructure.csproj", "src/Teryaq.Infrastructure/"]
RUN dotnet restore "src/Teryaq.API/Teryaq.API.csproj"
COPY . .
WORKDIR "/src/src/Teryaq.API"
RUN dotnet build "Teryaq.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Teryaq.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Teryaq.API.dll"]
