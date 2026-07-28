using System;
using Preagonal.Scripting.GS2Engine.GS2.Script;

namespace Preagonal.Scripting.GS2Engine.Models.Properties;

public readonly struct FunctionDefinition<TInstance, TRet>(string propertyName, string description, PropertyFunctionDelegate<TInstance, TRet>? callTyped = null) : IFunctionDefinition<TInstance>
{
	public  string                                     PropertyName { get; init; } = propertyName;
	public  string                                     Description  { get; init; } = description;
	private PropertyFunctionDelegate<TInstance, TRet>? CallTyped    { get; init; } = callTyped;
	public  Type                                       ReturnType   => typeof(TRet);

	object? IFunctionDefinition<TInstance>.Call(TInstance instance, ScriptMachine? machine, params IStackEntry[] value)
	{
		if (CallTyped is null) return null;
		// You can choose how strict you want this cast to be:
		// direct cast if you trust callers:
		// WriteTyped(instance, (TRet)value!);

		// or a safer conversion path:
		var ret = CallTyped(instance, value);
		var v   = ret != null ? ret : (TRet)Convert.ChangeType(ret!, typeof(TRet));
		return v;
	}
}

public readonly struct ContextualFunctionDefinition<TInstance, TRet>(
	string propertyName,
	string description,
	ContextualPropertyFunctionDelegate<TInstance, TRet> callTyped
) : IFunctionDefinition<TInstance>
{
	public string PropertyName { get; } = propertyName;
	public string Description { get; } = description;
	public Type ReturnType => typeof(TRet);

	object? IFunctionDefinition<TInstance>.Call(TInstance instance, ScriptMachine? machine, params IStackEntry[] arguments) =>
		machine == null ? null : callTyped(instance, machine, arguments);
}
