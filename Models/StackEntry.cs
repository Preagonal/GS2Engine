using GS2Engine.Enums;

namespace GS2Engine.Models
{
	public class StackEntry : IStackEntry
	{
		internal StackEntry(StackEntryType type, object? value)
		{
			Type = type;
			Value = value;
		}

		private object? Value { get; set; }

		public StackEntryType Type       { get; private set; }
		public object?        GetValue() => Value;

		public T1?            GetValue<T1>() => (T1?)GetValue();

		public void SetValue(object? value)
		{
			Value = value switch
			{
				string   => (TString)value,
				TString  => value,
				int      => (double)value,
				double   => (double)value,
				float    => (double)value,
				decimal  => (double)value,
				string[] => (string[])value,
				bool     => (bool)value,
				_		 => value,
			};
			Type = value switch
			{
				string   => StackEntryType.String,
				TString  => StackEntryType.String,
				int      => StackEntryType.Number,
				double   => StackEntryType.Number,
				float    => StackEntryType.Number,
				decimal  => StackEntryType.Number,
				string[] => StackEntryType.Array,
				bool     => StackEntryType.Boolean,
				_		 => StackEntryType.Array,
			};

		}
	}
}