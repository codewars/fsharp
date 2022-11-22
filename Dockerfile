FROM mcr.microsoft.com/dotnet/sdk:6.0

RUN set -ex; \
    useradd --create-home -u 9999 codewarrior; \
    mkdir -p /workspace; \
    chown codewarrior:codewarrior /workspace;

COPY --chown=codewarrior:codewarrior workspace /workspace

RUN set -ex; \
    echo "#!/bin/sh" > /usr/bin/fsc; \
    echo "dotnet /usr/share/dotnet/sdk/$(dotnet --version)/FSharp/fsc.dll \$@" >> /usr/bin/fsc; \
    chmod +x /usr/bin/fsc; \
    mkdir -p /opt/nuget/packages; \
    mkdir -p /opt/nuget/cache; \
    chmod -R o+rw /opt/nuget;

USER codewarrior
ENV USER=codewarrior \
    HOME=/home/codewarrior \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_PACKAGES=/opt/nuget/packages \
    NUGET_HTTP_CACHE_PATH=/opt/nuget/cache

RUN set -ex; \
    cd /workspace; \
    dotnet restore; \
# Copy all the necessary files to bin/
    dotnet build --no-restore; \
# Remove obj/ to get the verbose output to extract reference paths
    rm -rf bin/Debug/net6.0/run.pdb obj; \
# Append reference paths to `cnfig.rsp`
    dotnet run --verbosity normal | grep '\-r:' >> config.rsp; \
# Sanity check
    fsc @config.rsp Preloaded.fs Solution.fs Tests.fs Program.fs; \
    dotnet bin/Debug/net6.0/run.dll; \
# Remove examples
    rm Preloaded.fs Solution.fs Tests.fs bin/Debug/net6.0/run.dll;

WORKDIR /workspace
