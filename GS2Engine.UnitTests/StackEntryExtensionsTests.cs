using GS2Engine.Enums;
using GS2Engine.Extensions;
using GS2Engine.Models;

namespace GS2Engine.UnitTests
{
	public class StackEntryExtensionsTests
	{
		[Fact]
		public void When_input_is_int_Then_return_StackEntry_with_type_number_and_value_type_double()
		{
			//Arrange
			const int val = 1;
			
			//Act
			IStackEntry test = val.ToStackEntry();
			
			//Assert
			Assert.Equal(StackEntryType.Number, test.Type);
			Assert.Equal(typeof(double), test.GetValue()?.GetType());
			Assert.Equal((double)val, test.GetValue());
		}
		
		[Fact]
		public void When_input_is_double_Then_return_StackEntry_with_type_number_and_value_type_double()
		{
			//Arrange
			const double val = 1.23d;
			
			//Act
			IStackEntry test = val.ToStackEntry();
			
			//Assert
			Assert.Equal(StackEntryType.Number, test.Type);
			Assert.Equal(typeof(double), test.GetValue()?.GetType());
			Assert.Equal((double)val, test.GetValue());
		}
		
		[Fact]
		public void When_input_is_decimal_Then_return_StackEntry_with_type_number_and_value_type_double()
		{
			//Arrange
			decimal val = new(1.23);
			
			//Act
			IStackEntry test = val.ToStackEntry();
			
			//Assert
			Assert.Equal(StackEntryType.Number, test.Type);
			Assert.Equal(typeof(double), test.GetValue()?.GetType());
			Assert.Equal((double)val, test.GetValue());
		}
		
		[Fact]
		public void When_input_is_float_Then_return_StackEntry_with_type_number_and_value_type_double()
		{
			//Arrange
			const float val = 1.23f;
			
			//Act
			IStackEntry test = val.ToStackEntry();
			
			//Assert
			Assert.Equal(StackEntryType.Number, test.Type);
			Assert.Equal(typeof(double), test.GetValue()?.GetType());
			Assert.Equal((double)val, test.GetValue());
		}

		[Fact]
		public void When_input_is_string_Then_return_StackEntry_with_type_string_and_value_type_TString()
		{
			//Arrange
			const string val = "test";
			
			//Act
			IStackEntry test = val.ToStackEntry();
			
			//Assert
			Assert.Equal(StackEntryType.String, test.Type);
			Assert.Equal(typeof(TString), test.GetValue()?.GetType());
			Assert.Equal(val, test.GetValue()?.ToString());
		}

		[Fact]
		public void When_input_is_string_array_Then_return_StackEntry_with_type_array_and_value_type_List_string()
		{
			//Arrange
			string[] val = {"test1","test2"};
			
			//Act
			IStackEntry test = val.ToStackEntry();
			
			//Assert
			Assert.Equal(StackEntryType.Array, test.Type);
			Assert.Equal(typeof(List<string>), test.GetValue()?.GetType());
			Assert.Equal(val, test.GetValue());
		}
	}
}