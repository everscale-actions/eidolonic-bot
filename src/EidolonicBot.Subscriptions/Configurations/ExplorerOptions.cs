namespace EidolonicBot.Configurations;

public record ExplorerOptions {
    public string Name { get; init; } = null!;
    public string TransactionLinkTemplate { get; init; } = null!;
}
