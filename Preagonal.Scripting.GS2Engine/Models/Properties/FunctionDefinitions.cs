using System.Collections.Generic;

namespace Preagonal.Scripting.GS2Engine.Models.Properties;

public class FunctionDefinitions<TInstance> : List<IFunctionDefinition<TInstance>> where TInstance : class
{
	public void Add<TRet>(
		string propertyName,
		string description,
		PropertyFunctionDelegate<TInstance, TRet>? callTyped = null
	) =>
		Add(new FunctionDefinition<TInstance, TRet>(propertyName, description, callTyped));

	public void Add<TRet>(
		string propertyName,
		string description,
		ContextualPropertyFunctionDelegate<TInstance, TRet> callTyped
	) =>
		Add(new ContextualFunctionDefinition<TInstance, TRet>(propertyName, description, callTyped));
}
