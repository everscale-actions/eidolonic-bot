using EidolonicBot.Attributes;

namespace EidolonicBot;

public enum Command {
    Unknown,

    [Command("/help", Description = "Show this help", IsWalletNeeded = false)]
    Help,

    [Command("/wallet", Description = "Wallet info")]
    Wallet,

    [Command("/send", Description = "Sends tokens from your wallet to user that you reply to. Usage: /send [amount]")]
    Send
}