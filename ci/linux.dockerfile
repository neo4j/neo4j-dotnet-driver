FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch

RUN apt-get update \
	&& apt-get install -y bash python3 python3-pip curl gnupg apt-transport-https openjdk-8-jdk \
	&& echo 'deb http://ftp.debian.org/debian stretch-backports main' | tee /etc/apt/sources.list.d/stretch-backports.list \
	&& curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - \
	&& echo 'deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-stretch-prod stretch main' > /etc/apt/sources.list.d/microsoft.list \
	&& apt-get update \
	&& apt-get install -y powershell openjdk-11-jdk \
	&& python3 -m pip install boltkit==1.2.0 \
	&& mkdir /java \
	&& rm -rf /var/lib/apt/lists/* \
	&& ln -s /usr/lib/jvm/java-11-openjdk-amd64 /java/jdk-4.0 \
    && ln -s /usr/lib/jvm/java-8-openjdk-amd64 /java/jdk-3.5 \
    && ln -s /usr/lib/jvm/java-8-openjdk-amd64 /java/jdk-3.4 \
    && ln -s /usr/lib/jvm/java-8-openjdk-amd64 /java/jdk-3.3 \
    && ln -s /usr/lib/jvm/java-8-openjdk-amd64 /java/jdk-3.2
ENV NEOCTRLARGS="-e 4.0"
ENV TEAMCITY_HOST="" TEAMCITY_USER="" TEAMCITY_PASSWORD="" TEAMCITY_PROJECT_NAME=""

ADD . /dotnet-driver
WORKDIR /dotnet-driver

CMD PYTHON=python3 JAVA_HOME=/java/jdk-`echo $NEOCTRLARGS | sed -E 's/-e\s*//g' | cut -d. -f1,2` pwsh -f ./Neo4j.Driver/runTests.ps1