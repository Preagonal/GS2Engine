namespace Preagonal.Scripting.GS2Engine.Models;

public class TStringProperties : ScriptProperties<TString>
{
	public TStringProperties() : base(null)
	{
		AddFunctions(
			this,
			new()
			{
				{ "lower", "", (value, _) => value.ToString().ToLowerInvariant() },
				{ "lowercase", "", (value, _) => value.ToString().ToLowerInvariant() },
				{ "upper", "", (value, _) => value.ToString().ToUpperInvariant() },
				{ "uppercase", "", (value, _) => value.ToString().ToUpperInvariant() },
				{ "replace", "", ReplaceAll },
				{ "replaceall", "", ReplaceAll }
			}
		);

		Compile();
	}

	private static string ReplaceAll(TString value, IStackEntry[] args)
	{
		var text = value.ToString();
		var oldValue = args.Length > 0 ? Tools.ToScriptString(args[0].GetValue()) : string.Empty;
		var newValue = args.Length > 1 ? Tools.ToScriptString(args[1].GetValue()) : string.Empty;
		return oldValue.Length == 0 ? text : text.Replace(oldValue, newValue, System.StringComparison.Ordinal);
	}
}
