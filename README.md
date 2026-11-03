# OpenDAoC

[![Build and Release](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-and-release.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

An emulator for Dark Age of Camelot (DAoC) servers, focused on recreating the **1.65** experience.

Originally a fork of [DOLSharp](https://github.com/Dawn-of-Light/DOLSharp), its core has been substantially reworked for performance and scalability.

## Quick Start

The easiest way to run OpenDAoC is with Docker. Use the `docker-compose.yml` in the repository root as a starting point.

For detailed instructions and additional setup options, see the full documentation below.

## Documentation

- [OpenDAoC Documentation](https://www.opendaoc.com/docs/) *(Note: May contain outdated information)*
- [Pathing with Detour](Pathing/Detour/README.md): Navmesh generation and server-side pathfinding setup
- [Client encryption (RSA)](docs/client-encryption-rsa.md): Optional 1.127/1.130 client handshake

## Releases & Images

Prebuilt releases are on the [Releases page](https://github.com/OpenDAoC/OpenDAoC-Core/releases).

Docker images are also available:

| Registry | Image |
|---|---|
| [GitHub Container Registry](https://ghcr.io/opendaoc/opendaoc-core) (Recommended) | `ghcr.io/opendaoc/opendaoc-core/opendaoc:latest` |
| [Docker Hub](https://hub.docker.com/repository/docker/claitz/opendaoc/) | `claitz/opendaoc:latest` |

## Companion Repositories

Part of the wider [OpenDAoC project](https://github.com/OpenDAoC):

| Repository | Purpose |
|---|---|
| [OpenDAoC-Database](https://github.com/OpenDAoC/OpenDAoC-Database) | Server database schemas and content |
| [OpenDAoC-BuildNav](https://github.com/OpenDAoC/OpenDAoC-BuildNav) | Navmesh generator for pathfinding |
| [OpenDAoC-AccountManager](https://github.com/OpenDAoC/OpenDAoC-AccountManager) | Web-based account management |
| [OpenDAoC-Launcher](https://github.com/OpenDAoC/OpenDAoC-Launcher) | Custom client launcher |

## License

Licensed under the [GNU GPL v3](LICENSE) to serve the DAoC community and promote open-source development.