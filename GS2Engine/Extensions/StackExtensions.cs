using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace GS2Engine.Extensions;

public static class StackExtensions
{
	public static Stack<T> Clone<T>(this Stack<T> stack)
	{
		Contract.Requires(stack != null);
		return new(stack.Reverse());
	}
}