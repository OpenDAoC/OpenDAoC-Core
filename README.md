# OpenDAoC
[![Build and Release](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-and-release.yml)

## About

OpenDAoC is an emulator for Dark Age of Camelot (DAoC) servers, originally a fork of the [DOLSharp](https://github.com/Dawn-of-Light/DOLSharp) project.

Now completely rewritten with ECS architecture, OpenDAoC ensures performance and scalability for many players, providing a robust platform for creating and managing DAoC servers.

While the project focuses on recreating the DAoC 1.65 experience, it can be adapted for any patch level.

## Documentation

The easiest way to get started with OpenDAoC is to use Docker. Check out the `docker-compose.yml` file in the repository root for an example setup.

For detailed instructions and additional setup options, refer to the full [OpenDAoC Documentation](https://www.opendaoc.com/docs/).

## Releases

Releases for OpenDAoC are available at [OpenDAoC Releases](https://github.com/OpenDAoC/OpenDAoC-Core/releases).

OpenDAoC is also available as a Docker image, which can be pulled from the following registries:

- [GitHub Container Registry](https://ghcr.io/opendaoc/opendaoc-core) (recommended): `ghcr.io/opendaoc/opendaoc-core/opendaoc:latest`
- [Docker Hub](https://hub.docker.com/repository/docker/claitz/opendaoc/): `claitz/opendaoc:latest`

For detailed instructions and additional setup options, refer to the documentation.

## Companion Repositories

Several companion repositories are part of the [OpenDAoC project](https://github.com/OpenDAoC).

Some of the main repositories include:

- [OpenDAoC Database v1.65](https://github.com/OpenDAoC/OpenDAoC-Database)
- [Account Manager](https://github.com/OpenDAoC/opendaoc-accountmanager)
- [Client Launcher](https://github.com/OpenDAoC/OpenDAoC-Launcher)

## License

OpenDAoC is licensed under the [GNU General Public License (GPL)](https://choosealicense.com/licenses/gpl-3.0/) v3 to serve the DAoC community and promote open-source development.  
See the [LICENSE](LICENSE) file for more details.
