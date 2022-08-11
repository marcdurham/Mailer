#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.


FROM node AS node-builder
# set working directory
##RUN mkdir /usr/src/app
##WORKDIR /usr/src/app
WORKDIR /app
# add `/usr/src/app/node_modules/.bin` to $PATH
#ENV PATH /usr/src/app/node_modules/.bin:$PATH
ENV PATH /app/node_modules/.bin:$PATH

# install and cache app dependencies
COPY ScheduleViewer/ClientApp/package.json /app/package.json
COPY ScheduleViewer/ClientApp/package-lock.json /app/package-lock.json
RUN npm install
#RUN npm install -g @angular/cli@1.7.0 --unsafe

#WORKDIR /node
#COPY ./ScheduleViewer/ClientApp /node
#RUN npm install
#RUN npm build

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ScheduleViewer/ScheduleViewer.csproj", "ScheduleViewer/"]
RUN dotnet restore "ScheduleViewer/ScheduleViewer.csproj"
COPY . .
WORKDIR "/src/ScheduleViewer"
RUN dotnet build "ScheduleViewer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ScheduleViewer.csproj" -c Release -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=node-builder /node/build ./ScheduleViewer/wwwroot
#ENTRYPOINT ["dotnet", "ScheduleViewer.dll"]

CMD ['npm', 'start']