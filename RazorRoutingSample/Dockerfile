FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY RazorRoutingSample.sln ./
COPY RazorRoutingSample/RazorRoutingSample.csproj RazorRoutingSample/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/RazorRoutingSample
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RazorRoutingSample.dll"]
