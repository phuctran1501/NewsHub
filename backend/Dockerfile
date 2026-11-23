# ==============================================================================
# Stage 1: Build & Publish (Sử dụng .NET 10 SDK)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /app

# Copy file solution và project files trước để tận dụng Docker layer cache cho restore
COPY NewsHub.slnx ./
COPY src/NewsHub.Domain/NewsHub.Domain.csproj src/NewsHub.Domain/
COPY src/NewsHub.Application/NewsHub.Application.csproj src/NewsHub.Application/
COPY src/NewsHub.Infrastructure/NewsHub.Infrastructure.csproj src/NewsHub.Infrastructure/
COPY src/NewsHub.API/NewsHub.API.csproj src/NewsHub.API/
COPY tests/NewsHub.Domain.Tests/NewsHub.Domain.Tests.csproj tests/NewsHub.Domain.Tests/
COPY tests/NewsHub.Application.Tests/NewsHub.Application.Tests.csproj tests/NewsHub.Application.Tests/

# Restore dependencies
RUN dotnet restore NewsHub.slnx

# Copy toàn bộ mã nguồn
COPY src/ src/
COPY tests/ tests/

# Publish Web API ở cấu hình Release
RUN dotnet publish src/NewsHub.API/NewsHub.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ==============================================================================
# Stage 2: Runtime Container (Siêu nhẹ & Tối ưu RAM cho Render 512MB)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# 1. Chuyển sang Workstation GC để không cấp phát heap theo vCPU host (tránh OOM 137)
ENV DOTNET_gcServer=0

# 2. Giới hạn cứng heap bộ nhớ ở mức ~350MB (0x16000000 bytes)
ENV DOTNET_GCHeapHardLimit=0x16000000

# 3. Môi trường Production
ENV ASPNETCORE_ENVIRONMENT=Production

# 4. Bind HTTP trên cổng mặc định 5000 (sẽ được ghi đè tự động bởi $PORT trên Render)
ENV ASPNETCORE_URLS=http://+:5000

# 5. Sử dụng tài khoản phi đặc quyền 'app' có sẵn trong base image .NET
USER app

# Copy artifacts đã compile từ build stage
COPY --from=build /app/publish .

# Khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "NewsHub.API.dll"]
