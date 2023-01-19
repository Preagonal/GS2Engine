using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GS2Engine.Enums;
using GS2Engine.Models;

namespace GS2Engine.Extensions
{
	public static class StackEntryExtensions
	{
		/*
		public static StackEntry ToStackEntry(this string stackObject, bool isVariable = false) =>
			new(isVariable ? StackEntryType.Variable : StackEntryType.String, (TString)stackObject);

		public static StackEntry ToStackEntry(this TString stackObject, bool isVariable = false) =>
			new(isVariable ? StackEntryType.Variable : StackEntryType.String, stackObject);

		public static StackEntry ToStackEntry(this bool stackObject) => new(StackEntryType.Boolean, stackObject);
		public static StackEntry ToStackEntry(this byte stackObject) => new(StackEntryType.Number, stackObject);
		public static StackEntry ToStackEntry(this int stackObject) => new(StackEntryType.Number, stackObject);

		public static StackEntry ToStackEntry(this float stackObject) =>
			new(StackEntryType.Number, stackObject);

		public static StackEntry ToStackEntry(this double stackObject) =>
			new(StackEntryType.Number, stackObject);

		public static StackEntry ToStackEntry(this decimal stackObject) =>
			new(StackEntryType.Number, (double)stackObject);

		public static StackEntry ToStackEntry(this short stackObject) =>
			new(StackEntryType.Number, stackObject);
*/
		public static StackEntry ToStackEntry(this object stackObject, bool isVariable = false) =>
			new(isVariable?StackEntryType.Variable:GetStackEntryType(stackObject), FixStackValue(stackObject));

		private static object? FixStackValue(object stackObject)
		{
			return stackObject  switch
			{
				string   => (TString)stackObject.ToString(),
				TString  => stackObject,
				int      => (double)(int)stackObject,
				double   => (double)stackObject,
				float    => (double)(float)stackObject,
				decimal  => (double)(decimal)stackObject,
				string[] => (string[])stackObject,
				bool     => (bool)stackObject,
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
					throw new ArgumentOutOfRangeException();
				}
					
			}
		}

		public static StackEntry ToStackEntry(this IEnumerable<string> stackObject) =>
			new(StackEntryType.Array, stackObject.ToList());

		public static StackEntry ToStackEntry(this IEnumerable<int> stackObject) =>
			new(StackEntryType.Array, stackObject.ToList());
	}
}