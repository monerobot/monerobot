# MoneroBot

# MoneroBot.Fider.ApiClient

Fider is the UI front end used to create posts (bounties). For our purposes we just want to write a simple
wrapper around their HTTP API.

The `ApiClient` just needs to support the following functionality:

1. Get the latest post
2. Get N posts starting from the most recent
3. Make a comment on a post
4. Edit a comment

# MoneroBot.Xmr.RpcClient

We use the daemon HTTP RPC interface to generate subaddresses and scan for incoming transactions. Again we
write a simple wrapper around the existing RPC interface to support the following functionality:

1. Open a wallet (open_wallet)
2. Close a wallet (close_wallet)
3. Retrieve the list of incoming transfers (incoming_transfers)

# MoneroBot.Console

The console app which leverages the `ApiClient` and `RpcClient` to perform the bots functionality. The console application
spwans two seperate background threads which log to the console:

## BountyRegistrationService

The purpose of this service is to find new posts using the Fider API and create 'bounty' entries in the database, the algorithim
for this is as follows:

1. Find the most recent bounties's associated post number
2. Get the latest post number
3. Work out how many posts have not had bounties created for them (latest post number - last bounty post number)
4. Grab all the posts that do not have bounties
5. For each post:
	5.1 generate a subaddress using the monero RPC
	5.2 create a bounty entry in the database
	5.3 create a comment with a QR code embedded

## BountyContributionService

The purpose of this service is to scan a monero wallet for incoming transfers and update the current dontation value of the bounty,
it should also make a comment that the bounty has been increased. It should use the following algorithim:

(TBD)

- Comments should be made for unconfirmed transfers but with different text (and then updated when confirmations > N)
- The process should be resumable/interuptable based upon the information in the `Bounty` table


# Tables

Bounty
	id
	post_number
	subaddress_index
	subaddress
	subaddress_qr

BountyContribution
	id
	bounty_id
	transaction_hash
	confirmations
	comment_id
	amount

View: BountyTotals
	bounty_id
	total_amount
	confirmed_amount
	unconfirmed_amount
