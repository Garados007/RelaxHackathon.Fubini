FROM ubuntu:latest
RUN apt-get update -y && \
    apt-get install -y --no-install-recommends wget curl ca-certificates && \
    wget https://packages.microsoft.com/config/ubuntu/20.10/packages-microsoft-prod.deb \
        --no-check-certificate \
        -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get install -y --no-install-recommends apt-transport-https && \
    apt-get update -y && \
    apt-get install -y --no-install-recommends dotnet-sdk-5.0 && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /usr/src/RelaxHackathon.Fubini
COPY . /usr/src/RelaxHackathon.Fubini

RUN dotnet restore && \
    dotnet build -c Release && \
    chmod +x RelaxHackathon.Fubini/bin/Release/net5.0/RelaxHackathon.Fubini

WORKDIR /app

ENTRYPOINT [ "/usr/src/RelaxHackathon.Fubini/RelaxHackathon.Fubini/bin/Release/net5.0/RelaxHackathon.Fubini" ]