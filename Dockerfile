# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all source files and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render expects web services to listen on port 10000 by default
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

# Replace with your actual compiled .dll assembly filename if different
ENTRYPOINT ["dotnet", "Live Code Share.dll"]