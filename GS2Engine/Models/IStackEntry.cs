using GS2Engine.Enums;

namespace GS2Engine.Models;

public interface IStackEntry
{
	public StackEntryType Type { get; }
	public object?        GetValue();
	public T?             GetValue<T>();
	void                  SetValue(object? getValue, bool skipCallback = false);
	bool                  TryGetValue<T>(out object? value);
	void                  SetCallback(VariableCollection.VariableCollectionCallback callback);
}