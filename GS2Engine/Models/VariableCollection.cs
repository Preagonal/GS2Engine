using System.Collections.Generic;
using GS2Engine.Extensions;

namespace GS2Engine.Models;

public class VariableCollection
{
	public delegate void VariableCollectionCallback(object? value);

	private readonly Dictionary<string, IStackEntry>                _collection = new();
	private readonly Dictionary<string, VariableCollectionCallback> _callbacks  = new();

	public VariableCollection()
	{
	}

	public VariableCollection(IDictionary<string, IStackEntry>? collection) => AddOrUpdate(collection);

	public IStackEntry this[TString key]
	{
		get => GetVariable(key);
		set => SetVariable(key, value);
	}

	public IStackEntry GetVariable(TString variable) =>
		_collection.TryGetValue(variable, out var entry)
			? entry
			: SetVariable(variable, "".ToStackEntry());

	public void Clear() => _collection.Clear();

	public IStackEntry AddOrUpdate(TString variable, IStackEntry value, bool skipCallback = false)
	{
		if (ContainsVariable(variable))
			_collection[variable].SetValue(value.GetValue(), skipCallback);
		else
			_collection.Add(variable, value);

		return _collection[variable];
	}

	protected void SetCallback(TString variable, VariableCollectionCallback callback)
	{
		if (!ContainsVariable(variable))
			_collection.Add(variable, 0.ToStackEntry());

		_collection[variable].SetCallback(callback);
	}

	public IStackEntry SetVariable(TString variable, IStackEntry value) => AddOrUpdate(variable, value);

	public bool ContainsVariable(TString variable) => _collection.ContainsKey(variable.ToString());

	public void AddOrUpdate(IDictionary<string, IStackEntry>? collection)
	{
		if (collection == null) return;
		foreach (var variable in collection)
			AddOrUpdate(variable.Key, variable.Value);
	}

	public IDictionary<string, IStackEntry> GetDictionary() => _collection;

	public void AddOrUpdate(VariableCollection? collection)
	{
		if (collection != null)
			AddOrUpdate(collection.GetDictionary());
	}
}