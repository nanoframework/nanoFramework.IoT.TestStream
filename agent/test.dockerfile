FROM mcr.microsoft.com/dotnet/sdk:8.0

RUN mkdir /azp && mkdir /azp/tools
RUN dotnet tool install -g nbgv
RUN dotnet tool install -g nanoff