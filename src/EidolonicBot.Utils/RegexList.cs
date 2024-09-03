using System.Text.RegularExpressions;

namespace EidolonicBot;

public abstract partial record RegexList {
  [GeneratedRegex("0:[0-9a-z]{64}", RegexOptions.Compiled)]
  public static partial Regex TvmAddressRegex();
}
