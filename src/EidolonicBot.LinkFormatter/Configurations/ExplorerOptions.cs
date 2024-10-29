namespace EidolonicBot.Configurations;

// ReSharper disable once ClassNeverInstantiated.Global
public record ExplorerOptions {
  public required string Name { get; init; }
  public required string TransactionLinkTemplate { get; init; }
  public required string AccountLinkTemplate { get; init; }
}
