using Preagonal.Scripting.GS2Engine.Models;

namespace Preagonal.Scripting.GS2Engine.UnitTests;

public class GuiControlTests
{
	[Fact]
	public void Given_child_controls_When_clearing_controls_Then_children_are_destroyed_without_modifying_enumeration()
	{
		var parent = new GuiControl("parent", null);
		var child = new GuiControl("child", null);
		parent.AddControl(child);

		parent.ClearControls();

		Assert.Empty(parent.Controls);
		Assert.False(child.Active);
		Assert.Null(child.Parent);
	}

	[Fact]
	public void Given_gui_control_When_reading_objecttype_property_Then_control_type_name_is_returned()
	{
		var control = new GuiControl("control", null);

		var objectType = control.Properties.Single(property => !property.IsFunction && property.PropertyName == "objecttype");

		Assert.Equal("GuiControl", objectType.Read(control));
	}

	[Fact]
	public void Given_gui_control_When_calling_objecttype_function_Then_control_type_name_is_returned()
	{
		var control = new GuiControl("control", null);

		var objectType = control.Properties.Single(property => property.IsFunction && property.PropertyName == "objecttype");

		Assert.Equal("GuiControl", objectType.Call(control));
	}

	[Fact]
	public void Given_readable_gui_control_property_When_called_Then_read_value_is_returned()
	{
		var control = new GuiControl("control", null);

		var objectType = control.Properties.Single(property => !property.IsFunction && property.PropertyName == "objecttype");

		Assert.Equal("GuiControl", objectType.Call(control));
	}

	[Fact]
	public void Given_focused_control_When_control_is_hidden_Then_first_responder_is_cleared()
	{
		var root = new GuiControl("root", null);
		var input = new GuiControl("input", null);
		root.AddControl(input);
		input.MakeFirstResponder(true);

		input.Hide();

		Assert.Null(root.FirstResponder);
	}

	[Fact]
	public void Given_focused_descendant_When_parent_is_hidden_Then_first_responder_is_cleared()
	{
		var root = new GuiControl("root", null);
		var panel = new GuiControl("panel", null);
		var input = new GuiControl("input", null);
		root.AddControl(panel);
		panel.AddControl(input);
		input.MakeFirstResponder(true);

		panel.Hide();

		Assert.Null(root.FirstResponder);
	}

	[Fact]
	public void Given_focused_control_When_control_is_destroyed_Then_first_responder_is_cleared()
	{
		var root = new GuiControl("root", null);
		var input = new GuiControl("input", null);
		root.AddControl(input);
		input.MakeFirstResponder(true);

		input.Destroy();

		Assert.Null(root.FirstResponder);
	}

	[Fact]
	public void Given_focused_descendant_When_parent_is_destroyed_Then_first_responder_is_cleared()
	{
		var root = new GuiControl("root", null);
		var panel = new GuiControl("panel", null);
		var input = new GuiControl("input", null);
		root.AddControl(panel);
		panel.AddControl(input);
		input.MakeFirstResponder(true);

		panel.Destroy();

		Assert.Null(root.FirstResponder);
	}
}
