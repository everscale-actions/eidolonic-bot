namespace EidolonicBot;

public enum Command {
    Unknown,

    [Command("/help",
        Description = "Show this help",
        IsBotInitCommand = true)]
    Help,

    [Command("/wallet",
        Description = "Wallet address and balance",
        IsInlineCommand = true,
        IsWalletNeeded = true,
        IsBotInitCommand = true)]
    Wallet,

    [Command("/send",
        Description = "Sends tokens to another telegram user that you reply to",
        IsInlineCommand = true,
        IsWalletNeeded = true)]
    [CommandArg("amount", "minimum 0.1 or all to send the whole balance")]
    Send,

    [Command("/withdrawal",
        Description = "Withdraw tokens to address",
        IsInlineCommand = true,
        IsWalletNeeded = true,
        IsBotInitCommand = true)]
    [CommandArg("amount", "minimum 0.1 or all to send the whole balance")]
    [CommandArg("address", "you outer address or address of the recipient", dependsOn: "amount")]
    [CommandArg("memo", "(optional) e.g. `user-id` for exchanges", dependsOn: "address")]
    Withdraw,

    [Command("/subscription",
        Description = "Get or control list of subscriptions",
        IsInlineCommand = true)]
    [CommandArg("list", "show subscriptions for this chat")]
    [CommandArg("add", "subscribe to transactions")]
    [CommandArg("remove", "unsubscribe from transactions")]
    [CommandArg("address", "account address", dependsOn: new[] { "add", "remove" })]
    Subscription,

    [Command("/start",
        Description = "Start bot")]
    Start
}
