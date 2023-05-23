namespace EidolonicBot.Models;

public record WalletInfo(string Address, decimal? Balance, TokenBalance[]? TokenBalances);
