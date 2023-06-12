using GS2Engine.Enums;
using GS2Engine.Extensions;
using GS2Engine.GS2.Script;
using GS2Engine.Models;

namespace GS2Engine.UnitTests
{
	public class StackExtensionsTests
	{
		[Fact]
		public void When_input_is_int_Then_return_StackEntry_with_type_number_and_value_type_double()
		{
			//Arrange
			Stack<StackEntry> stack = new();
			stack.Push("asd".ToStackEntry());
			
			//Act
			Stack<StackEntry> newStack = stack.Clone();
			
			//Assert
			Assert.Equal(stack.Peek(), newStack.Peek());
		}
	}
}