using System.Text.RegularExpressions;
using Serilog.Enrichers.Sensitive;

namespace EidolonicBot.Serilog;

public class RegexWithSecretMaskingOperator : RegexMaskingOperator {
    public RegexWithSecretMaskingOperator(string regexWithSecret) : base(regexWithSecret) { }

    protected override string PreprocessMask(string mask, Match match) {
        return match.Value.Replace(match.Groups["secret"].Value, mask);
    }
}
