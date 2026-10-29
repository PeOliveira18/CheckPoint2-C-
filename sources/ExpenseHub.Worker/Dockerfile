# Build a partir da raiz do repositório:
#   docker build -f sources/ExpenseHub.Worker/Dockerfile -t expensehub-worker .
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY Directory.Build.props .editorconfig ./
COPY sources/ExpenseHub.Core/ExpenseHub.Core.csproj sources/ExpenseHub.Core/
COPY sources/ExpenseHub.Worker/ExpenseHub.Worker.csproj sources/ExpenseHub.Worker/
RUN dotnet restore sources/ExpenseHub.Worker/ExpenseHub.Worker.csproj -a $TARGETARCH
COPY sources/ExpenseHub.Core/ sources/ExpenseHub.Core/
COPY sources/ExpenseHub.Worker/ sources/ExpenseHub.Worker/
RUN dotnet publish sources/ExpenseHub.Worker/ExpenseHub.Worker.csproj -c Release -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "ExpenseHub.Worker.dll"]
