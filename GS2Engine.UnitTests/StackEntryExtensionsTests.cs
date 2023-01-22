using GS2Engine.Enums;
using GS2Engine.Extensions;
using GS2Engine.Models;

namespace GS2Engine.UnitTests
{
	public class StackEntryExtensionsTests
	{
		[Fact]
		public void When_input_is_number_Then_return_StackEntry_with_type_number_and_value_type_double()
		{
			//Arrange
			//
			IStackEntry test = 0.ToStackEntry();
			
			//Assert
			Assert.Equal(StackEntryType.Number, test.Type);
			Assert.Equal(typeof(double), test.GetValue()?.GetType());
		}

		[Fact]
		public void When_input_is_string_Then_return_StackEntry_with_type_string_and_value_type_TString()
		{
			//Arrange
			//
			IStackEntry test = "".ToStackEntry();
			
			//Assert
			Assert.Equal(StackEntryType.String, test.Type);
			Assert.Equal(typeof(TString), test.GetValue()?.GetType());
		}
	}
}