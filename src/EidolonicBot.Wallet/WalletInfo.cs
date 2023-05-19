using EidolonicBot.Models;

namespace EidolonicBot;

public record WalletInfo(string Address, decimal? Balance, TokenBalance[]? TokenBalances);
