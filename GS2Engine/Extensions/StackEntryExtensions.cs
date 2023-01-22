using System;
using System.Collections.Generic;
using System.Linq;
using GS2Engine.Enums;
using GS2Engine.Models;

namespace GS2Engine.Extensions
{
	public static class StackEntryExtensions
	{
		public static StackEntry ToStackEntry(this object stackObject, bool isVariable = false) =>
			new(isVariable?StackEntryType.Variable:GetStackEntryType(stackObject), FixStackValue(stackObject));

		private static object? FixStackValue(object stackObject)
		{
			return stackObject  switch
			{
				string           => (TString)stackObject.ToString(),
				TString          => stackObject,
				int i            => (double)i,
				double d         => d,
				float f          => (double)f,
				decimal o        => (double)o,
				string[] strings => strings,
				bool b           => b,
				_                => stackObject,
			};
		}

		private static StackEntryType GetStackEntryType(object stackObject)
		{
			switch (Type.GetTypeCode(stackObject.GetType()))
			{
				case TypeCode.Boolean:
					return StackEntryType.Boolean;
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.SByte:
					return StackEntryType.Number;
				case TypeCode.String:
				case TypeCode.DateTime:
					return StackEntryType.String;
				default:
				{
					if (stackObject.GetType() == typeof(TString))
						return StackEntryType.String;

					if (stackObject.GetType().IsGenericType && stackObject.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
						return StackEntryType.Array;

					throw new ArgumentOutOfRangeException();
				}
			}
		}

		public static IStackEntry ToStackEntry(this IEnumerable<string> stackObject) =>
			new StackEntry(StackEntryType.Array, stackObject.ToList());

		public static IStackEntry ToStackEntry(this IEnumerable<int> stackObject) =>
			new StackEntry(StackEntryType.Array, stackObject.ToList());
	}
}