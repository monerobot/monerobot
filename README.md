# MoneroBot ![Build Passing](https://github.com/HennyH/monerobot/actions/workflows/ci.yml/badge.svg)

Your friendly neighbourhood monero bounty bot!

## Overview

This a C# re-implementation of the [original](https://github.com/t-900-a/fider-monero-bot) monero bounty bot which used with
the [Fider](https://fider.io/) frontend to power [bounties.monero.social](https://bounties.monero.social/). This C# implementation was
made because the existing code stopped working and is no longer maintained.

## Featues

- A single executable whose behaviour can be configured easily by editing the `appsettings.json` file.
- Stable donation address selection for posts which allows posts to be re-imported and retain their donation address.
- Donation scanning updates comments depending on their status (unconfirmed, confirmed, sent).
- Extensive structured logging used in the background services to simplify deployment maintenance.
- More robust error handling and tolerance of faults in the consumed Fider API and Wallet RPC.
- Supports configuring the account number used for donation addresses.
- Automatically labels subaddresses in the wallet for ease of use.
