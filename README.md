# MoneroBot ![Build Passing](https://github.com/HennyH/monerobot/actions/workflows/ci.yml/badge.svg)

Your friendly neighbourhood monero bounty bot!

## Overview

This a C# re-implementation of the [original](https://github.com/t-900-a/fider-monero-bot) monero bounty bot which used with
the [Fider](https://fider.io/) frontend to power [bounties.monero.social](https://bounties.monero.social/). This C# implementation was
made because the existing code stopped working and is no longer maintained.

![Architecture Diagram](./docs/assets/architecture.png)

## Featues

- A single executable whose behaviour can be configured easily by editing the `appsettings.json` file.
- Stable donation address selection for posts which prevents posts from using an address which has already recieved funds.
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

## Development

To run the monerobot locally for development purposes you will need to:

1. Set up `monerod` to run in testnet mode (e.g. `./monerod --testnet --no-igd --no-zmq --prune-blockchain --data-dir "/home/henry/Projects/monerobot/.bitmonero"`).
2. Set up `monero-wallet-rpc` to run testnet mode pointing to the bounty bot's wallet (e.g. `./monero-wallet-rpc --rpc-bind-port 28083 --testnet --wallet-file monerobot --prompt-for-password --disable-rpc-login`).
3. Create a `appsettings.Development.json` and set the appropriate overrides, for example:
```json
{
    "ConnectionStrings": {
        "MoneroBotContext": "Data Source=/home/henry/Documents/monerobot/monerobot.db"
    },
    "DaemonOptions": {
        "FiderMoneroBotUserId": 1
    },
    "Fider": {
        "ApiKey": "Kc0tioz9Uwyv64qkYXRJFviyCuQwT7Me3i3jGCpCyP9RNBqRwE4AU5tiuLqbtp2J",
        "BaseAddress": "http://localhost:8080",
        "ImpersonationUserId": 1
    }
}
```
4. Start up the development services (`seq` for logging, `smtp4dev` for email interception, `fider` for a local fider instance, `postgres` for a database, `adminer` for an interface to the database) via `docker compose -f docker-compose.dev.yml up`.

Then open up fider at `http://localhost:8080`, the bot should start processing bounties. A visual studio launch profile for the daemon has been included.

## Configuration

The `appsettings.json` file can be modified and placed alongside the executable to configure the daemon. The
following settings are available:

```jsonc
{
    "ConnectionStrings": {
        // The sqlite database in which the state of the bot is stored.
        "MoneroBotContext": null
    },
    // Settings related to the daemon program itself.
    "DaemonOptions": {
        // The account index to use when registering new bounties.
        "WalletAccountIndex": 0,
        // The delay which background processes wait for before starting the
        // background action again.
        "PollingInterval": "00:00:10",
        // The Fider user ID of the Monero Bot account - only comments by this
        // user will be considered when attempting to restore the database
        // from fider state.
        "FiderMoneroBotUserId": null
    },
    "Fider": {
    // The Fider API key to use when creating or updating posts/comments.
        "ApiKey": null,
        // The address where the Fider instance is accessible (i.e https://localhost:38080).
        "BaseAddress": null,
        // If set the value will be used to set the X-Fider-UserID header allowing
        // the user to be impersonated.
        "ImpersonationUserId": null
    },
    "MoneroWalletRpc": {
        // The address where a running instance of the Monero Wallet RPC server is accessible (i.e https://localhost:28083).
        "BaseAddress": "https://localhost:28083",
        // If provided configures the username to use when logging into the RPC server.
        "RpcUsername": null,
        // Can be provided along with `RpcUsername` when logging into the RPC server requires
        // a password.
        "RpcPassword": null,
        // Configures whether the daemon program should accept self-signed HTTPS certificates
        // from the Monero Wallet RPC server.
        "AcceptSelfSignedCerts": true
    }
}
```
