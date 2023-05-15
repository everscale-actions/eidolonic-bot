namespace EidolonicBot;

public enum Command {
    Unknown,

    [Command("/help",
        Description = "Show this help",
        IsBotInitCommand = true)]
    Help,

    [Command("/wallet",
        Description = "Wallet address and balance",
        IsWalletNeeded = true,
        IsBotInitCommand = true)]
    Wallet,

    [Command("/send",
        Description = "Sends tokens to another telegram user that you reply to",
        IsWalletNeeded = true,
        IsBotInitCommand = true)]
    [CommandArg("amount", "minimum 0.1 or all to send the whole balance")]
    Send,

    [Command("/withdrawal",
        Description = "Withdraw tokens to address",
        IsWalletNeeded = true,
        IsBotInitCommand = true)]
    [CommandArg("amount", "minimum 0.1 or all to send the whole balance")]
    [CommandArg("address", "you outer address or address of the recipient", "amount")]
    [CommandArg("memo", "(optional) e.g. `user-id` for exchanges", "amount address")]
    Withdraw,

    [Command("/subscription",
        Description = "Get or control list of subscriptions",
        IsBotInitCommand = true)]
    [CommandArg("list", "show subscriptions for this chat")]
    [CommandArg("add", "subscribe for transactions")]
    [CommandArg("edit", "edit subscription parameters")]
    [CommandArg("remove", "unsubscribe from transactions")]
    [CommandArg("address", "account address", "add", "edit", "remove")]
    [CommandArg("full", "(optional) show full address", "list")]
    [CommandArg("min\\_delta", "(optional) minimum balance delta for notification", "add address", "edit address")]
    Subscription
}
