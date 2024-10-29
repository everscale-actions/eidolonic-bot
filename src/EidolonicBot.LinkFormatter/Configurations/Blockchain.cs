namespace EidolonicBot.Configurations;

public record BlockchainOptions {
  public required string DefaultExplorer { get; init; }
  public required IReadOnlyCollection<ExplorerOptions> Explorers { get; init; }
}
