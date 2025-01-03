﻿using System;
using GS2Engine.Enums;

namespace GS2Engine.Models;

public class StackEntry : IStackEntry
{
	internal StackEntry(StackEntryType type, object? value)
	{
		Type  = type;
		Value = value;
	}

	private object?                                           Value          { get; set; }
	private VariableCollection.VariableCollectionSetCallback? SetterCallback { get; set; }
	private VariableCollection.VariableCollectionGetCallback? GetterCallback { get; set; }
	public  StackEntryType                                    Type           { get; private set; }
	public  object?                                           GetValue()     => Value;

	public T1? GetValue<T1>()
	{
		if (TryGetValue<T1>(out var value))
		{
			return (T1?)value;
		}

		return default;
	}

	public bool TryGetValue<T>(out object? value)
	{
		try
		{
			if (GetterCallback != null)
			{
				value = GetterCallback();
			}
			else if (Value?.GetType() == typeof(T))
			{
				value = (T)Value;
			}
			else if (typeof(T) == typeof(bool))
			{
				if (Value?.GetType() == typeof(TString))
				{
					if (bool.TryParse(Value.ToString(), out var boolVar))
					{
						value = boolVar;
					}
					else
					{
						value = false;
					}
				}
				else if (Value?.GetType() == typeof(double))
				{
					value = (double)Value != 0;
				}
			}
			else if (typeof(T) == typeof(TString))
			{
				value = (TString)(Value?.ToString() ?? "");
			}

			value = (T?)Value;

			return true;
		}
		catch (Exception e)
		{
			Tools.DebugLine(e.Message);
			value = default;
			return false;
		}
	}

	public void SetCallback(VariableCollection.VariableCollectionSetCallback setCallback) => SetterCallback = setCallback;

	public void GetCallback(VariableCollection.VariableCollectionGetCallback getCallback) => GetterCallback = getCallback;

	public void SetValue(object? value, bool skipCallback = false)
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
			_        => value,
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
			_        => StackEntryType.Array,
		};

		if (SetterCallback != null && !skipCallback)
			SetterCallback(value);
	}
}