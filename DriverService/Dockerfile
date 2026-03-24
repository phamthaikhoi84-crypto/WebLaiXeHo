# 1. Môi trường Build (Chứa SDK để biên dịch code)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file cấu trúc để tải thư viện trước (giúp build nhanh hơn)
COPY ["DriverService.API/DriverService.API.csproj", "DriverService.API/"]
COPY ["DriverService.Application/DriverService.Application.csproj", "DriverService.Application/"]
COPY ["DriverService.Domain/DriverService.Domain.csproj", "DriverService.Domain/"]
COPY ["DriverService.Infrastructure/DriverService.Infrastructure.csproj", "DriverService.Infrastructure/"]
COPY ["DriverService.Shared/DriverService.Shared.csproj", "DriverService.Shared/"]

RUN dotnet restore "DriverService.API/DriverService.API.csproj"

# Copy toàn bộ code thực tế và tiến hành đóng gói (Publish)
COPY . .
WORKDIR "/src/DriverService.API"
RUN dotnet publish "DriverService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Môi trường Runtime (Chỉ chứa lõi .NET siêu nhẹ để chạy App)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Mở cổng 8080 (Mặc định của .NET 8 trong Docker)
EXPOSE 8080
ENTRYPOINT ["dotnet", "DriverService.API.dll"]