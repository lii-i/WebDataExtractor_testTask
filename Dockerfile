FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

CMD ["dotnet", "run", "--urls", "http://0.0.0.0:80"]
