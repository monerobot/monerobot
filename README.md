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
- Allows Fider API requests to use an impersonated user.

## Usage

Build/download the monero bounty bot daemon binaries appropriate to your platform and then modify the `appsettings.json` file as
appropriate. The daemon program itself expects you to have configured/have running the following APIs/services:

1. A [Fider](https://fider.io/) instance.
2. A [Monero Wallet RPC Server](https://www.getmonero.org/resources/developer-guides/wallet-rpc.html) instance which was launched with the `--wallet-file` argument pointing
to the managed donation wallet.

## Configuration

The `appsettings.json` file can be modified and placed alongside the executable to configure the daemon. The
following settings are available:

```jsonc
{
  "ConnectionStrings": {
    // The sqlite database in which the bot's state is stored.
    "MoneroBotContext": "Data Source=C:/Users/Henry/Databases/MoneroBot.db;Foreign Keys=true;"
  },
  // Settings related to the daemon program itself.
  "DaemonOptions": {
    // The account index to use when registering new bounties.
    "WalletAccountIndex": 0,
    // The delay which background processes wait for before starting the
    // background action again.
    "PollingInterval": "00:00:05"
  },
  "Fider": {
    // The Fider API key to use when creating or updating posts/comments.
    "ApiKey": "wcu2gmm7WrPqITcR8fmEXidcvQS6oFJFfnL4maIWmFjmEgaW7rOIJgAZoXcXNsJ1",
    // The address where the Fider instance is accessible.
    "BaseAddress": "http://localhost:3000",
    // If set the value will be used to set the X-Fider-UserID header allowing
    // the user to be impersonated.
    "ImpersonationUserId": null
  },
  "MoneroWalletRpc": {
    // The address where a running instance of the Monero Wallet RPC server is accessible.
    "BaseAddress": "https://localhost:28083",
    // If provided configures the username to use when logging into the RPC server.
    "RpcUsername": "user",
    // Can be provided along with `RpcUsername` when logging into the RPC server requires
    // a password.
    "RpcPassword": "password",
    // Configures whether the daemon program should accept self-signed HTTPS certificates
    // from the Monero Wallet RPC server.
    "AcceptSelfSignedCerts": true
  }
}
```
