FROM ubuntu:22.04
ENV TARGETARCH="linux-x64"
# Also can be "linux-arm", "linux-arm64".

# The core elements
RUN apt update
RUN apt upgrade -y
RUN apt install -y curl git jq libicu70 nano

###################################################
# Global Tools Installation

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

###################################################
# ADO Agent download and user setup and other tools
WORKDIR /azp/

# VSTest which needs to be extracted from the nuget
RUN wget -q https://www.nuget.org/api/v2/package/Microsoft.TestPlatform/17.11.1 -O /tmp/microsoft.testplatform.zip
RUN apt-get install -y unzip
RUN mkdir /tmp/microsoft.testplatform
RUN mkdir /azp/TestPlatform
RUN unzip /tmp/microsoft.testplatform.zip -d /tmp/microsoft.testplatform
RUN mv /tmp/microsoft.testplatform/tools/net462/Common7/IDE/Extensions/TestPlatform/* /azp/TestPlatform
RUN rm -rf /tmp/microsoft.testplatform && rm /tmp/microsoft.testplatform.zip

COPY ./start.sh ./
RUN chmod +x ./start.sh

# Create agent user and set up home directory
RUN useradd -m -d /home/agent agent
RUN chown -R agent:agent /azp /home/agent

USER agent
# Another option is to run the agent as root.
# ENV AGENT_ALLOW_RUNASROOT="true"

# nanoff tool installation in the azp directory
RUN mkdir /azp/tools
# RUN dotnet tool install nanoff --tool-path /azp/tools
ENV PATH="${PATH}:/azp/tools"

ENTRYPOINT [ "./start.sh" ]