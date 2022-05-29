namespace MoneroBot.WalletRpc.Models.Generated;

using System.Text.Json.Serialization;
using System.Collections.Generic;

#nullable enable
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1600 // Elements should be documented

public class PerSubaddressInfo
{
    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("address_index")]
    public uint AddressIndex { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("balance")]
    public ulong Balance { get; set; }

    [JsonPropertyName("unlocked_balance")]
    public ulong UnlockedBalance { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("num_unspent_outputs")]
    public ulong NumUnspentOutputs { get; set; }

    [JsonPropertyName("blocks_to_unlock")]
    public ulong BlocksToUnlock { get; set; }

    [JsonPropertyName("time_to_unlock")]
    public ulong TimeToUnlock { get; set; }

    public PerSubaddressInfo(uint accountIndex, uint addressIndex, string address, ulong balance, ulong unlockedBalance, string label, ulong numUnspentOutputs, ulong blocksToUnlock, ulong timeToUnlock)
    {
        AccountIndex = accountIndex;
        AddressIndex = addressIndex;
        Address = address;
        Balance = balance;
        UnlockedBalance = unlockedBalance;
        Label = label;
        NumUnspentOutputs = numUnspentOutputs;
        BlocksToUnlock = blocksToUnlock;
        TimeToUnlock = timeToUnlock;
    }
}

public class AddressInfo
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("address_index")]
    public uint AddressIndex { get; set; }

    [JsonPropertyName("used")]
    public bool Used { get; set; }

    public AddressInfo(string address, string label, uint addressIndex, bool used)
    {
        Address = address;
        Label = label;
        AddressIndex = addressIndex;
        Used = used;
    }
}

public class SubaddressAccountInfo
{
    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("base_address")]
    public string BaseAddress { get; set; }

    [JsonPropertyName("balance")]
    public ulong Balance { get; set; }

    [JsonPropertyName("unlocked_balance")]
    public ulong UnlockedBalance { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("tag")]
    public string Tag { get; set; }

    public SubaddressAccountInfo(uint accountIndex, string baseAddress, ulong balance, ulong unlockedBalance, string label, string tag)
    {
        AccountIndex = accountIndex;
        BaseAddress = baseAddress;
        Balance = balance;
        UnlockedBalance = unlockedBalance;
        Label = label;
        Tag = tag;
    }
}

public class AccountTagInfo
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("accounts")]
    public List<uint>? Accounts { get; set; }

    public AccountTagInfo(string tag, string label, List<uint>? accounts)
    {
        Tag = tag;
        Label = label;
        Accounts = accounts;
    }
}

public class TransferDestination
{
    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    public TransferDestination(ulong amount, string address)
    {
        Amount = amount;
        Address = address;
    }
}

public class KeyImageList
{
    [JsonPropertyName("key_images")]
    public List<string>? KeyImages { get; set; }

    public KeyImageList(List<string>? keyImages)
    {
        KeyImages = keyImages;
    }
}

public class KeyList
{
    [JsonPropertyName("keys")]
    public List<string>? Keys { get; set; }

    public KeyList(List<string>? keys)
    {
        Keys = keys;
    }
}

public class Recipient
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    public Recipient(string address, ulong amount)
    {
        Address = address;
        Amount = amount;
    }
}

public class TransferDescription
{
    [JsonPropertyName("amount_in")]
    public ulong AmountIn { get; set; }

    [JsonPropertyName("amount_out")]
    public ulong AmountOut { get; set; }

    [JsonPropertyName("ring_size")]
    public uint RingSize { get; set; }

    [JsonPropertyName("unlock_time")]
    public ulong UnlockTime { get; set; }

    [JsonPropertyName("recipients")]
    public List<Recipient>? Recipients { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("change_amount")]
    public ulong ChangeAmount { get; set; }

    [JsonPropertyName("change_address")]
    public string ChangeAddress { get; set; }

    [JsonPropertyName("fee")]
    public ulong Fee { get; set; }

    [JsonPropertyName("dummy_outputs")]
    public uint DummyOutputs { get; set; }

    [JsonPropertyName("extra")]
    public string Extra { get; set; }

    public TransferDescription(ulong amountIn, ulong amountOut, uint ringSize, ulong unlockTime, List<Recipient>? recipients, string paymentId, ulong changeAmount, string changeAddress, ulong fee, uint dummyOutputs, string extra)
    {
        AmountIn = amountIn;
        AmountOut = amountOut;
        RingSize = ringSize;
        UnlockTime = unlockTime;
        Recipients = recipients;
        PaymentId = paymentId;
        ChangeAmount = changeAmount;
        ChangeAddress = changeAddress;
        Fee = fee;
        DummyOutputs = dummyOutputs;
        Extra = extra;
    }
}

public class TxsetSummary
{
    [JsonPropertyName("amount_in")]
    public ulong AmountIn { get; set; }

    [JsonPropertyName("amount_out")]
    public ulong AmountOut { get; set; }

    [JsonPropertyName("recipients")]
    public List<Recipient>? Recipients { get; set; }

    [JsonPropertyName("change_amount")]
    public ulong ChangeAmount { get; set; }

    [JsonPropertyName("change_address")]
    public string ChangeAddress { get; set; }

    [JsonPropertyName("fee")]
    public ulong Fee { get; set; }

    public TxsetSummary(ulong amountIn, ulong amountOut, List<Recipient>? recipients, ulong changeAmount, string changeAddress, ulong fee)
    {
        AmountIn = amountIn;
        AmountOut = amountOut;
        Recipients = recipients;
        ChangeAmount = changeAmount;
        ChangeAddress = changeAddress;
        Fee = fee;
    }
}

public class PaymentDetails
{
    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("tx_hash")]
    public string TxHash { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("block_height")]
    public ulong BlockHeight { get; set; }

    [JsonPropertyName("unlock_time")]
    public ulong UnlockTime { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("subaddr_index")]
    public SubaddressIndex SubaddrIndex { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    public PaymentDetails(string paymentId, string txHash, ulong amount, ulong blockHeight, ulong unlockTime, bool locked, SubaddressIndex subaddrIndex, string address)
    {
        PaymentId = paymentId;
        TxHash = txHash;
        Amount = amount;
        BlockHeight = blockHeight;
        UnlockTime = unlockTime;
        Locked = locked;
        SubaddrIndex = subaddrIndex;
        Address = address;
    }
}

public class TransferDetails
{
    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("spent")]
    public bool Spent { get; set; }

    [JsonPropertyName("global_index")]
    public ulong GlobalIndex { get; set; }

    [JsonPropertyName("tx_hash")]
    public string TxHash { get; set; }

    [JsonPropertyName("subaddr_index")]
    public SubaddressIndex SubaddrIndex { get; set; }

    [JsonPropertyName("key_image")]
    public string KeyImage { get; set; }

    [JsonPropertyName("pubkey")]
    public string Pubkey { get; set; }

    [JsonPropertyName("block_height")]
    public ulong BlockHeight { get; set; }

    [JsonPropertyName("frozen")]
    public bool Frozen { get; set; }

    [JsonPropertyName("unlocked")]
    public bool Unlocked { get; set; }

    public TransferDetails(ulong amount, bool spent, ulong globalIndex, string txHash, SubaddressIndex subaddrIndex, string keyImage, string pubkey, ulong blockHeight, bool frozen, bool unlocked)
    {
        Amount = amount;
        Spent = spent;
        GlobalIndex = globalIndex;
        TxHash = txHash;
        SubaddrIndex = subaddrIndex;
        KeyImage = keyImage;
        Pubkey = pubkey;
        BlockHeight = blockHeight;
        Frozen = frozen;
        Unlocked = unlocked;
    }
}

public class TransferEntry
{
    [JsonPropertyName("txid")]
    public string Txid { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("height")]
    public ulong Height { get; set; }

    [JsonPropertyName("timestamp")]
    public ulong Timestamp { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("amounts")]
    public List<ulong>? Amounts { get; set; }

    [JsonPropertyName("fee")]
    public ulong Fee { get; set; }

    [JsonPropertyName("note")]
    public string Note { get; set; }

    [JsonPropertyName("destinations")]
    public List<TransferDestination>? Destinations { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("unlock_time")]
    public ulong UnlockTime { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("subaddr_index")]
    public SubaddressIndex SubaddrIndex { get; set; }

    [JsonPropertyName("subaddr_indices")]
    public List<SubaddressIndex>? SubaddrIndices { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("double_spend_seen")]
    public bool DoubleSpendSeen { get; set; }

    [JsonPropertyName("confirmations")]
    public ulong Confirmations { get; set; }

    [JsonPropertyName("suggested_confirmations_threshold")]
    public ulong SuggestedConfirmationsThreshold { get; set; }

    public TransferEntry(string txid, string paymentId, ulong height, ulong timestamp, ulong amount, List<ulong>? amounts, ulong fee, string note, List<TransferDestination>? destinations, string type, ulong unlockTime, bool locked, SubaddressIndex subaddrIndex, List<SubaddressIndex>? subaddrIndices, string address, bool doubleSpendSeen, ulong confirmations, ulong suggestedConfirmationsThreshold)
    {
        Txid = txid;
        PaymentId = paymentId;
        Height = height;
        Timestamp = timestamp;
        Amount = amount;
        Amounts = amounts;
        Fee = fee;
        Note = note;
        Destinations = destinations;
        Type = type;
        UnlockTime = unlockTime;
        Locked = locked;
        SubaddrIndex = subaddrIndex;
        SubaddrIndices = subaddrIndices;
        Address = address;
        DoubleSpendSeen = doubleSpendSeen;
        Confirmations = confirmations;
        SuggestedConfirmationsThreshold = suggestedConfirmationsThreshold;
    }
}

public class SignedKeyImage
{
    [JsonPropertyName("key_image")]
    public string KeyImage { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public SignedKeyImage(string keyImage, string signature)
    {
        KeyImage = keyImage;
        Signature = signature;
    }
}

public class UriSpec
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("tx_description")]
    public string TxDescription { get; set; }

    [JsonPropertyName("recipient_name")]
    public string RecipientName { get; set; }

    public UriSpec(string address, string paymentId, ulong amount, string txDescription, string recipientName)
    {
        Address = address;
        PaymentId = paymentId;
        Amount = amount;
        TxDescription = txDescription;
        RecipientName = recipientName;
    }
}

public class Entry
{
    [JsonPropertyName("index")]
    public ulong Index { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    public Entry(ulong index, string address, string description)
    {
        Index = index;
        Address = address;
        Description = description;
    }
}

public class Request
{
    [JsonPropertyName("restore_height")]
    public ulong RestoreHeight { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("spendkey")]
    public string Spendkey { get; set; }

    [JsonPropertyName("viewkey")]
    public string Viewkey { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("autosave_current")]
    public bool AutosaveCurrent { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    public Request(ulong restoreHeight, string filename, string address, string spendkey, string viewkey, string password, bool autosaveCurrent, string language)
    {
        RestoreHeight = restoreHeight;
        Filename = filename;
        Address = address;
        Spendkey = spendkey;
        Viewkey = viewkey;
        Password = password;
        AutosaveCurrent = autosaveCurrent;
        Language = language;
    }
}

public class Response
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("info")]
    public string Info { get; set; }

    public Response(string address, string info)
    {
        Address = address;
        Info = info;
    }
}

public class SubaddressIndex
{
    [JsonPropertyName("major")]
    public uint Major { get; set; }

    [JsonPropertyName("minor")]
    public uint Minor { get; set; }

    public SubaddressIndex(uint major, uint minor)
    {
        Major = major;
        Minor = minor;
    }
}

public class GetBalanceParameters
{
    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("address_indices")]
    public HashSet<uint>? AddressIndices { get; set; }

    [JsonPropertyName("all_accounts")]
    public bool AllAccounts { get; set; }

    [JsonPropertyName("strict")]
    public bool Strict { get; set; }

    public GetBalanceParameters(uint accountIndex, HashSet<uint>? addressIndices, bool allAccounts, bool strict)
    {
        AccountIndex = accountIndex;
        AddressIndices = addressIndices;
        AllAccounts = allAccounts;
        Strict = strict;
    }
}

public class GetBalanceResult
{
    [JsonPropertyName("balance")]
    public ulong Balance { get; set; }

    [JsonPropertyName("unlocked_balance")]
    public ulong UnlockedBalance { get; set; }

    [JsonPropertyName("multisig_import_needed")]
    public bool MultisigImportNeeded { get; set; }

    [JsonPropertyName("per_subaddress")]
    public List<PerSubaddressInfo>? PerSubaddress { get; set; }

    [JsonPropertyName("blocks_to_unlock")]
    public ulong BlocksToUnlock { get; set; }

    [JsonPropertyName("time_to_unlock")]
    public ulong TimeToUnlock { get; set; }

    public GetBalanceResult(ulong balance, ulong unlockedBalance, bool multisigImportNeeded, List<PerSubaddressInfo>? perSubaddress, ulong blocksToUnlock, ulong timeToUnlock)
    {
        Balance = balance;
        UnlockedBalance = unlockedBalance;
        MultisigImportNeeded = multisigImportNeeded;
        PerSubaddress = perSubaddress;
        BlocksToUnlock = blocksToUnlock;
        TimeToUnlock = timeToUnlock;
    }
}

public class GetAddressParameters
{
    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("address_index")]
    public List<uint>? AddressIndex { get; set; }

    public GetAddressParameters(uint accountIndex, List<uint>? addressIndex)
    {
        AccountIndex = accountIndex;
        AddressIndex = addressIndex;
    }
}

public class GetAddressResult
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("addresses")]
    public List<AddressInfo>? Addresses { get; set; }

    public GetAddressResult(string address, List<AddressInfo>? addresses)
    {
        Address = address;
        Addresses = addresses;
    }
}

public class GetAddressIndexParameters
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    public GetAddressIndexParameters(string address)
    {
        Address = address;
    }
}

public class GetAddressIndexResult
{
    [JsonPropertyName("index")]
    public SubaddressIndex Index { get; set; }

    public GetAddressIndexResult(SubaddressIndex index)
    {
        Index = index;
    }
}

public class CreateAddressParameters
{
    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("count")]
    public uint Count { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    public CreateAddressParameters(uint accountIndex, uint count, string label)
    {
        AccountIndex = accountIndex;
        Count = count;
        Label = label;
    }
}

public class CreateAddressResult
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("address_index")]
    public uint AddressIndex { get; set; }

    [JsonPropertyName("addresses")]
    public List<string>? Addresses { get; set; }

    [JsonPropertyName("address_indices")]
    public List<uint>? AddressIndices { get; set; }

    public CreateAddressResult(string address, uint addressIndex, List<string>? addresses, List<uint>? addressIndices)
    {
        Address = address;
        AddressIndex = addressIndex;
        Addresses = addresses;
        AddressIndices = addressIndices;
    }
}

public class LabelAddressParameters
{
    [JsonPropertyName("index")]
    public SubaddressIndex Index { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    public LabelAddressParameters(SubaddressIndex index, string label)
    {
        Index = index;
        Label = label;
    }
}

public class LabelAddressResult
{
    public LabelAddressResult()
    {
    }
}

public class GetAccountsParameters
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; }

    [JsonPropertyName("strict_balances")]
    public bool StrictBalances { get; set; }

    [JsonPropertyName("regexp")]
    public bool Regexp { get; set; }

    public GetAccountsParameters(string tag, bool strictBalances, bool regexp)
    {
        Tag = tag;
        StrictBalances = strictBalances;
        Regexp = regexp;
    }
}

public class GetAccountsResult
{
    [JsonPropertyName("total_balance")]
    public ulong TotalBalance { get; set; }

    [JsonPropertyName("total_unlocked_balance")]
    public ulong TotalUnlockedBalance { get; set; }

    [JsonPropertyName("subaddress_accounts")]
    public List<SubaddressAccountInfo>? SubaddressAccounts { get; set; }

    public GetAccountsResult(ulong totalBalance, ulong totalUnlockedBalance, List<SubaddressAccountInfo>? subaddressAccounts)
    {
        TotalBalance = totalBalance;
        TotalUnlockedBalance = totalUnlockedBalance;
        SubaddressAccounts = subaddressAccounts;
    }
}

public class CreateAccountParameters
{
    [JsonPropertyName("label")]
    public string Label { get; set; }

    public CreateAccountParameters(string label)
    {
        Label = label;
    }
}

public class CreateAccountResult
{
    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    public CreateAccountResult(uint accountIndex, string address)
    {
        AccountIndex = accountIndex;
        Address = address;
    }
}

public class LabelAccountParameters
{
    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    public LabelAccountParameters(uint accountIndex, string label)
    {
        AccountIndex = accountIndex;
        Label = label;
    }
}

public class LabelAccountResult
{
    public LabelAccountResult()
    {
    }
}

public class GetAccountTagsParameters
{
    public GetAccountTagsParameters()
    {
    }
}

public class GetAccountTagsResult
{
    [JsonPropertyName("account_tags")]
    public List<AccountTagInfo>? AccountTags { get; set; }

    public GetAccountTagsResult(List<AccountTagInfo>? accountTags)
    {
        AccountTags = accountTags;
    }
}

public class TagAccountsParameters
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; }

    [JsonPropertyName("accounts")]
    public HashSet<uint>? Accounts { get; set; }

    public TagAccountsParameters(string tag, HashSet<uint>? accounts)
    {
        Tag = tag;
        Accounts = accounts;
    }
}

public class TagAccountsResult
{
    public TagAccountsResult()
    {
    }
}

public class UntagAccountsParameters
{
    [JsonPropertyName("accounts")]
    public HashSet<uint>? Accounts { get; set; }

    public UntagAccountsParameters(HashSet<uint>? accounts)
    {
        Accounts = accounts;
    }
}

public class UntagAccountsResult
{
    public UntagAccountsResult()
    {
    }
}

public class SetAccountTagDescriptionParameters
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    public SetAccountTagDescriptionParameters(string tag, string description)
    {
        Tag = tag;
        Description = description;
    }
}

public class SetAccountTagDescriptionResult
{
    public SetAccountTagDescriptionResult()
    {
    }
}

public class GetHeightParameters
{
    public GetHeightParameters()
    {
    }
}

public class GetHeightResult
{
    [JsonPropertyName("height")]
    public ulong Height { get; set; }

    public GetHeightResult(ulong height)
    {
        Height = height;
    }
}

public class FreezeParameters
{
    [JsonPropertyName("key_image")]
    public string KeyImage { get; set; }

    public FreezeParameters(string keyImage)
    {
        KeyImage = keyImage;
    }
}

public class FreezeResult
{
    public FreezeResult()
    {
    }
}

public class ThawParameters
{
    [JsonPropertyName("key_image")]
    public string KeyImage { get; set; }

    public ThawParameters(string keyImage)
    {
        KeyImage = keyImage;
    }
}

public class ThawResult
{
    public ThawResult()
    {
    }
}

public class FrozenParameters
{
    [JsonPropertyName("key_image")]
    public string KeyImage { get; set; }

    public FrozenParameters(string keyImage)
    {
        KeyImage = keyImage;
    }
}

public class FrozenResult
{
    [JsonPropertyName("frozen")]
    public bool Frozen { get; set; }

    public FrozenResult(bool frozen)
    {
        Frozen = frozen;
    }
}

public class TransferParameters
{
    [JsonPropertyName("destinations")]
    public List<TransferDestination>? Destinations { get; set; }

    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("subaddr_indices")]
    public HashSet<uint>? SubaddrIndices { get; set; }

    [JsonPropertyName("priority")]
    public uint Priority { get; set; }

    [JsonPropertyName("ring_size")]
    public ulong RingSize { get; set; }

    [JsonPropertyName("unlock_time")]
    public ulong UnlockTime { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("get_tx_key")]
    public bool GetTxKey { get; set; }

    [JsonPropertyName("do_not_relay")]
    public bool DoNotRelay { get; set; }

    [JsonPropertyName("get_tx_hex")]
    public bool GetTxHex { get; set; }

    [JsonPropertyName("get_tx_metadata")]
    public bool GetTxMetadata { get; set; }

    public TransferParameters(List<TransferDestination>? destinations, uint accountIndex, HashSet<uint>? subaddrIndices, uint priority, ulong ringSize, ulong unlockTime, string paymentId, bool getTxKey, bool doNotRelay, bool getTxHex, bool getTxMetadata)
    {
        Destinations = destinations;
        AccountIndex = accountIndex;
        SubaddrIndices = subaddrIndices;
        Priority = priority;
        RingSize = ringSize;
        UnlockTime = unlockTime;
        PaymentId = paymentId;
        GetTxKey = getTxKey;
        DoNotRelay = doNotRelay;
        GetTxHex = getTxHex;
        GetTxMetadata = getTxMetadata;
    }
}

public class TransferResult
{
    [JsonPropertyName("tx_hash")]
    public string TxHash { get; set; }

    [JsonPropertyName("tx_key")]
    public string TxKey { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("fee")]
    public ulong Fee { get; set; }

    [JsonPropertyName("weight")]
    public ulong Weight { get; set; }

    [JsonPropertyName("tx_blob")]
    public string TxBlob { get; set; }

    [JsonPropertyName("tx_metadata")]
    public string TxMetadata { get; set; }

    [JsonPropertyName("multisig_txset")]
    public string MultisigTxset { get; set; }

    [JsonPropertyName("unsigned_txset")]
    public string UnsignedTxset { get; set; }

    [JsonPropertyName("spent_key_images")]
    public KeyImageList SpentKeyImages { get; set; }

    public TransferResult(string txHash, string txKey, ulong amount, ulong fee, ulong weight, string txBlob, string txMetadata, string multisigTxset, string unsignedTxset, KeyImageList spentKeyImages)
    {
        TxHash = txHash;
        TxKey = txKey;
        Amount = amount;
        Fee = fee;
        Weight = weight;
        TxBlob = txBlob;
        TxMetadata = txMetadata;
        MultisigTxset = multisigTxset;
        UnsignedTxset = unsignedTxset;
        SpentKeyImages = spentKeyImages;
    }
}

public class TransferSplitParameters
{
    [JsonPropertyName("destinations")]
    public List<TransferDestination>? Destinations { get; set; }

    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("subaddr_indices")]
    public HashSet<uint>? SubaddrIndices { get; set; }

    [JsonPropertyName("priority")]
    public uint Priority { get; set; }

    [JsonPropertyName("ring_size")]
    public ulong RingSize { get; set; }

    [JsonPropertyName("unlock_time")]
    public ulong UnlockTime { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("get_tx_keys")]
    public bool GetTxKeys { get; set; }

    [JsonPropertyName("do_not_relay")]
    public bool DoNotRelay { get; set; }

    [JsonPropertyName("get_tx_hex")]
    public bool GetTxHex { get; set; }

    [JsonPropertyName("get_tx_metadata")]
    public bool GetTxMetadata { get; set; }

    public TransferSplitParameters(List<TransferDestination>? destinations, uint accountIndex, HashSet<uint>? subaddrIndices, uint priority, ulong ringSize, ulong unlockTime, string paymentId, bool getTxKeys, bool doNotRelay, bool getTxHex, bool getTxMetadata)
    {
        Destinations = destinations;
        AccountIndex = accountIndex;
        SubaddrIndices = subaddrIndices;
        Priority = priority;
        RingSize = ringSize;
        UnlockTime = unlockTime;
        PaymentId = paymentId;
        GetTxKeys = getTxKeys;
        DoNotRelay = doNotRelay;
        GetTxHex = getTxHex;
        GetTxMetadata = getTxMetadata;
    }
}

public class TransferSplitResult
{
    [JsonPropertyName("tx_hash_list")]
    public List<string>? TxHashList { get; set; }

    [JsonPropertyName("tx_key_list")]
    public List<string>? TxKeyList { get; set; }

    [JsonPropertyName("amount_list")]
    public List<ulong>? AmountList { get; set; }

    [JsonPropertyName("fee_list")]
    public List<ulong>? FeeList { get; set; }

    [JsonPropertyName("weight_list")]
    public List<ulong>? WeightList { get; set; }

    [JsonPropertyName("tx_blob_list")]
    public List<string>? TxBlobList { get; set; }

    [JsonPropertyName("tx_metadata_list")]
    public List<string>? TxMetadataList { get; set; }

    [JsonPropertyName("multisig_txset")]
    public string MultisigTxset { get; set; }

    [JsonPropertyName("unsigned_txset")]
    public string UnsignedTxset { get; set; }

    [JsonPropertyName("spent_key_images_list")]
    public List<KeyImageList>? SpentKeyImagesList { get; set; }

    public TransferSplitResult(List<string>? txHashList, List<string>? txKeyList, List<ulong>? amountList, List<ulong>? feeList, List<ulong>? weightList, List<string>? txBlobList, List<string>? txMetadataList, string multisigTxset, string unsignedTxset, List<KeyImageList>? spentKeyImagesList)
    {
        TxHashList = txHashList;
        TxKeyList = txKeyList;
        AmountList = amountList;
        FeeList = feeList;
        WeightList = weightList;
        TxBlobList = txBlobList;
        TxMetadataList = txMetadataList;
        MultisigTxset = multisigTxset;
        UnsignedTxset = unsignedTxset;
        SpentKeyImagesList = spentKeyImagesList;
    }
}

public class DescribeTransferParameters
{
    [JsonPropertyName("unsigned_txset")]
    public string UnsignedTxset { get; set; }

    [JsonPropertyName("multisig_txset")]
    public string MultisigTxset { get; set; }

    public DescribeTransferParameters(string unsignedTxset, string multisigTxset)
    {
        UnsignedTxset = unsignedTxset;
        MultisigTxset = multisigTxset;
    }
}

public class DescribeTransferResult
{
    [JsonPropertyName("desc")]
    public List<TransferDescription>? Desc { get; set; }

    [JsonPropertyName("summary")]
    public TxsetSummary Summary { get; set; }

    public DescribeTransferResult(List<TransferDescription>? desc, TxsetSummary summary)
    {
        Desc = desc;
        Summary = summary;
    }
}

public class SignTransferParameters
{
    [JsonPropertyName("unsigned_txset")]
    public string UnsignedTxset { get; set; }

    [JsonPropertyName("export_raw")]
    public bool ExportRaw { get; set; }

    [JsonPropertyName("get_tx_keys")]
    public bool GetTxKeys { get; set; }

    public SignTransferParameters(string unsignedTxset, bool exportRaw, bool getTxKeys)
    {
        UnsignedTxset = unsignedTxset;
        ExportRaw = exportRaw;
        GetTxKeys = getTxKeys;
    }
}

public class SignTransferResult
{
    [JsonPropertyName("signed_txset")]
    public string SignedTxset { get; set; }

    [JsonPropertyName("tx_hash_list")]
    public List<string>? TxHashList { get; set; }

    [JsonPropertyName("tx_raw_list")]
    public List<string>? TxRawList { get; set; }

    [JsonPropertyName("tx_key_list")]
    public List<string>? TxKeyList { get; set; }

    public SignTransferResult(string signedTxset, List<string>? txHashList, List<string>? txRawList, List<string>? txKeyList)
    {
        SignedTxset = signedTxset;
        TxHashList = txHashList;
        TxRawList = txRawList;
        TxKeyList = txKeyList;
    }
}

public class SubmitTransferParameters
{
    [JsonPropertyName("tx_data_hex")]
    public string TxDataHex { get; set; }

    public SubmitTransferParameters(string txDataHex)
    {
        TxDataHex = txDataHex;
    }
}

public class SubmitTransferResult
{
    [JsonPropertyName("tx_hash_list")]
    public List<string>? TxHashList { get; set; }

    public SubmitTransferResult(List<string>? txHashList)
    {
        TxHashList = txHashList;
    }
}

public class SweepDustParameters
{
    [JsonPropertyName("get_tx_keys")]
    public bool GetTxKeys { get; set; }

    [JsonPropertyName("do_not_relay")]
    public bool DoNotRelay { get; set; }

    [JsonPropertyName("get_tx_hex")]
    public bool GetTxHex { get; set; }

    [JsonPropertyName("get_tx_metadata")]
    public bool GetTxMetadata { get; set; }

    public SweepDustParameters(bool getTxKeys, bool doNotRelay, bool getTxHex, bool getTxMetadata)
    {
        GetTxKeys = getTxKeys;
        DoNotRelay = doNotRelay;
        GetTxHex = getTxHex;
        GetTxMetadata = getTxMetadata;
    }
}

public class SweepDustResult
{
    [JsonPropertyName("tx_hash_list")]
    public List<string>? TxHashList { get; set; }

    [JsonPropertyName("tx_key_list")]
    public List<string>? TxKeyList { get; set; }

    [JsonPropertyName("amount_list")]
    public List<ulong>? AmountList { get; set; }

    [JsonPropertyName("fee_list")]
    public List<ulong>? FeeList { get; set; }

    [JsonPropertyName("weight_list")]
    public List<ulong>? WeightList { get; set; }

    [JsonPropertyName("tx_blob_list")]
    public List<string>? TxBlobList { get; set; }

    [JsonPropertyName("tx_metadata_list")]
    public List<string>? TxMetadataList { get; set; }

    [JsonPropertyName("multisig_txset")]
    public string MultisigTxset { get; set; }

    [JsonPropertyName("unsigned_txset")]
    public string UnsignedTxset { get; set; }

    [JsonPropertyName("spent_key_images_list")]
    public List<KeyImageList>? SpentKeyImagesList { get; set; }

    public SweepDustResult(List<string>? txHashList, List<string>? txKeyList, List<ulong>? amountList, List<ulong>? feeList, List<ulong>? weightList, List<string>? txBlobList, List<string>? txMetadataList, string multisigTxset, string unsignedTxset, List<KeyImageList>? spentKeyImagesList)
    {
        TxHashList = txHashList;
        TxKeyList = txKeyList;
        AmountList = amountList;
        FeeList = feeList;
        WeightList = weightList;
        TxBlobList = txBlobList;
        TxMetadataList = txMetadataList;
        MultisigTxset = multisigTxset;
        UnsignedTxset = unsignedTxset;
        SpentKeyImagesList = spentKeyImagesList;
    }
}

public class SweepAllParameters
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("subaddr_indices")]
    public HashSet<uint>? SubaddrIndices { get; set; }

    [JsonPropertyName("subaddr_indices_all")]
    public bool SubaddrIndicesAll { get; set; }

    [JsonPropertyName("priority")]
    public uint Priority { get; set; }

    [JsonPropertyName("ring_size")]
    public ulong RingSize { get; set; }

    [JsonPropertyName("outputs")]
    public ulong Outputs { get; set; }

    [JsonPropertyName("unlock_time")]
    public ulong UnlockTime { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("get_tx_keys")]
    public bool GetTxKeys { get; set; }

    [JsonPropertyName("below_amount")]
    public ulong BelowAmount { get; set; }

    [JsonPropertyName("do_not_relay")]
    public bool DoNotRelay { get; set; }

    [JsonPropertyName("get_tx_hex")]
    public bool GetTxHex { get; set; }

    [JsonPropertyName("get_tx_metadata")]
    public bool GetTxMetadata { get; set; }

    public SweepAllParameters(string address, uint accountIndex, HashSet<uint>? subaddrIndices, bool subaddrIndicesAll, uint priority, ulong ringSize, ulong outputs, ulong unlockTime, string paymentId, bool getTxKeys, ulong belowAmount, bool doNotRelay, bool getTxHex, bool getTxMetadata)
    {
        Address = address;
        AccountIndex = accountIndex;
        SubaddrIndices = subaddrIndices;
        SubaddrIndicesAll = subaddrIndicesAll;
        Priority = priority;
        RingSize = ringSize;
        Outputs = outputs;
        UnlockTime = unlockTime;
        PaymentId = paymentId;
        GetTxKeys = getTxKeys;
        BelowAmount = belowAmount;
        DoNotRelay = doNotRelay;
        GetTxHex = getTxHex;
        GetTxMetadata = getTxMetadata;
    }
}

public class SweepAllResult
{
    [JsonPropertyName("tx_hash_list")]
    public List<string>? TxHashList { get; set; }

    [JsonPropertyName("tx_key_list")]
    public List<string>? TxKeyList { get; set; }

    [JsonPropertyName("amount_list")]
    public List<ulong>? AmountList { get; set; }

    [JsonPropertyName("fee_list")]
    public List<ulong>? FeeList { get; set; }

    [JsonPropertyName("weight_list")]
    public List<ulong>? WeightList { get; set; }

    [JsonPropertyName("tx_blob_list")]
    public List<string>? TxBlobList { get; set; }

    [JsonPropertyName("tx_metadata_list")]
    public List<string>? TxMetadataList { get; set; }

    [JsonPropertyName("multisig_txset")]
    public string MultisigTxset { get; set; }

    [JsonPropertyName("unsigned_txset")]
    public string UnsignedTxset { get; set; }

    [JsonPropertyName("spent_key_images_list")]
    public List<KeyImageList>? SpentKeyImagesList { get; set; }

    public SweepAllResult(List<string>? txHashList, List<string>? txKeyList, List<ulong>? amountList, List<ulong>? feeList, List<ulong>? weightList, List<string>? txBlobList, List<string>? txMetadataList, string multisigTxset, string unsignedTxset, List<KeyImageList>? spentKeyImagesList)
    {
        TxHashList = txHashList;
        TxKeyList = txKeyList;
        AmountList = amountList;
        FeeList = feeList;
        WeightList = weightList;
        TxBlobList = txBlobList;
        TxMetadataList = txMetadataList;
        MultisigTxset = multisigTxset;
        UnsignedTxset = unsignedTxset;
        SpentKeyImagesList = spentKeyImagesList;
    }
}

public class SweepSingleParameters
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("priority")]
    public uint Priority { get; set; }

    [JsonPropertyName("ring_size")]
    public ulong RingSize { get; set; }

    [JsonPropertyName("outputs")]
    public ulong Outputs { get; set; }

    [JsonPropertyName("unlock_time")]
    public ulong UnlockTime { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("get_tx_key")]
    public bool GetTxKey { get; set; }

    [JsonPropertyName("key_image")]
    public string KeyImage { get; set; }

    [JsonPropertyName("do_not_relay")]
    public bool DoNotRelay { get; set; }

    [JsonPropertyName("get_tx_hex")]
    public bool GetTxHex { get; set; }

    [JsonPropertyName("get_tx_metadata")]
    public bool GetTxMetadata { get; set; }

    public SweepSingleParameters(string address, uint priority, ulong ringSize, ulong outputs, ulong unlockTime, string paymentId, bool getTxKey, string keyImage, bool doNotRelay, bool getTxHex, bool getTxMetadata)
    {
        Address = address;
        Priority = priority;
        RingSize = ringSize;
        Outputs = outputs;
        UnlockTime = unlockTime;
        PaymentId = paymentId;
        GetTxKey = getTxKey;
        KeyImage = keyImage;
        DoNotRelay = doNotRelay;
        GetTxHex = getTxHex;
        GetTxMetadata = getTxMetadata;
    }
}

public class SweepSingleResult
{
    [JsonPropertyName("tx_hash")]
    public string TxHash { get; set; }

    [JsonPropertyName("tx_key")]
    public string TxKey { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("fee")]
    public ulong Fee { get; set; }

    [JsonPropertyName("weight")]
    public ulong Weight { get; set; }

    [JsonPropertyName("tx_blob")]
    public string TxBlob { get; set; }

    [JsonPropertyName("tx_metadata")]
    public string TxMetadata { get; set; }

    [JsonPropertyName("multisig_txset")]
    public string MultisigTxset { get; set; }

    [JsonPropertyName("unsigned_txset")]
    public string UnsignedTxset { get; set; }

    [JsonPropertyName("spent_key_images")]
    public KeyImageList SpentKeyImages { get; set; }

    public SweepSingleResult(string txHash, string txKey, ulong amount, ulong fee, ulong weight, string txBlob, string txMetadata, string multisigTxset, string unsignedTxset, KeyImageList spentKeyImages)
    {
        TxHash = txHash;
        TxKey = txKey;
        Amount = amount;
        Fee = fee;
        Weight = weight;
        TxBlob = txBlob;
        TxMetadata = txMetadata;
        MultisigTxset = multisigTxset;
        UnsignedTxset = unsignedTxset;
        SpentKeyImages = spentKeyImages;
    }
}

public class RelayTxParameters
{
    [JsonPropertyName("hex")]
    public string Hex { get; set; }

    public RelayTxParameters(string hex)
    {
        Hex = hex;
    }
}

public class RelayTxResult
{
    [JsonPropertyName("tx_hash")]
    public string TxHash { get; set; }

    public RelayTxResult(string txHash)
    {
        TxHash = txHash;
    }
}

public class StoreParameters
{
    public StoreParameters()
    {
    }
}

public class StoreResult
{
    public StoreResult()
    {
    }
}

public class GetPaymentsParameters
{
    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    public GetPaymentsParameters(string paymentId)
    {
        PaymentId = paymentId;
    }
}

public class GetPaymentsResult
{
    [JsonPropertyName("payments")]
    public List<PaymentDetails>? Payments { get; set; }

    public GetPaymentsResult(List<PaymentDetails>? payments)
    {
        Payments = payments;
    }
}

public class GetBulkPaymentsParameters
{
    [JsonPropertyName("payment_ids")]
    public List<string>? PaymentIds { get; set; }

    [JsonPropertyName("min_block_height")]
    public ulong MinBlockHeight { get; set; }

    public GetBulkPaymentsParameters(List<string>? paymentIds, ulong minBlockHeight)
    {
        PaymentIds = paymentIds;
        MinBlockHeight = minBlockHeight;
    }
}

public class GetBulkPaymentsResult
{
    [JsonPropertyName("payments")]
    public List<PaymentDetails>? Payments { get; set; }

    public GetBulkPaymentsResult(List<PaymentDetails>? payments)
    {
        Payments = payments;
    }
}

public class IncomingTransfersParameters
{
    [JsonPropertyName("transfer_type")]
    public string TransferType { get; set; }

    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("subaddr_indices")]
    public HashSet<uint>? SubaddrIndices { get; set; }

    public IncomingTransfersParameters(string transferType, uint accountIndex, HashSet<uint>? subaddrIndices)
    {
        TransferType = transferType;
        AccountIndex = accountIndex;
        SubaddrIndices = subaddrIndices;
    }
}

public class IncomingTransfersResult
{
    [JsonPropertyName("transfers")]
    public List<TransferDetails>? Transfers { get; set; }

    public IncomingTransfersResult(List<TransferDetails>? transfers)
    {
        Transfers = transfers;
    }
}

public class QueryKeyParameters
{
    [JsonPropertyName("key_type")]
    public string KeyType { get; set; }

    public QueryKeyParameters(string keyType)
    {
        KeyType = keyType;
    }
}

public class QueryKeyResult
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    public QueryKeyResult(string key)
    {
        Key = key;
    }
}

public class MakeIntegratedAddressParameters
{
    [JsonPropertyName("standard_address")]
    public string StandardAddress { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    public MakeIntegratedAddressParameters(string standardAddress, string paymentId)
    {
        StandardAddress = standardAddress;
        PaymentId = paymentId;
    }
}

public class MakeIntegratedAddressResult
{
    [JsonPropertyName("integrated_address")]
    public string IntegratedAddress { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    public MakeIntegratedAddressResult(string integratedAddress, string paymentId)
    {
        IntegratedAddress = integratedAddress;
        PaymentId = paymentId;
    }
}

public class SplitIntegratedAddressParameters
{
    [JsonPropertyName("integrated_address")]
    public string IntegratedAddress { get; set; }

    public SplitIntegratedAddressParameters(string integratedAddress)
    {
        IntegratedAddress = integratedAddress;
    }
}

public class SplitIntegratedAddressResult
{
    [JsonPropertyName("standard_address")]
    public string StandardAddress { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("is_subaddress")]
    public bool IsSubaddress { get; set; }

    public SplitIntegratedAddressResult(string standardAddress, string paymentId, bool isSubaddress)
    {
        StandardAddress = standardAddress;
        PaymentId = paymentId;
        IsSubaddress = isSubaddress;
    }
}

public class StopWalletParameters
{
    public StopWalletParameters()
    {
    }
}

public class StopWalletResult
{
    public StopWalletResult()
    {
    }
}

public class RescanBlockchainParameters
{
    [JsonPropertyName("hard")]
    public bool Hard { get; set; }

    public RescanBlockchainParameters(bool hard)
    {
        Hard = hard;
    }
}

public class RescanBlockchainResult
{
    public RescanBlockchainResult()
    {
    }
}

public class SetTxNotesParameters
{
    [JsonPropertyName("txids")]
    public List<string>? Txids { get; set; }

    [JsonPropertyName("notes")]
    public List<string>? Notes { get; set; }

    public SetTxNotesParameters(List<string>? txids, List<string>? notes)
    {
        Txids = txids;
        Notes = notes;
    }
}

public class SetTxNotesResult
{
    public SetTxNotesResult()
    {
    }
}

public class GetTxNotesParameters
{
    [JsonPropertyName("txids")]
    public List<string>? Txids { get; set; }

    public GetTxNotesParameters(List<string>? txids)
    {
        Txids = txids;
    }
}

public class GetTxNotesResult
{
    [JsonPropertyName("notes")]
    public List<string>? Notes { get; set; }

    public GetTxNotesResult(List<string>? notes)
    {
        Notes = notes;
    }
}

public class SetAttributeParameters
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    public SetAttributeParameters(string key, string value)
    {
        Key = key;
        Value = value;
    }
}

public class SetAttributeResult
{
    public SetAttributeResult()
    {
    }
}

public class GetAttributeParameters
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    public GetAttributeParameters(string key)
    {
        Key = key;
    }
}

public class GetAttributeResult
{
    [JsonPropertyName("value")]
    public string Value { get; set; }

    public GetAttributeResult(string value)
    {
        Value = value;
    }
}

public class GetTxKeyParameters
{
    [JsonPropertyName("txid")]
    public string Txid { get; set; }

    public GetTxKeyParameters(string txid)
    {
        Txid = txid;
    }
}

public class GetTxKeyResult
{
    [JsonPropertyName("tx_key")]
    public string TxKey { get; set; }

    public GetTxKeyResult(string txKey)
    {
        TxKey = txKey;
    }
}

public class CheckTxKeyParameters
{
    [JsonPropertyName("txid")]
    public string Txid { get; set; }

    [JsonPropertyName("tx_key")]
    public string TxKey { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    public CheckTxKeyParameters(string txid, string txKey, string address)
    {
        Txid = txid;
        TxKey = txKey;
        Address = address;
    }
}

public class CheckTxKeyResult
{
    [JsonPropertyName("received")]
    public ulong Received { get; set; }

    [JsonPropertyName("in_pool")]
    public bool InPool { get; set; }

    [JsonPropertyName("confirmations")]
    public ulong Confirmations { get; set; }

    public CheckTxKeyResult(ulong received, bool inPool, ulong confirmations)
    {
        Received = received;
        InPool = inPool;
        Confirmations = confirmations;
    }
}

public class GetTxProofParameters
{
    [JsonPropertyName("txid")]
    public string Txid { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    public GetTxProofParameters(string txid, string address, string message)
    {
        Txid = txid;
        Address = address;
        Message = message;
    }
}

public class GetTxProofResult
{
    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public GetTxProofResult(string signature)
    {
        Signature = signature;
    }
}

public class CheckTxProofParameters
{
    [JsonPropertyName("txid")]
    public string Txid { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public CheckTxProofParameters(string txid, string address, string message, string signature)
    {
        Txid = txid;
        Address = address;
        Message = message;
        Signature = signature;
    }
}

public class CheckTxProofResult
{
    [JsonPropertyName("good")]
    public bool Good { get; set; }

    [JsonPropertyName("received")]
    public ulong Received { get; set; }

    [JsonPropertyName("in_pool")]
    public bool InPool { get; set; }

    [JsonPropertyName("confirmations")]
    public ulong Confirmations { get; set; }

    public CheckTxProofResult(bool good, ulong received, bool inPool, ulong confirmations)
    {
        Good = good;
        Received = received;
        InPool = inPool;
        Confirmations = confirmations;
    }
}

public class GetSpendProofParameters
{
    [JsonPropertyName("txid")]
    public string Txid { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    public GetSpendProofParameters(string txid, string message)
    {
        Txid = txid;
        Message = message;
    }
}

public class GetSpendProofResult
{
    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public GetSpendProofResult(string signature)
    {
        Signature = signature;
    }
}

public class CheckSpendProofParameters
{
    [JsonPropertyName("txid")]
    public string Txid { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public CheckSpendProofParameters(string txid, string message, string signature)
    {
        Txid = txid;
        Message = message;
        Signature = signature;
    }
}

public class CheckSpendProofResult
{
    [JsonPropertyName("good")]
    public bool Good { get; set; }

    public CheckSpendProofResult(bool good)
    {
        Good = good;
    }
}

public class GetReserveProofParameters
{
    [JsonPropertyName("all")]
    public bool All { get; set; }

    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    public GetReserveProofParameters(bool all, uint accountIndex, ulong amount, string message)
    {
        All = all;
        AccountIndex = accountIndex;
        Amount = amount;
        Message = message;
    }
}

public class GetReserveProofResult
{
    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public GetReserveProofResult(string signature)
    {
        Signature = signature;
    }
}

public class CheckReserveProofParameters
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public CheckReserveProofParameters(string address, string message, string signature)
    {
        Address = address;
        Message = message;
        Signature = signature;
    }
}

public class CheckReserveProofResult
{
    [JsonPropertyName("good")]
    public bool Good { get; set; }

    [JsonPropertyName("total")]
    public ulong Total { get; set; }

    [JsonPropertyName("spent")]
    public ulong Spent { get; set; }

    public CheckReserveProofResult(bool good, ulong total, ulong spent)
    {
        Good = good;
        Total = total;
        Spent = spent;
    }
}

public class GetTransfersParameters
{
    [JsonPropertyName("in")]
    public bool In { get; set; }

    [JsonPropertyName("out")]
    public bool Out { get; set; }

    [JsonPropertyName("pending")]
    public bool Pending { get; set; }

    [JsonPropertyName("failed")]
    public bool Failed { get; set; }

    [JsonPropertyName("pool")]
    public bool Pool { get; set; }

    [JsonPropertyName("filter_by_height")]
    public bool FilterByHeight { get; set; }

    [JsonPropertyName("min_height")]
    public ulong MinHeight { get; set; }

    [JsonPropertyName("max_height")]
    public ulong MaxHeight { get; set; }

    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("subaddr_indices")]
    public HashSet<uint>? SubaddrIndices { get; set; }

    [JsonPropertyName("all_accounts")]
    public bool AllAccounts { get; set; }

    public GetTransfersParameters(bool @in, bool @out, bool pending, bool failed, bool pool, bool filterByHeight, ulong minHeight, ulong maxHeight, uint accountIndex, HashSet<uint>? subaddrIndices, bool allAccounts)
    {
        In = @in;
        Out = @out;
        Pending = pending;
        Failed = failed;
        Pool = pool;
        FilterByHeight = filterByHeight;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
        AccountIndex = accountIndex;
        SubaddrIndices = subaddrIndices;
        AllAccounts = allAccounts;
    }
}

public class GetTransfersResult
{
    [JsonPropertyName("in")]
    public List<TransferEntry>? In { get; set; }

    [JsonPropertyName("out")]
    public List<TransferEntry>? Out { get; set; }

    [JsonPropertyName("pending")]
    public List<TransferEntry>? Pending { get; set; }

    [JsonPropertyName("failed")]
    public List<TransferEntry>? Failed { get; set; }

    [JsonPropertyName("pool")]
    public List<TransferEntry>? Pool { get; set; }

    public GetTransfersResult(List<TransferEntry>? @in, List<TransferEntry>? @out, List<TransferEntry>? pending, List<TransferEntry>? failed, List<TransferEntry>? pool)
    {
        In = @in;
        Out = @out;
        Pending = pending;
        Failed = failed;
        Pool = pool;
    }
}

public class GetTransferByTxidParameters
{
    [JsonPropertyName("txid")]
    public string Txid { get; set; }

    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    public GetTransferByTxidParameters(string txid, uint accountIndex)
    {
        Txid = txid;
        AccountIndex = accountIndex;
    }
}

public class GetTransferByTxidResult
{
    [JsonPropertyName("transfer")]
    public TransferEntry Transfer { get; set; }

    [JsonPropertyName("transfers")]
    public List<TransferEntry>? Transfers { get; set; }

    public GetTransferByTxidResult(TransferEntry transfer, List<TransferEntry>? transfers)
    {
        Transfer = transfer;
        Transfers = transfers;
    }
}

public class SignParameters
{
    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("account_index")]
    public uint AccountIndex { get; set; }

    [JsonPropertyName("address_index")]
    public uint AddressIndex { get; set; }

    [JsonPropertyName("signature_type")]
    public string SignatureType { get; set; }

    public SignParameters(string data, uint accountIndex, uint addressIndex, string signatureType)
    {
        Data = data;
        AccountIndex = accountIndex;
        AddressIndex = addressIndex;
        SignatureType = signatureType;
    }
}

public class SignResult
{
    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public SignResult(string signature)
    {
        Signature = signature;
    }
}

public class VerifyParameters
{
    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; }

    public VerifyParameters(string data, string address, string signature)
    {
        Data = data;
        Address = address;
        Signature = signature;
    }
}

public class VerifyResult
{
    [JsonPropertyName("good")]
    public bool Good { get; set; }

    [JsonPropertyName("version")]
    public uint Version { get; set; }

    [JsonPropertyName("old")]
    public bool Old { get; set; }

    [JsonPropertyName("signature_type")]
    public string SignatureType { get; set; }

    public VerifyResult(bool good, uint version, bool old, string signatureType)
    {
        Good = good;
        Version = version;
        Old = old;
        SignatureType = signatureType;
    }
}

public class ExportOutputsParameters
{
    [JsonPropertyName("all")]
    public bool All { get; set; }

    public ExportOutputsParameters(bool all)
    {
        All = all;
    }
}

public class ExportOutputsResult
{
    [JsonPropertyName("outputs_data_hex")]
    public string OutputsDataHex { get; set; }

    public ExportOutputsResult(string outputsDataHex)
    {
        OutputsDataHex = outputsDataHex;
    }
}

public class ImportOutputsParameters
{
    [JsonPropertyName("outputs_data_hex")]
    public string OutputsDataHex { get; set; }

    public ImportOutputsParameters(string outputsDataHex)
    {
        OutputsDataHex = outputsDataHex;
    }
}

public class ImportOutputsResult
{
    [JsonPropertyName("num_imported")]
    public ulong NumImported { get; set; }

    public ImportOutputsResult(ulong numImported)
    {
        NumImported = numImported;
    }
}

public class ExportKeyImagesParameters
{
    [JsonPropertyName("all")]
    public bool All { get; set; }

    public ExportKeyImagesParameters(bool all)
    {
        All = all;
    }
}

public class ExportKeyImagesResult
{
    [JsonPropertyName("offset")]
    public uint Offset { get; set; }

    [JsonPropertyName("signed_key_images")]
    public List<SignedKeyImage>? SignedKeyImages { get; set; }

    public ExportKeyImagesResult(uint offset, List<SignedKeyImage>? signedKeyImages)
    {
        Offset = offset;
        SignedKeyImages = signedKeyImages;
    }
}

public class ImportKeyImagesParameters
{
    [JsonPropertyName("offset")]
    public uint Offset { get; set; }

    [JsonPropertyName("signed_key_images")]
    public List<SignedKeyImage>? SignedKeyImages { get; set; }

    public ImportKeyImagesParameters(uint offset, List<SignedKeyImage>? signedKeyImages)
    {
        Offset = offset;
        SignedKeyImages = signedKeyImages;
    }
}

public class ImportKeyImagesResult
{
    [JsonPropertyName("height")]
    public ulong Height { get; set; }

    [JsonPropertyName("spent")]
    public ulong Spent { get; set; }

    [JsonPropertyName("unspent")]
    public ulong Unspent { get; set; }

    public ImportKeyImagesResult(ulong height, ulong spent, ulong unspent)
    {
        Height = height;
        Spent = spent;
        Unspent = unspent;
    }
}

public class MakeUriParameters
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; set; }

    [JsonPropertyName("tx_description")]
    public string TxDescription { get; set; }

    [JsonPropertyName("recipient_name")]
    public string RecipientName { get; set; }

    public MakeUriParameters(string address, string paymentId, ulong amount, string txDescription, string recipientName)
    {
        Address = address;
        PaymentId = paymentId;
        Amount = amount;
        TxDescription = txDescription;
        RecipientName = recipientName;
    }
}

public class MakeUriResult
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; }

    public MakeUriResult(string uri)
    {
        Uri = uri;
    }
}

public class ParseUriParameters
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; }

    public ParseUriParameters(string uri)
    {
        Uri = uri;
    }
}

public class ParseUriResult
{
    [JsonPropertyName("uri")]
    public UriSpec Uri { get; set; }

    [JsonPropertyName("unknown_parameters")]
    public List<string>? UnknownParameters { get; set; }

    public ParseUriResult(UriSpec uri, List<string>? unknownParameters)
    {
        Uri = uri;
        UnknownParameters = unknownParameters;
    }
}

public class AddAddressBookEntryParameters
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    public AddAddressBookEntryParameters(string address, string description)
    {
        Address = address;
        Description = description;
    }
}

public class AddAddressBookEntryResult
{
    [JsonPropertyName("index")]
    public ulong Index { get; set; }

    public AddAddressBookEntryResult(ulong index)
    {
        Index = index;
    }
}

public class EditAddressBookEntryParameters
{
    [JsonPropertyName("index")]
    public ulong Index { get; set; }

    [JsonPropertyName("set_address")]
    public bool SetAddress { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("set_description")]
    public bool SetDescription { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    public EditAddressBookEntryParameters(ulong index, bool setAddress, string address, bool setDescription, string description)
    {
        Index = index;
        SetAddress = setAddress;
        Address = address;
        SetDescription = setDescription;
        Description = description;
    }
}

public class EditAddressBookEntryResult
{
    public EditAddressBookEntryResult()
    {
    }
}

public class GetAddressBookEntryParameters
{
    [JsonPropertyName("entries")]
    public List<ulong>? Entries { get; set; }

    public GetAddressBookEntryParameters(List<ulong>? entries)
    {
        Entries = entries;
    }
}

public class GetAddressBookEntryResult
{
    [JsonPropertyName("entries")]
    public List<Entry>? Entries { get; set; }

    public GetAddressBookEntryResult(List<Entry>? entries)
    {
        Entries = entries;
    }
}

public class DeleteAddressBookEntryParameters
{
    [JsonPropertyName("index")]
    public ulong Index { get; set; }

    public DeleteAddressBookEntryParameters(ulong index)
    {
        Index = index;
    }
}

public class DeleteAddressBookEntryResult
{
    public DeleteAddressBookEntryResult()
    {
    }
}

public class RescanSpentParameters
{
    public RescanSpentParameters()
    {
    }
}

public class RescanSpentResult
{
    public RescanSpentResult()
    {
    }
}

public class RefreshParameters
{
    [JsonPropertyName("start_height")]
    public ulong StartHeight { get; set; }

    public RefreshParameters(ulong startHeight)
    {
        StartHeight = startHeight;
    }
}

public class RefreshResult
{
    [JsonPropertyName("blocks_fetched")]
    public ulong BlocksFetched { get; set; }

    [JsonPropertyName("received_money")]
    public bool ReceivedMoney { get; set; }

    public RefreshResult(ulong blocksFetched, bool receivedMoney)
    {
        BlocksFetched = blocksFetched;
        ReceivedMoney = receivedMoney;
    }
}

public class AutoRefreshParameters
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("period")]
    public uint Period { get; set; }

    public AutoRefreshParameters(bool enable, uint period)
    {
        Enable = enable;
        Period = period;
    }
}

public class AutoRefreshResult
{
    public AutoRefreshResult()
    {
    }
}

public class ScanTxParameters
{
    [JsonPropertyName("txids")]
    public List<string>? Txids { get; set; }

    public ScanTxParameters(List<string>? txids)
    {
        Txids = txids;
    }
}

public class ScanTxResult
{
    public ScanTxResult()
    {
    }
}

public class StartMiningParameters
{
    [JsonPropertyName("threads_count")]
    public ulong ThreadsCount { get; set; }

    [JsonPropertyName("do_background_mining")]
    public bool DoBackgroundMining { get; set; }

    [JsonPropertyName("ignore_battery")]
    public bool IgnoreBattery { get; set; }

    public StartMiningParameters(ulong threadsCount, bool doBackgroundMining, bool ignoreBattery)
    {
        ThreadsCount = threadsCount;
        DoBackgroundMining = doBackgroundMining;
        IgnoreBattery = ignoreBattery;
    }
}

public class StartMiningResult
{
    public StartMiningResult()
    {
    }
}

public class StopMiningParameters
{
    public StopMiningParameters()
    {
    }
}

public class StopMiningResult
{
    public StopMiningResult()
    {
    }
}

public class GetLanguagesParameters
{
    public GetLanguagesParameters()
    {
    }
}

public class GetLanguagesResult
{
    [JsonPropertyName("languages")]
    public List<string>? Languages { get; set; }

    [JsonPropertyName("languages_local")]
    public List<string>? LanguagesLocal { get; set; }

    public GetLanguagesResult(List<string>? languages, List<string>? languagesLocal)
    {
        Languages = languages;
        LanguagesLocal = languagesLocal;
    }
}

public class CreateWalletParameters
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    public CreateWalletParameters(string filename, string password, string language)
    {
        Filename = filename;
        Password = password;
        Language = language;
    }
}

public class CreateWalletResult
{
    public CreateWalletResult()
    {
    }
}

public class OpenWalletParameters
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("autosave_current")]
    public bool AutosaveCurrent { get; set; }

    public OpenWalletParameters(string filename, string password, bool autosaveCurrent)
    {
        Filename = filename;
        Password = password;
        AutosaveCurrent = autosaveCurrent;
    }
}

public class OpenWalletResult
{
    public OpenWalletResult()
    {
    }
}

public class CloseWalletParameters
{
    [JsonPropertyName("autosave_current")]
    public bool AutosaveCurrent { get; set; }

    public CloseWalletParameters(bool autosaveCurrent)
    {
        AutosaveCurrent = autosaveCurrent;
    }
}

public class CloseWalletResult
{
    public CloseWalletResult()
    {
    }
}

public class ChangeWalletPasswordParameters
{
    [JsonPropertyName("old_password")]
    public string OldPassword { get; set; }

    [JsonPropertyName("new_password")]
    public string NewPassword { get; set; }

    public ChangeWalletPasswordParameters(string oldPassword, string newPassword)
    {
        OldPassword = oldPassword;
        NewPassword = newPassword;
    }
}

public class ChangeWalletPasswordResult
{
    public ChangeWalletPasswordResult()
    {
    }
}

public class GenerateFromKeysParameters
{
    public GenerateFromKeysParameters()
    {
    }
}

public class GenerateFromKeysResult
{
    public GenerateFromKeysResult()
    {
    }
}

public class RestoreDeterministicWalletParameters
{
    [JsonPropertyName("restore_height")]
    public ulong RestoreHeight { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    [JsonPropertyName("seed")]
    public string Seed { get; set; }

    [JsonPropertyName("seed_offset")]
    public string SeedOffset { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("autosave_current")]
    public bool AutosaveCurrent { get; set; }

    public RestoreDeterministicWalletParameters(ulong restoreHeight, string filename, string seed, string seedOffset, string password, string language, bool autosaveCurrent)
    {
        RestoreHeight = restoreHeight;
        Filename = filename;
        Seed = seed;
        SeedOffset = seedOffset;
        Password = password;
        Language = language;
        AutosaveCurrent = autosaveCurrent;
    }
}

public class RestoreDeterministicWalletResult
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("seed")]
    public string Seed { get; set; }

    [JsonPropertyName("info")]
    public string Info { get; set; }

    [JsonPropertyName("was_deprecated")]
    public bool WasDeprecated { get; set; }

    public RestoreDeterministicWalletResult(string address, string seed, string info, bool wasDeprecated)
    {
        Address = address;
        Seed = seed;
        Info = info;
        WasDeprecated = wasDeprecated;
    }
}

public class IsMultisigParameters
{
    public IsMultisigParameters()
    {
    }
}

public class IsMultisigResult
{
    [JsonPropertyName("multisig")]
    public bool Multisig { get; set; }

    [JsonPropertyName("ready")]
    public bool Ready { get; set; }

    [JsonPropertyName("threshold")]
    public uint Threshold { get; set; }

    [JsonPropertyName("total")]
    public uint Total { get; set; }

    public IsMultisigResult(bool multisig, bool ready, uint threshold, uint total)
    {
        Multisig = multisig;
        Ready = ready;
        Threshold = threshold;
        Total = total;
    }
}

public class PrepareMultisigParameters
{
    public PrepareMultisigParameters()
    {
    }
}

public class PrepareMultisigResult
{
    [JsonPropertyName("multisig_info")]
    public string MultisigInfo { get; set; }

    public PrepareMultisigResult(string multisigInfo)
    {
        MultisigInfo = multisigInfo;
    }
}

public class MakeMultisigParameters
{
    [JsonPropertyName("multisig_info")]
    public List<string>? MultisigInfo { get; set; }

    [JsonPropertyName("threshold")]
    public uint Threshold { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    public MakeMultisigParameters(List<string>? multisigInfo, uint threshold, string password)
    {
        MultisigInfo = multisigInfo;
        Threshold = threshold;
        Password = password;
    }
}

public class MakeMultisigResult
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("multisig_info")]
    public string MultisigInfo { get; set; }

    public MakeMultisigResult(string address, string multisigInfo)
    {
        Address = address;
        MultisigInfo = multisigInfo;
    }
}

public class ExportMultisigParameters
{
    public ExportMultisigParameters()
    {
    }
}

public class ExportMultisigResult
{
    [JsonPropertyName("info")]
    public string Info { get; set; }

    public ExportMultisigResult(string info)
    {
        Info = info;
    }
}

public class ImportMultisigParameters
{
    [JsonPropertyName("info")]
    public List<string>? Info { get; set; }

    public ImportMultisigParameters(List<string>? info)
    {
        Info = info;
    }
}

public class ImportMultisigResult
{
    [JsonPropertyName("n_outputs")]
    public ulong NOutputs { get; set; }

    public ImportMultisigResult(ulong nOutputs)
    {
        NOutputs = nOutputs;
    }
}

public class FinalizeMultisigParameters
{
    public FinalizeMultisigParameters()
    {
    }
}

public class FinalizeMultisigResult
{
    public FinalizeMultisigResult()
    {
    }
}

public class ExchangeMultisigKeysParameters
{
    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("multisig_info")]
    public List<string>? MultisigInfo { get; set; }

    public ExchangeMultisigKeysParameters(string password, List<string>? multisigInfo)
    {
        Password = password;
        MultisigInfo = multisigInfo;
    }
}

public class ExchangeMultisigKeysResult
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("multisig_info")]
    public string MultisigInfo { get; set; }

    public ExchangeMultisigKeysResult(string address, string multisigInfo)
    {
        Address = address;
        MultisigInfo = multisigInfo;
    }
}

public class SignMultisigParameters
{
    [JsonPropertyName("tx_data_hex")]
    public string TxDataHex { get; set; }

    public SignMultisigParameters(string txDataHex)
    {
        TxDataHex = txDataHex;
    }
}

public class SignMultisigResult
{
    [JsonPropertyName("tx_data_hex")]
    public string TxDataHex { get; set; }

    [JsonPropertyName("tx_hash_list")]
    public List<string>? TxHashList { get; set; }

    public SignMultisigResult(string txDataHex, List<string>? txHashList)
    {
        TxDataHex = txDataHex;
        TxHashList = txHashList;
    }
}

public class SubmitMultisigParameters
{
    [JsonPropertyName("tx_data_hex")]
    public string TxDataHex { get; set; }

    public SubmitMultisigParameters(string txDataHex)
    {
        TxDataHex = txDataHex;
    }
}

public class SubmitMultisigResult
{
    [JsonPropertyName("tx_hash_list")]
    public List<string>? TxHashList { get; set; }

    public SubmitMultisigResult(List<string>? txHashList)
    {
        TxHashList = txHashList;
    }
}

public class GetVersionParameters
{
    public GetVersionParameters()
    {
    }
}

public class GetVersionResult
{
    [JsonPropertyName("version")]
    public uint Version { get; set; }

    [JsonPropertyName("release")]
    public bool Release { get; set; }

    public GetVersionResult(uint version, bool release)
    {
        Version = version;
        Release = release;
    }
}

public class ValidateAddressParameters
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("any_net_type")]
    public bool AnyNetType { get; set; }

    [JsonPropertyName("allow_openalias")]
    public bool AllowOpenalias { get; set; }

    public ValidateAddressParameters(string address, bool anyNetType, bool allowOpenalias)
    {
        Address = address;
        AnyNetType = anyNetType;
        AllowOpenalias = allowOpenalias;
    }
}

public class ValidateAddressResult
{
    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("integrated")]
    public bool Integrated { get; set; }

    [JsonPropertyName("subaddress")]
    public bool Subaddress { get; set; }

    [JsonPropertyName("nettype")]
    public string Nettype { get; set; }

    [JsonPropertyName("openalias_address")]
    public string OpenaliasAddress { get; set; }

    public ValidateAddressResult(bool valid, bool integrated, bool subaddress, string nettype, string openaliasAddress)
    {
        Valid = valid;
        Integrated = integrated;
        Subaddress = subaddress;
        Nettype = nettype;
        OpenaliasAddress = openaliasAddress;
    }
}

public class SetDaemonParameters
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("trusted")]
    public bool Trusted { get; set; }

    [JsonPropertyName("ssl_support")]
    public string SslSupport { get; set; }

    [JsonPropertyName("ssl_private_key_path")]
    public string SslPrivateKeyPath { get; set; }

    [JsonPropertyName("ssl_certificate_path")]
    public string SslCertificatePath { get; set; }

    [JsonPropertyName("ssl_ca_file")]
    public string SslCaFile { get; set; }

    [JsonPropertyName("ssl_allowed_fingerprints")]
    public List<string>? SslAllowedFingerprints { get; set; }

    [JsonPropertyName("ssl_allow_any_cert")]
    public bool SslAllowAnyCert { get; set; }

    public SetDaemonParameters(string address, string username, string password, bool trusted, string sslSupport, string sslPrivateKeyPath, string sslCertificatePath, string sslCaFile, List<string>? sslAllowedFingerprints, bool sslAllowAnyCert)
    {
        Address = address;
        Username = username;
        Password = password;
        Trusted = trusted;
        SslSupport = sslSupport;
        SslPrivateKeyPath = sslPrivateKeyPath;
        SslCertificatePath = sslCertificatePath;
        SslCaFile = sslCaFile;
        SslAllowedFingerprints = sslAllowedFingerprints;
        SslAllowAnyCert = sslAllowAnyCert;
    }
}

public class SetDaemonResult
{
    public SetDaemonResult()
    {
    }
}

public class SetLogLevelParameters
{
    [JsonPropertyName("level")]
    public byte Level { get; set; }

    public SetLogLevelParameters(byte level)
    {
        Level = level;
    }
}

public class SetLogLevelResult
{
    public SetLogLevelResult()
    {
    }
}

public class SetLogCategoriesParameters
{
    [JsonPropertyName("categories")]
    public string Categories { get; set; }

    public SetLogCategoriesParameters(string categories)
    {
        Categories = categories;
    }
}

public class SetLogCategoriesResult
{
    [JsonPropertyName("categories")]
    public string Categories { get; set; }

    public SetLogCategoriesResult(string categories)
    {
        Categories = categories;
    }
}

public class EstimateTxSizeAndWeightParameters
{
    [JsonPropertyName("n_inputs")]
    public uint NInputs { get; set; }

    [JsonPropertyName("n_outputs")]
    public uint NOutputs { get; set; }

    [JsonPropertyName("ring_size")]
    public uint RingSize { get; set; }

    [JsonPropertyName("rct")]
    public bool Rct { get; set; }

    public EstimateTxSizeAndWeightParameters(uint nInputs, uint nOutputs, uint ringSize, bool rct)
    {
        NInputs = nInputs;
        NOutputs = nOutputs;
        RingSize = ringSize;
        Rct = rct;
    }
}

public class EstimateTxSizeAndWeightResult
{
    [JsonPropertyName("size")]
    public ulong Size { get; set; }

    [JsonPropertyName("weight")]
    public ulong Weight { get; set; }

    public EstimateTxSizeAndWeightResult(ulong size, ulong weight)
    {
        Size = size;
        Weight = weight;
    }
}

#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1201 // Elements should appear in the correct order
#nullable disable
