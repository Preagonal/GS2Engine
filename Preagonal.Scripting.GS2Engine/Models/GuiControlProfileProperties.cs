using Preagonal.Scripting.GS2Engine.Models.Properties;

namespace Preagonal.Scripting.GS2Engine.Models;

public class GuiControlProfileProperties : ScriptProperties<GuiControlProfile>
{
	public GuiControlProfileProperties() : base(typeof(ScriptVariable))
	{
		var properties = new PropertyDefinitions<GuiControlProfile>
		{
			{ "align", "", profile => profile.Align, (profile, value) => { profile.Align = value; profile.Justify = value; } },
			{ "autosizeheight", "", profile => profile.AutoSizeHeight, (profile, value) => profile.AutoSizeHeight = value },
			{ "autosizewidth", "", profile => profile.AutoSizeWidth, (profile, value) => profile.AutoSizeWidth = value },
			{ "backgroundinset", "", profile => profile.BackgroundInset, (profile, value) => profile.BackgroundInset = value },
			{ "bitmap", "", profile => profile.Bitmap, (profile, value) => profile.Bitmap = value },
			{ "border", "", profile => profile.Border, (profile, value) => profile.Border = value },
			{ "bordercolor", "", profile => profile.BorderColor, (profile, value) => profile.BorderColor = value },
			{ "bordercolorhl", "", profile => profile.BorderColorHl, (profile, value) => profile.BorderColorHl = value },
			{ "bordercolorna", "", profile => profile.BorderColorNa, (profile, value) => profile.BorderColorNa = value },
			{ "borderthickness", "", profile => profile.BorderThickness, (profile, value) => profile.BorderThickness = value },
			{ "boxextent", "", profile => profile.BoxExtent, (profile, value) => profile.BoxExtent = value },
			{ "cankeyfocus", "", profile => profile.CanKeyFocus, (profile, value) => profile.CanKeyFocus = value },
			{ "cursorcolor", "", profile => profile.CursorColor, (profile, value) => profile.CursorColor = value },
			{ "fillcolor", "", profile => profile.FillColor, (profile, value) => profile.FillColor = value },
			{ "fillcolorhl", "", profile => profile.FillColorHl, (profile, value) => profile.FillColorHl = value },
			{ "fillcolorna", "", profile => profile.FillColorNa, (profile, value) => profile.FillColorNa = value },
			{ "fillonlynonchildarea", "", profile => profile.FillOnlyNonChildArea, (profile, value) => profile.FillOnlyNonChildArea = value },
			{ "focusonshow", "", profile => profile.FocusOnShow, (profile, value) => profile.FocusOnShow = value },
			{ "fontcolor", "", profile => profile.FontColor, (profile, value) => profile.FontColor = value },
			{ "fontcolorhl", "", profile => profile.FontColorHl, (profile, value) => profile.FontColorHl = value },
			{ "fontcolorna", "", profile => profile.FontColorNa, (profile, value) => profile.FontColorNa = value },
			{ "fontcolorsel", "", profile => profile.FontColorSel, (profile, value) => profile.FontColorSel = value },
			{ "fontcolorlink", "", profile => profile.FontColorLink, (profile, value) => profile.FontColorLink = value },
			{ "fontcolorlinkhl", "", profile => profile.FontColorLinkHl, (profile, value) => profile.FontColorLinkHl = value },
			{ "fontsize", "", profile => profile.FontSize, (profile, value) => profile.FontSize = value },
			{ "fontstyle", "", profile => profile.FontStyle, (profile, value) => profile.FontStyle = value },
			{ "fontstylecontrolwords", "", profile => profile.FontStyleControlWords, (profile, value) => profile.FontStyleControlWords = value },
			{ "fontstyleidentifiers", "", profile => profile.FontStyleIdentifiers, (profile, value) => profile.FontStyleIdentifiers = value },
			{ "fontstylestrings", "", profile => profile.FontStyleStrings, (profile, value) => profile.FontStyleStrings = value },
			{ "fontstylenumbers", "", profile => profile.FontStyleNumbers, (profile, value) => profile.FontStyleNumbers = value },
			{ "fonttype", "", profile => profile.FontType, (profile, value) => profile.FontType = value },
			{ "gradientcolor", "", profile => profile.GradientColor, (profile, value) => profile.GradientColor = value },
			{ "justify", "", profile => profile.Align, (profile, value) => { profile.Align = value; profile.Justify = value; } },
			{ "linespacing", "", profile => profile.LineSpacing, (profile, value) => profile.LineSpacing = value },
			{ "mouseoverbitmap", "", profile => profile.MouseOverBitmap, (profile, value) => profile.MouseOverBitmap = value },
			{ "mouseoverselected", "", profile => profile.MouseOverSelected, (profile, value) => profile.MouseOverSelected = value },
			{ "modal", "", profile => profile.Modal, (profile, value) => profile.Modal = value },
			{ "normalbitmap", "", profile => profile.NormalBitmap, (profile, value) => profile.NormalBitmap = value },
			{ "numbersonly", "", profile => profile.NumbersOnly, (profile, value) => profile.NumbersOnly = value },
			{ "returntab", "", profile => profile.ReturnTab, (profile, value) => profile.ReturnTab = value },
			{ "opaque", "", profile => profile.Opaque, (profile, value) => profile.Opaque = value },
			{ "overridestylefont", "", profile => profile.OverrideStyleFont, (profile, value) => profile.OverrideStyleFont = value },
			{ "pressedbitmap", "", profile => profile.PressedBitmap, (profile, value) => profile.PressedBitmap = value },
			{ "shadowcolor", "", profile => profile.ShadowColor, (profile, value) => profile.ShadowColor = value },
			{ "shadowoffset", "", profile => profile.ShadowOffset, (profile, value) => profile.ShadowOffset = value },
			{ "soundbuttondown", "", profile => profile.SoundButtonDown, (profile, value) => profile.SoundButtonDown = value },
			{ "soundbuttonover", "", profile => profile.SoundButtonOver, (profile, value) => profile.SoundButtonOver = value },
			{ "tab", "", profile => profile.Tab, (profile, value) => profile.Tab = value },
			{ "textgradient", "", profile => profile.TextGradient, (profile, value) => profile.TextGradient = value },
			{ "textoffset", "", profile => profile.TextOffset, (profile, value) => profile.TextOffset = value },
			{ "textshadow", "", profile => profile.TextShadow, (profile, value) => profile.TextShadow = value },
			{ "transparency", "", profile => profile.Transparency, (profile, value) => profile.Transparency = value }
		};
		AddProperties(this, properties);

		var functions = new FunctionDefinitions<GuiControlProfile>
		{
			{ "gettextwidth", "", (profile, args) => profile.GetTextWidth(args.Length > 0 ? args[0].GetValue()?.ToString() ?? string.Empty : string.Empty) },
			{ "gettextheight", "", (profile, _) => profile.GetTextHeight() },
			{ "preloadfont", "", (profile, _) => { profile.PreloadFont(); return 0; } }
		};
		AddFunctions(this, functions);
		Compile();
	}
}
