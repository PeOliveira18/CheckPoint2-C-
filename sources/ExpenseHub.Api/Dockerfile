# Build a partir da raiz do repositório:
#   docker build -f sources/ExpenseHub.Api/Dockerfile -t expensehub-api .
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY Directory.Build.props .editorconfig ./
COPY sources/ExpenseHub.Core/ExpenseHub.Core.csproj sources/ExpenseHub.Core/
COPY sources/ExpenseHub.Api/ExpenseHub.Api.csproj sources/ExpenseHub.Api/
RUN dotnet restore sources/ExpenseHub.Api/ExpenseHub.Api.csproj -a $TARGETARCH
COPY sources/ExpenseHub.Core/ sources/ExpenseHub.Core/
COPY sources/ExpenseHub.Api/ sources/ExpenseHub.Api/
RUN dotnet publish sources/ExpenseHub.Api/ExpenseHub.Api.csproj -c Release -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ExpenseHub.Api.dll"]
