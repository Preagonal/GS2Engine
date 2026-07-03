namespace Preagonal.Scripting.GS2Engine.Models;

public class ScriptVariableProperties : ScriptProperties<ScriptVariable>
{
	public ScriptVariableProperties() : base(null)
	{
		AddProperties(
			this,
			new()
			{
				{ "joinedclasses", "", variable => variable.JoinedClasses, (variable, value) => variable.JoinedClasses = value },
			}
		);

		AddFunctions(
			this,
			new()
			{
				{
					"join",
					"",
					(variable, args) =>
					{
						if (args.Length > 0)
							variable.Join(args[0].GetValue()?.ToString() ?? string.Empty);

						return 0;
					}
				},
			}
		);

		Compile();
	}
}
