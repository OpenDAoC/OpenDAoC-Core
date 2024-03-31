# ---- build ----
# Use the official .NET 8.0 SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
LABEL stage=build

# Set the working directory in the container
WORKDIR /build

# Copy the source code to the build container
COPY . .

# Install required tools: unzip, git, and the text processing utilities we might need
RUN apt-get update && \
    apt-get install -y unzip git sed

# Clone the database repository
RUN git config --global http.sslVerify false
RUN git clone https://github.com/OpenDAoC/OpenDAoC-Database.git /tmp/opendaoc-db

# Combine the SQL files
WORKDIR /tmp/opendaoc-db/opendaoc-db-core
RUN cat *.sql > combined.sql

# Restore NuGet packages
WORKDIR /build
RUN dotnet restore DOLLinux.sln

# Copy serverconfig.example.xml to serverconfig.xml
RUN cp /build/CoreServer/config/serverconfig.example.xml /build/CoreServer/config/serverconfig.xml

# Build the application in Release mode
RUN dotnet build DOLLinux.sln -c Release

# Use the official .NET 8.0 Runtime image as the base for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
LABEL stage=final

# Install a few packages useful for debugging
RUN apt-get update && \
    apt-get upgrade -y && \
    apt-get install -y mariadb-client iproute2

# Set the working directory in the container
WORKDIR /app

# Copy the build output from the build stage
COPY --from=build /build/Release .

# Copy the combined.sql file from the build stage
COPY --from=build /tmp/opendaoc-db/opendaoc-db-core/combined.sql /tmp/opendaoc-db/combined.sql

# Copy the entrypoint script
COPY --from=build /build/entrypoint.sh /app

# Set the entrypoint
ENTRYPOINT ["/bin/bash", "/app/entrypoint.sh"]
