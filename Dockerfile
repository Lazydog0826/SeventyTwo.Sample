FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "."]
COPY ["src/SeventyTwo.Sample.WebApi/SeventyTwo.Sample.WebApi.csproj", "src/SeventyTwo.Sample.WebApi/"]
COPY ["src/SeventyTwo.Sample.Application/SeventyTwo.Sample.Application.csproj", "src/SeventyTwo.Sample.Application/"]
COPY ["src/SeventyTwo.Sample.Common/SeventyTwo.Sample.Common.csproj", "src/SeventyTwo.Sample.Common/"]
COPY ["src/SeventyTwo.Sample.Domain/SeventyTwo.Sample.Domain.csproj", "src/SeventyTwo.Sample.Domain/"]
COPY ["src/SeventyTwo.Sample.Infrastructure/SeventyTwo.Sample.Infrastructure.csproj", "src/SeventyTwo.Sample.Infrastructure/"]
RUN dotnet restore "src/SeventyTwo.Sample.WebApi/SeventyTwo.Sample.WebApi.csproj"

COPY src/ src/
RUN dotnet publish "src/SeventyTwo.Sample.WebApi/SeventyTwo.Sample.WebApi.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SeventyTwo.Sample.WebApi.dll"]
