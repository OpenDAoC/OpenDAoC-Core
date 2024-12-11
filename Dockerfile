# ---- build ----
# Use the official .NET 9.0 SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
LABEL stage=build

# Set the working directory in the container
WORKDIR /build

# Copy the source code to the build container
COPY . .

# Install required tools and clone the database repository
RUN apt-get update && \
    apt-get install -y unzip git sed && \
    git config --global http.sslVerify false && \
    git clone https://github.com/OpenDAoC/OpenDAoC-Database.git /tmp/opendaoc-db && \
    rm -rf /var/lib/apt/lists/*

# Combine the SQL files
WORKDIR /tmp/opendaoc-db/opendaoc-db-core
RUN cat *.sql > combined.sql

# Set the working directory back to the build container
WORKDIR /build

# Copy serverconfig.example.xml to serverconfig.xml
RUN cp /build/CoreServer/config/serverconfig.example.xml /build/CoreServer/config/serverconfig.xml

# Build the application in Release mode
RUN dotnet build DOLLinux.sln -c Release

# ---- final ----
# Use the official .NET 9.0 Alpine Runtime image as the base for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
LABEL stage=final

## Install ICU libraries and su-exec
RUN apk add --no-cache icu-libs su-exec

# Set the working directory in the container
WORKDIR /app

# Copy the build output from the build stage
COPY --from=build /build/Release /app

# Copy the combined.sql file from the build stage
COPY --from=build /tmp/opendaoc-db/opendaoc-db-core/combined.sql /tmp/opendaoc-db/combined.sql

# Copy the entrypoint script
COPY --from=build /build/entrypoint.sh /app

# Make the entrypoint script executable
RUN chmod +x /app/entrypoint.sh

# Set the entrypoint
ENTRYPOINT ["/bin/sh", "/app/entrypoint.sh"]
