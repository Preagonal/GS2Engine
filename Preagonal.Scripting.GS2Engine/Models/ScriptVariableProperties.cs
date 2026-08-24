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
				{ "joinedclasses", "The names of the classes joined to this object.", variable => variable.JoinedClasses, (variable, value) => variable.JoinedClasses = value },
			}
		);

		AddFunctions(
			this,
			new()
			{
				{
					"join",
					"Joins a class script to this object.",
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
					},
					[new("className", typeof(string))]
				},
			}
		);

		Compile();
	}
}
