namespace Preagonal.Scripting.GS2Engine.Models;

using Preagonal.Scripting.GS2Engine.GS2.Script;

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
						{
							var className = args[0].GetValue()?.ToString() ?? string.Empty;
							variable.Join(className);
							if (variable is Script script)
								script.ScriptManager.RequestClassScript(className);
						}

						return 0;
					}
				},
			}
		);

		Compile();
	}
}
