using System.Collections.Generic;
using GS2Engine.Extensions;

namespace GS2Engine.Models
{
	public class VariableCollection
	{
		private readonly Dictionary<string?, IStackEntry> _collection = new();

		public VariableCollection() {}
		public VariableCollection(IDictionary<string?, IStackEntry> collection) => AddOrUpdate(collection);

		public IStackEntry GetVariable(TString? variable)
		{
			return _collection.TryGetValue(variable, out IStackEntry? entry) ? entry : SetVariable(variable, "".ToStackEntry());
		}

		public void        Clear()                        => _collection.Clear();

		public IStackEntry AddOrUpdate(TString variable, IStackEntry value)
		{
			if (ContainsVariable(variable))
				_collection[variable].SetValue(value.GetValue());
			else
				_collection.Add(variable, value);

			return _collection[variable];
		}
		
		public IStackEntry this[TString key]
		{
			get => GetVariable(key);
			set => SetVariable(key, value);
		}

		public IStackEntry SetVariable(TString variable, IStackEntry value) => AddOrUpdate(variable, value);

		public bool ContainsVariable(TString variable) => _collection.ContainsKey(variable.ToString());

		public void AddOrUpdate(ICollection<KeyValuePair<string?,IStackEntry>> collection)
		{
			foreach (KeyValuePair<string,IStackEntry> variable in collection) AddOrUpdate(variable.Key, variable.Value);
		}

		public IDictionary<string?,IStackEntry> GetDictionary() => _collection;
	}
}