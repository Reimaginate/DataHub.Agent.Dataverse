# Licensing

## Agent source and packages

Reimaginate Pty Ltd licenses all Reimaginate-authored source in this repository
and the corresponding official Agent package contents under the
[MIT License](LICENSE).

For clarity, this is an express separate MIT licence grant for the copies
distributed from this Agent repository and its official packages, including
any portions that also appear in, or were adapted from, Reimaginate DataHub.
It does not change the licence that applies to copies distributed as part of
DataHub.

## DataHub dependencies

The Dataverse runtime communicates with DataHub through separately distributed
DataHub client and shared-contract packages. The Dataverse test-framework
package additionally provides optional in-process DataHub integration-test
hosting and therefore depends on the DataHub runtime and test framework.

Those dependencies retain the
[DataHub Business Source License 1.1](https://github.com/Reimaginate/DataHub/blob/v1.4.0/LICENSE).
The Agent's MIT licence does not relicense them. Full DataHub runtime APIs are
used only by source under the `IntegrationTesting` namespace and directory.

The exact restored dependency inventory and applicable licences are recorded in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
