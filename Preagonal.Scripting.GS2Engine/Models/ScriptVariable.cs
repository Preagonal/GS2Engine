using System;
using System.Collections.Generic;
using System.Linq;
using Preagonal.Scripting.GS2Engine.Extensions;

namespace Preagonal.Scripting.GS2Engine.Models;

public class ScriptVariable : VariableCollection, IScriptVariable
{
	private readonly List<string> _joinedClasses = [];

	public ScriptVariable(string name = "")
	{
		Name = name;
	}

	public string Name { get; protected set; }
	public IReadOnlyList<string> JoinedClassNames => _joinedClasses;

	public string JoinedClasses
	{
		get => string.Join(",", _joinedClasses);
		set
		{
			_joinedClasses.Clear();
			foreach (var className in value.TokenizeForScript(" ,"))
				Join(className);
		}
	}

	public static readonly ScriptVariableProperties PropertiesInstance = [];
	public virtual         IScriptProperties        Properties => PropertiesInstance;

	public void Join(string className)
	{
		var normalizedClassName = className.Trim().ToLowerInvariant();
		if (string.IsNullOrEmpty(normalizedClassName) || _joinedClasses.Contains(normalizedClassName)) return;

		_joinedClasses.Add(normalizedClassName);
	}

	protected void SetCallback(string variable, CallbackDelegate setCallback)
	{
		Properties.FirstOrDefault(x => x.PropertyName.Equals(variable, StringComparison.CurrentCultureIgnoreCase))?.SetCallback(setCallback);
	}

	/*
	protected void GetCallback(TString variable, VariableCollectionGetCallback getCallback)
	{
		if (!ContainsVariable(variable))
			_collection.Add(variable, 0.ToStackEntry());

		_collection[variable].GetCallback(getCallback);
	}
	*/
}
