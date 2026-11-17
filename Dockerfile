# Build stage: restore and publish the web project.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/QuizCiel.Web/QuizCiel.Web.csproj src/QuizCiel.Web/
RUN dotnet restore src/QuizCiel.Web/QuizCiel.Web.csproj
COPY src/ src/
RUN for attempt in 1 2 3; do \
        dotnet msbuild src/QuizCiel.Web/QuizCiel.Web.csproj -t:LibraryManagerRestore -nologo -v:minimal && break; \
        echo "LibMan restore failed (attempt $attempt), retrying..."; sleep 5; \
    done; \
    test -f src/QuizCiel.Web/wwwroot/lib/jquery/dist/jquery.min.js
RUN dotnet publish src/QuizCiel.Web/QuizCiel.Web.csproj -c Release -o /app/publish --no-restore -p:LibraryRestore=false

# Runtime stage: only the ASP.NET Core runtime and the published output, running as non-root.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "QuizCiel.Web.dll"]
