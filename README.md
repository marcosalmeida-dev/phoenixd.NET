# phoenixd.NET

An implementation of the [Phoenixd](https://phoenix.acinq.co/server) Bitcoin/Lightning service from [ACINQ](https://acinq.co), using .NET 8 SignalR hubs for client notifications.

## Table of Contents

- [Introduction](#introduction)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Backend Setup](#backend-setup)
- [Blazor Client Sample](#blazor-client-sample)
- [Configuration](#configuration)
- [Docker Setup](#docker-setup)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgments](#acknowledgments)

## Introduction

**phoenixd.NET** is a .NET 8 implementation of the Phoenixd Bitcoin/Lightning service provided by [ACINQ](https://acinq.co). It leverages SignalR hubs to facilitate real-time client notifications, making it easier to integrate Lightning payments into your .NET applications.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started)
- A running instance of **phoenixd** (provided via Docker)

## Installation

Clone the repository:

```bash
git clone https://github.com/marcosalmeida-dev/phoenixd.NET.git
