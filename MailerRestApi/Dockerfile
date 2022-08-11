#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Marc: See the following link to build a Docker container 
# with React and ASP.NET
# https://docs.microsoft.com/en-us/visualstudio/containers/container-tools-react?view=vs-2022

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

RUN apt-get update
RUN apt-get install -y curl
RUN apt-get install -y libpng-dev libjpeg-dev curl libxi6 build-essential libgl1-mesa-glx
RUN curl -sL https://deb.nodesource.com/setup_lts.x | bash -
RUN apt-get install -y nodejs

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
RUN apt-get update
RUN apt-get install -y curl
RUN apt-get install -y libpng-dev libjpeg-dev curl libxi6 build-essential libgl1-mesa-glx
RUN curl -sL https://deb.nodesource.com/setup_lts.x | bash -
RUN apt-get install -y nodejs
WORKDIR /src
COPY ["MailerRestApi/MailerRestApi.csproj", "MailerRestApi/"]
COPY ["MailerCommon/MailerCommon.csproj", "MailerCommon/"]
RUN dotnet restore "MailerRestApi/MailerRestApi.csproj"
COPY . .
WORKDIR "/src/MailerRestApi"
RUN dotnet build "MailerRestApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MailerRestApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MailerRestApi.dll"]