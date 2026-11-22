# Aiursoft PipelineWatchDog

<a name="pipelinewatchdog"></a>

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AiursoftWeb/pipelinewatchdog/blob/master/LICENSE)
[![Pipeline stat](https://badge-proxy.aiursoft.com/aiursoft/pipelinewatchdog/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/pipelinewatchdog/-/pipelines)
[![Test Coverage](https://badge-proxy.aiursoft.com/aiursoft/pipelinewatchdog/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/pipelinewatchdog/-/pipelines)
[![S360 compliance](https://s360.aiursoft.com/badge.svg?project=aiursoft%2Fpipelinewatchdog)](https://s360.aiursoft.com/SupplyChain?tab=projects&project=aiursoft%2Fpipelinewatchdog&scope=baseline&ecosystem=all)
[![NuGet version (Aiursoft.PipelineWatchDog)](https://img.shields.io/nuget/v/Aiursoft.PipelineWatchDog.svg)](https://www.nuget.org/packages/Aiursoft.PipelineWatchDog/)
[![Man hours](https://manhours.aiursoft.com/r/github.com/aiursoftweb/pipelinewatchdog.svg)](https://manhours.aiursoft.com/r/github.com/aiursoftweb/pipelinewatchdog.html)

Monitors GitLab pipelines and stalled NugetNinja updates, reporting prolonged failures to AlertCenter.

## Overview

### What is PipelineWatchDog?

Monitor default-branch GitLab pipeline failures and stalled NugetNinja updates for selected groups, and report prolonged failures to AlertCenter. This is a long-running monitoring tool, not an interactive command dispatcher.

## Installation

Install the CLI with the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0):

```bash
dotnet tool install --global Aiursoft.PipelineWatchDog
```

Update an existing installation:

```bash
dotnet tool update --global Aiursoft.PipelineWatchDog
```

Remove the CLI:

```bash
dotnet tool uninstall --global Aiursoft.PipelineWatchDog
```

**Deployment with Docker**

The image is built from this repository and runs the same monitoring process. Put the variables from [Configuration](#configuration) in a private environment file, then run one replica:

```bash
docker run -d --name pipeline-watchdog \
  --env-file pipeline-watchdog.env \
  -v pipeline-watchdog-data:/data \
  aiursoft/pipelinewatchdog:latest
```

The container runs as UID 1654. Keep `/data` writable and persistent across replacements so credential rotation can resume. To check the last successful scan:

```bash
docker exec pipeline-watchdog dotnet /app/pipeline-watchdog.dll --healthcheck
```

## Usage

Set the environment variables in [Configuration](#configuration), then run:

```bash
pipeline-watchdog
```

Run one replica. The process continues scanning until it is stopped; no repository CI changes are needed.

### Monitoring behavior

The watchdog monitors default-branch pipelines in the selected GitLab groups. It reports a new failure on the next scan at Sev4. Failures aged 3 hours or more are Sev3, and those aged 8 hours or more are Sev2. Escalation updates the same incident. A successful pipeline automatically mitigates it. Also monitors stalled NugetNinja compliance per repository: Sev3 after 24 hours, upgrading the same incident to Sev2 after 7 days. A subsequent successful NugetNinja MR merge mitigates the repository’s incident, even when it replaces a deleted MR. Closing or deleting a request alone does not recover it. No repository CI changes are needed.

The scanner waits one minute between scans. Discovery and reporting failures are logged without including credentials; unsuccessful scans do not refresh the health marker.

### Health check

To check the last successful scan, run:

```bash
pipeline-watchdog --healthcheck
```

Run this in the same environment as the monitored process. It exits with `0` when `/tmp/pipeline-bridge-health` exists and was updated within the last 15 minutes, otherwise `1`. This checks the scan marker; it does not start a scan or contact GitLab.

## Configuration

Run one replica with `GITLAB_URL`, `ALERTCENTER_URL`, `GITLAB_GROUPS` (comma-separated group paths), `GITLAB_ACCESS_TOKEN` and `ALERTCENTER_API_KEY`. Use a dedicated GitLab personal access token with `read_api` and `self_rotate`; it rotates daily. AlertCenter keys are managed separately.

Mount writable persistent storage at `/data` for UID 1654 (override with `TOKEN_DIRECTORY`). Saved tokens take precedence over the initial environment token. After an interrupted rotation, stop the service, supply a fresh token and remove `gitlab-token.json` before restarting. Pipeline history remains authoritative; the volume stores credentials only.

**Environment variables**

| Variable | Meaning | Default |
| --- | --- | --- |
| `GITLAB_URL` | GitLab HTTP(S) endpoint, without embedded credentials, query or fragment | Required |
| `GITLAB_GROUPS` | Comma-separated exact group paths; duplicates are removed | Required |
| `GITLAB_ACCESS_TOKEN` | Initial dedicated GitLab PAT with `read_api` and `self_rotate` | Required |
| `ALERTCENTER_URL` | AlertCenter HTTP(S) endpoint, without embedded credentials, query or fragment | Required |
| `ALERTCENTER_API_KEY` | AlertCenter API key | Required |
| `TOKEN_DIRECTORY` | Writable directory for the persisted rotating GitLab credential | `/data` |

Supply secrets through your process or deployment environment. Do not commit real tokens. Persist the token directory across process or container replacement; a saved token takes precedence over the initial environment token.

## Development

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

From the repository root, restore and build:

```bash
dotnet restore Aiursoft.PipelineWatchDog.sln
dotnet build Aiursoft.PipelineWatchDog.sln --no-restore
```

<a name="run-locally"></a>
Run the tool from source, with the same environment variables configured:

```bash
dotnet run --project src/Aiursoft.PipelineWatchDog/Aiursoft.PipelineWatchDog.csproj
```

<a name="run-in-microsoft-visual-studio"></a>
In Microsoft Visual Studio, open `Aiursoft.PipelineWatchDog.sln`, select `Aiursoft.PipelineWatchDog` as the startup project and press `F5`. Configure the same environment variables for the process.

Run the existing tests:

```bash
dotnet test Aiursoft.PipelineWatchDog.sln --no-restore
```

## Documentation

- [Configuration guide](https://github.com/AiursoftWeb/pipelinewatchdog/blob/master/docs/configuration.md)

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you have push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.

## License

Licensed under the [MIT License](https://github.com/AiursoftWeb/pipelinewatchdog/blob/master/LICENSE).
