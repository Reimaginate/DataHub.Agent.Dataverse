# DataHub Agent for Dataverse

This repository contains the buildable public source snapshot for version
1.4.2 of the Reimaginate DataHub Dataverse agent packages:

- `Reimaginate.DataHub.Agent.Dataverse`
- `Reimaginate.DataHub.Agent.TestFramework.Dataverse`

Release tags identify the source corresponding to published NuGet packages.
Shared agent functionality is consumed from the separately versioned
[`Reimaginate.DataHub.Agent`](https://github.com/Reimaginate/DataHub.Agent)
packages.

## Package migration

The current package IDs replace these legacy identities:

- `Reimaginate.DataHub.D365Agent` → `Reimaginate.DataHub.Agent.Dataverse`
- `Reimaginate.DataHub.TestFramework.Agents.D365CE` →
  `Reimaginate.DataHub.Agent.TestFramework.Dataverse`

## Build and test

Install the .NET 10 SDK, then run:

```powershell
dotnet restore .\Reimaginate.DataHub.Agent.Dataverse.slnx
dotnet build .\Reimaginate.DataHub.Agent.Dataverse.slnx --configuration Release
dotnet test .\Reimaginate.DataHub.Agent.Dataverse.slnx --configuration Release
```

## Vendor-maintained source

This project is maintained and released by Reimaginate. Its source code is
provided under the MIT License to support transparency, debugging,
customisation, and customer assurance.

The MIT License permits customers to inspect, modify, and redistribute this
software. Reimaginate supports only official, unmodified builds and releases
unless otherwise agreed under a commercial support arrangement.

External pull requests are not accepted. Reproducible bug reports and feature
requests are welcome through the structured GitHub issue forms, but GitHub
Issues are not a support channel and do not carry a response or implementation
commitment. Report security vulnerabilities privately as described in
[SECURITY.md](SECURITY.md).

Customers may send reproduction details or proposed patches through
[support@reimaginate.online](mailto:support@reimaginate.online). Reimaginate
decides whether to independently incorporate suggested changes.

See [SUPPORT.md](SUPPORT.md) for the support policy.

## License

The Agent source and official Agent packages are licensed under the
[MIT License](LICENSE), including Agent copies of code that may also appear in
DataHub. DataHub dependencies remain separately licensed under DataHub's
Business Source License 1.1. Full DataHub hosting is confined to the optional
integration-test support in the test-framework package. See
[LICENSING.md](LICENSING.md) and
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
