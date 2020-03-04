FROM gitpod/workspace-dotnet

USER root

RUN apt update && apt install -qy --no-install-recommends mono-runtime;
