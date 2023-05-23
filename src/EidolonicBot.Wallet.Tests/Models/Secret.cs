namespace EidolonicBot.Models;

public class Secret {
    public string Phrase { get; set; } = null!;
    public KeyPair KeyPair { get; set; } = null!;
}
