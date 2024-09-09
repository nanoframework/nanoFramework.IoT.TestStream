FROM ubuntu:22.04
ENV TARGETARCH="linux-x64"
# Also can be "linux-arm", "linux-arm64".

# The core elements
RUN apt update
RUN apt upgrade -y
RUN apt install -y curl git jq libicu70 

###################################
# Tools installation

# Powershell
RUN apt install -y wget apt-transport-https software-properties-common
# Download the Microsoft repository keys
RUN wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
# Register the Microsoft repository keys
RUN dpkg -i packages-microsoft-prod.deb
# Delete the Microsoft repository keys file
RUN rm packages-microsoft-prod.deb
# Update the list of packages after we added packages.microsoft.com
RUN apt-get update
# Install PowerShell
RUN apt-get install -y powershell

# dotnet SDK 8.0 needed for nanoff
RUN apt-get install -y dotnet-sdk-8.0

# mono-complete needed to run the test and build nano in case
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get install -y -q dirmngr gnupg apt-transport-https ca-certificates software-properties-common mono-complete

# VSTest which needs to be extracted from the nuget
RUN wget -q https://www.nuget.org/api/v2/package/Microsoft.TestPlatform/17.11.1 -O microsoft.testplatform.zip
RUN apt-get install -y unzip
RUN mkdir /usr/bin/microsoft.testplatform
RUN unzip microsoft.testplatform.zip -d /usr/bin/microsoft.testplatform

#RUN dotnet tool install -g nanoff
RUN wget https://api.nuget.org/v3-flatcontainer/nanoff/2.5.90/nanoff.2.5.90.nupkg -O nanoff.2.5.90.nupkg
RUN dotnet tool install -g --add-source ./nanoff.2.5.90.nupkg nanoff
ENV PATH="${PATH}:/root/.dotnet/tools"

###################################
# ADO Agent download and user setup
WORKDIR /azp/

COPY ./start.sh ./
RUN chmod +x ./start.sh

# Create agent user and set up home directory
RUN useradd -m -d /home/agent agent
RUN chown -R agent:agent /azp /home/agent

USER agent
# Another option is to run the agent as root.
# ENV AGENT_ALLOW_RUNASROOT="true"

# Install nanoff as dotnet tool in user context and add it to the path
#RUN mkdir /home/agent/.dotnet
#RUN mkdir /home/agent/.dotnet/tools

#ENV PATH="${PATH}:/home/agent/.dotnet/tools"

ENTRYPOINT [ "./start.sh" ]