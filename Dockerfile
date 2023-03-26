# ---- build ----
# Use the official .NET 6.0 SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
LABEL stage=build

# Set the working directory in the container
WORKDIR /build

# Copy the source code to the build container
COPY . .

# Install unzip
RUN apt-get update && \
    apt-get install -y unzip

# Extract the DummyDB.zip file
RUN unzip DummyDB.zip -d /tmp/dummy-db

# Restore NuGet packages
RUN dotnet restore DOLLinux.sln

# Copy serverconfig.example.xml to serverconfig.xml
RUN cp /build/DOLServer/config/serverconfig.example.xml /build/DOLServer/config/serverconfig.xml

# Build the application in Release mode
RUN dotnet build DOLLinux.sln -c Release

# Use the official .NET 6.0 Runtime image as the base for the final image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
LABEL stage=final

# Install a few packages useful for debugging
RUN apt-get update && \
    apt-get upgrade -y && \
    apt-get install -y mariadb-client iproute2

# Set the working directory in the container
WORKDIR /app

# Copy the build output from the build stage
COPY --from=build /build/Release .

# Copy the dummydb.sql file from the build stage
COPY --from=build /tmp/dummy-db/DummyDB.sql /tmp/dummy-db/DummyDB.sql

# Copy the entrypoint script
COPY --from=build /build/entrypoint.sh /app

# Set the entrypoint
ENTRYPOINT ["/bin/bash", "/app/entrypoint.sh"]
