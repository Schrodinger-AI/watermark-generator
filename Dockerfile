FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG servicename
WORKDIR /app
COPY ./font/*.ttf /app/font/
COPY out/$servicename .
