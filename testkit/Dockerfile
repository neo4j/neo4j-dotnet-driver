FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

RUN apt update \
	&& ln -fs /usr/share/zoneinfo/Europe/London /etc/localtime \
	&& DEBIAN_FRONTEND=noninteractive apt install -y tzdata \
	&& DEBIAN_FRONTEND=noninteractive dpkg-reconfigure --frontend noninteractive tzdata \
	&& rm -rf /var/lib/apt/lists/*

RUN apt update \
	&& apt install -y python3 \
	&& rm -rf /var/lib/apt/lists/*

ENV PYTHON=python3
ENV DOTNET_DRIVER_USING_LOCAL_SERVER=true
ENV TEST_NEO4J_USING_TESTKIT=true
ENV TK_CUSTOM_CA_PATH="/usr/local/share/custom-ca-certificates/"

# Install our own CAs on the image.
# Assumes Linux Debian based image.
COPY CAs/* /usr/local/share/ca-certificates/
RUN update-ca-certificates

COPY CustomCAs/* /usr/local/share/custom-ca-certificates/