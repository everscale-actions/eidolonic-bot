namespace EidolonicBot.Configurations;

public record BlockchainOptions {
    public string DefaultExplorer { get; init; } = null!;
    public ExplorerOptions[] Explorers { get; init; } = null!;
}
