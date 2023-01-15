using System.Diagnostics.Contracts;

namespace GS2Engine
{
	public static class StackExtensions
	{
		public static Stack<T> Clone<T>(this Stack<T> stack)
		{
			Contract.Requires(stack != null);
			return new(stack.Reverse());
		}
	}
}