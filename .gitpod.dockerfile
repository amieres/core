FROM gitpod/workspace-dotnet

USER root

RUN apt update && apt install -qy --no-install-recommends mono-runtime libfsharp-core4.3-cil;