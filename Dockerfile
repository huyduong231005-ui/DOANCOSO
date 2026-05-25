# ─────────────────────────────────────────────────────────────────────
# Stage 1: build & publish ASP.NET Core app (.NET 10)
# ─────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore deps first (tận dụng layer cache khi chỉ code thay đổi)
COPY t/t.csproj t/
RUN dotnet restore t/t.csproj

# Copy phần còn lại của project và publish
COPY t/ t/
RUN dotnet publish t/t.csproj -c Release -o /app/publish /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────
# Stage 2: runtime (image nhẹ hơn nhiều)
# ─────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render set biến PORT, app sẽ tự bind theo
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Render tự gán PORT. Local fallback = 8080.
EXPOSE 8080
ENV PORT=8080

ENTRYPOINT ["dotnet", "t.dll"]
