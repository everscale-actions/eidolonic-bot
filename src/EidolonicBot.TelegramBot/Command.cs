namespace EidolonicBot;

public enum Command {
    Unknown,

    [Command("/help",
        Description = "Show this help")]
    Help,

    [Command("/wallet",
        Description = "Wallet address and balance",
        IsWalletNeeded = true)]
    Wallet,

    [Command("/send",
        Description = "Sends tokens from to another user",
        Help = "Sends tokens from your wallet to user that you reply to or special address\n" +
               " Usage: `/send amount `\\[address]\n" +
               "  amount - minimum 0.1 or all to send the whole balance\n" +
               "  address - get some coins for withdrawal",
        IsWalletNeeded = true)]
    Send,

    [Command("/subscription",
        Description = "Subscribe to transaction of address",
        Help = " Usage:\n" +
               "  `/subscription list`\n" +
               "  `/subscription add `address\n" +
               "  `/subscription remove `address\n" +
               " [*] only chat admins can control subscriptions")]
    Subscription
}
