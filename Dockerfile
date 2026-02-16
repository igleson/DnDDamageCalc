# Build stage - .NET 10 SDK for Native AOT compilation
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Install Native AOT prerequisites (clang, linker, build tools)
# See: https://aka.ms/nativeaot-prerequisites
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    clang \
    build-essential \
    zlib1g-dev \
    libssl-dev && \
    rm -rf /var/lib/apt/lists/*

# Copy solution and project files
COPY DnDDamageCalc.slnx .
COPY src/DnDDamageCalc.Web/DnDDamageCalc.Web.csproj src/DnDDamageCalc.Web/

# Restore dependencies
RUN dotnet restore src/DnDDamageCalc.Web/DnDDamageCalc.Web.csproj

# Copy source code
COPY src/DnDDamageCalc.Web/ src/DnDDamageCalc.Web/

# Publish with Native AOT for linux-x64
# This produces a single, self-contained native binary
RUN dotnet publish src/DnDDamageCalc.Web/DnDDamageCalc.Web.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish && \
    ls -la /app/publish

# Runtime stage - .NET 10 runtime dependencies
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS runtime
WORKDIR /app

# Use existing app user from base image (UID 1654, GID 1654)
# The runtime-deps image already includes a non-root user

# Copy published application from build stage
COPY --from=build /app/publish .

# Set ownership to app user (the default non-root user in runtime-deps image is UID 1654)
RUN chown -R 1654:1654 /app

# Switch to non-root user (app user from base image)
USER 1654

# Expose port 8080 (Fly.io's internal port)
EXPOSE 8080

# Set environment variable for ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV Logging__Console__FormatterName=Simple
ENV Logging__LogLevel__Default=Information

# Run the native AOT binary
ENTRYPOINT ["./DnDDamageCalc.Web"]
