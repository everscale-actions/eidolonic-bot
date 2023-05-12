namespace EidolonicBot.Utils;

public static class CommandExtensions {
    public static bool IsWalletNeeded(this Command command) {
        return CommandHelpers.CommandAttributeByCommand[command]?.IsWalletNeeded ?? false;
    }
}
