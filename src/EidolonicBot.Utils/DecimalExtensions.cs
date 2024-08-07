using System.Globalization;

namespace EidolonicBot;

public static class DecimalExtensions {
  public static string ToEvers(this decimal value) {
    var evers = Math.Abs(value) switch {
      0 => '0' + Constants.Currency,
      < 0.01m => value.ToString(CultureInfo.InvariantCulture) + Constants.Currency,
      _ => value.ToString("#,0.00").Replace(",", " ") + Constants.Currency
    };
    return evers.ToEscapedMarkdownV2();
  }
}
