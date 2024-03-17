FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY out .
COPY ./font/*.ttf /app/font/
ENTRYPOINT ["dotnet", "WatermarkGenerator.dll"]
