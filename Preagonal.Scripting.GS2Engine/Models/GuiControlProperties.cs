using System.Linq;
using Preagonal.Scripting.GS2Engine.Models.Properties;

namespace Preagonal.Scripting.GS2Engine.Models;

public class GuiControlProperties : ScriptProperties<GuiControl>
{
	public GuiControlProperties() : base(typeof(ScriptVariable))
	{
		_ = ScriptVariable.PropertiesInstance;
		_ = GuiAnimation.PropertiesInstance;

		var propertyDefinitions = new PropertyDefinitions<GuiControl>();
		propertyDefinitions.Add("acceptdropfiles", "", control => control.AcceptDropFiles, (control, acceptDropFiles) => control.AcceptDropFiles = acceptDropFiles);
		propertyDefinitions.Add("active", "", control => control.Active, (control, active) => control.Active = active);
		propertyDefinitions.Add("alpha", "", control => control.Alpha, (control, alpha) => control.Alpha = alpha);
		propertyDefinitions.Add("areaclickpriority", "", control => control.AreaClickPriority, (control, areaClickPriority) => control.AreaClickPriority = areaClickPriority);
		propertyDefinitions.Add("awake", "", control => control.Awake);
		propertyDefinitions.Add("bitmapcache", "", control => control.BitmapCache, (control, bitmapCache) => control.BitmapCache = bitmapCache);
		propertyDefinitions.Add("blue", "", control => control.Blue, (control, blue) => control.Blue = blue);
		propertyDefinitions.Add("bounds", "", control => control.Bounds, (control, bounds) => control.Bounds = bounds);
		propertyDefinitions.Add("canclose", "", control => control.CanClose, (control, canClose) => control.CanClose = canClose);
		propertyDefinitions.Add("canmaximize", "", control => control.CanMaximize, (control, canMaximize) => control.CanMaximize = canMaximize);
		propertyDefinitions.Add("canminimize", "", control => control.CanMinimize, (control, canMinimize) => control.CanMinimize = canMinimize);
		propertyDefinitions.Add("canmove", "", control => control.CanMove, (control, canMove) => control.CanMove = canMove);
		propertyDefinitions.Add("canresize", "", control => control.CanResize, (control, canResize) => control.CanResize = canResize);
		propertyDefinitions.Add("clientextent", "", control => control.ClientExtent, (control, clientExtent) => control.ClientExtent = clientExtent);
		propertyDefinitions.Add("clientheight", "", control => control.ClientHeight, (control, clientHeight) => control.ClientHeight = clientHeight);
		propertyDefinitions.Add("clientwidth", "", control => control.ClientWidth, (control, clientWidth) => control.ClientWidth = clientWidth);
		propertyDefinitions.Add("clipchildren", "", control => control.ClipChildren, (control, clipChildren) => control.ClipChildren = clipChildren);
		propertyDefinitions.Add("clipmove", "", control => control.ClipMove, (control, clipMove) => control.ClipMove = clipMove);
		propertyDefinitions.Add("cliptobounds", "", control => control.ClipToBounds, (control, clipToBounds) => control.ClipToBounds = clipToBounds);
		propertyDefinitions.Add("color", "", control => control.Color, (control, color) => control.Color = color);
		propertyDefinitions.Add("cursor", "", control => control.Cursor, (control, cursor) => control.Cursor = cursor);
		propertyDefinitions.Add("editing", "", control => control.Editing, (control, editing) => control.Editing = editing);
		propertyDefinitions.Add("mode", "", control => control.Mode, (control, mode) => control.Mode = mode);
		propertyDefinitions.Add("objecttype", "", control => control.GetType().Name);
		propertyDefinitions.Add("extent", "", control => control.Extent, (control, extent) => control.Extent = extent);
		propertyDefinitions.Add("fastchildrender", "", control => control.FastChildRender, (control, fastChildRender) => control.FastChildRender = fastChildRender);
		propertyDefinitions.Add<object?>("firstresponder", "", control => control.FirstResponder, (control, firstResponder) => control.FirstResponder = GetControl(firstResponder));
		propertyDefinitions.Add("flickering", "", control => control.Flickering, (control, flickering) => control.Flickering = flickering);
		propertyDefinitions.Add("flickertime", "", control => control.FlickerTime, (control, flickerTime) => control.FlickerTime = flickerTime);
		propertyDefinitions.Add("flickerbasetime", "", control => control.FlickerBaseTime, (control, flickerBaseTime) => control.FlickerBaseTime = flickerBaseTime);
		propertyDefinitions.Add("green", "", control => control.Green, (control, green) => control.Green = green);
		propertyDefinitions.Add("height", "", control => control.Height, (control, height) => control.Height = height < 1 ? 1 : height);
		propertyDefinitions.Add("hint", "", control => control.Hint, (control, hint) => control.Hint = hint);
		propertyDefinitions.Add("hinttime", "", control => control.HintTime, (control, hintTime) => control.HintTime = hintTime);
		propertyDefinitions.Add("horizsizing", "", control => control.HorizSizing, (control, horizSizing) => control.HorizSizing = horizSizing);
		propertyDefinitions.Add("isexternal", "", control => control.IsExternal, (control, isExternal) => control.IsExternal = isExternal);
		propertyDefinitions.Add("isinanimation", "", control => control.IsInAnimation, (control, isInAnimation) => control.IsInAnimation = isInAnimation);
		propertyDefinitions.Add("isininoutanimation", "", control => control.IsInInOutAnimation, (control, isInInOutAnimation) => control.IsInInOutAnimation = isInInOutAnimation);
		propertyDefinitions.Add("lockmousedown", "", control => control.LockMouseDown, (control, lockMouseDown) => control.LockMouseDown = lockMouseDown);
		propertyDefinitions.Add("maximized", "", control => control.Maximized, (control, maximized) => control.Maximized = maximized);
		propertyDefinitions.Add("vertsizing", "", control => control.VertSizing, (control, vertSizing) => control.VertSizing = vertSizing);
		propertyDefinitions.Add("minextent", "", control => control.MinExtent, (control, minExtent) => control.MinExtent = minExtent);
		propertyDefinitions.Add("minsize", "", control => control.MinSize, (control, minSize) => control.MinSize = minSize);
		propertyDefinitions.Add("modal", "", control => control.Modal, (control, modal) => control.Modal = modal);
		propertyDefinitions.Add<object?>("parent", "", control => control.Parent);
		propertyDefinitions.Add("position", "", control => control.Position, (control, position) => control.Position = position);
		propertyDefinitions.Add<object?>("profile", "", control => control.Profile, (control, profile) => control.Profile = GetValue(profile));
		propertyDefinitions.Add("red", "", control => control.Red, (control, red) => control.Red = red);
		propertyDefinitions.Add("resizewidth", "", control => control.ResizeWidth, (control, resizeWidth) => control.ResizeWidth = resizeWidth);
		propertyDefinitions.Add("resizeheight", "", control => control.ResizeHeight, (control, resizeHeight) => control.ResizeHeight = resizeHeight);
		propertyDefinitions.Add("rotation", "", control => control.Rotation, (control, rotation) => control.Rotation = rotation);
		propertyDefinitions.Add("rotationcenter", "", control => control.RotationCenter, (control, rotationCenter) => control.RotationCenter = rotationCenter);
		propertyDefinitions.Add("scrolllinex", "", control => control.ScrollLineX, (control, scrollLineX) => control.ScrollLineX = scrollLineX);
		propertyDefinitions.Add("scrollliney", "", control => control.ScrollLineY, (control, scrollLineY) => control.ScrollLineY = scrollLineY);
		propertyDefinitions.Add("showhint", "", control => control.ShowHint, (control, showHint) => control.ShowHint = showHint);
		propertyDefinitions.Add("alwaysOnTop", "", control => control.AlwaysOnTop, (control, alwaysOnTop) => control.AlwaysOnTop = alwaysOnTop);
		propertyDefinitions.Add("style", "", control => control.Style, (control, style) => control.Style = style);
		propertyDefinitions.Add("text", "", control => control.Text, (control, text) => control.Text = text);
		propertyDefinitions.Add("useownprofile", "", control => control.UseOwnProfile, (control, useOwnProfile) => control.UseOwnProfile = useOwnProfile);
		propertyDefinitions.Add("visible", "", control => control.Visible, (control, visible) => control.Visible = visible);
		propertyDefinitions.Add("width", "", control => control.Width, (control, width) => control.Width = width < 1 ? 1 : width);
		propertyDefinitions.Add("x", "", control => control.X, (control, x) => control.X = x);
		propertyDefinitions.Add("y", "", control => control.Y, (control, y) => control.Y = y);
		propertyDefinitions.Add("controls", "", control => control.Controls);

		AddProperties(this, propertyDefinitions);

		var functionDefinitions = new FunctionDefinitions<GuiControl>();
		functionDefinitions.Add<string>("objecttype", "", (control, _) => control.GetType().Name);
		functionDefinitions.Add<int>("addcontrol", "",
		                                 (control, o2) =>
		                                 {
			                                 var control2 = o2.FirstOrDefault();
			                                 switch (control2)
			                                 {
					                                 case GuiControl newControl:
						                                 control.AddControl(newControl);
						                                 break;
					                                 case IStackEntry newControlStackEntry:
						                                 var stackControl = newControlStackEntry.GetValue<GuiControl>();
						                                 control.AddControl(stackControl);
						                                 break;
			                                 }
			                                 return 0;
		                                 }
		);
		functionDefinitions.Add<int>("bringtofront", "", (control, _) => { control.BringToFront(); return 0; });
		functionDefinitions.Add<int>("clearcontrols", "", (control, _) => { control.ClearControls(); return 0; });
		functionDefinitions.Add<object>("createanimation", "", (control, _) => control.CreateAnimation() is { } animation ? animation : 0);
		functionDefinitions.Add<int>("destroy", "", (control, _) => { control.Destroy(); return 0; });
		functionDefinitions.Add<object>("findcontrol", "", (control, args) => control.FindControl(GetString(args, 0)) is { } foundControl ? foundControl : 0);
		functionDefinitions.Add<object>("getparent", "", (control, _) => control.GetParent() is { } parent ? parent : 0);
		functionDefinitions.Add<string>("globaltolocalcoord", "", (control, args) => control.GlobalToLocalCoord(GetString(args, 0)));
		functionDefinitions.Add<string>("gettext", "", (control, _) => control.Text);
		functionDefinitions.Add<int>("hide", "", (control, _) => { control.Hide(); return 0; });
		functionDefinitions.Add<bool>("isempty", "", (control, _) => string.IsNullOrEmpty(control.Text));
		functionDefinitions.Add<bool>("isactuallyvisible", "", (control, _) => control.IsActuallyVisible());
		functionDefinitions.Add<bool>("isfirstresponder", "", (control, _) => control.IsFirstResponder());
		functionDefinitions.Add<bool>("ismouselocked", "", (control, args) => control.IsMouseLocked(GetInt(args, 0)));
		functionDefinitions.Add<string>("localtoglobalcoord", "", (control, args) => control.LocalToGlobalCoord(GetString(args, 0)));
		functionDefinitions.Add<int>("makefirstresponder", "", (control, args) => { control.MakeFirstResponder(GetBool(args, 0)); return 0; });
		functionDefinitions.Add<int>("mouselock", "", (control, args) => { control.MouseLock(GetInt(args, 0)); return 0; });
		functionDefinitions.Add<int>("mouseunlock", "", (control, args) => { control.MouseUnlock(GetInt(args, 0)); return 0; });
		functionDefinitions.Add<int>("mouseunlockall", "", (control, _) => { control.MouseUnlockAll(); return 0; });
		functionDefinitions.Add<int>("pushtoback", "", (control, _) => { control.PushToBack(); return 0; });
		functionDefinitions.Add<int>("resize", "",
		                             (control, args) =>
		                             {
			                             control.Resize(GetInt(args, 0), GetInt(args, 1), GetInt(args, 2), GetInt(args, 3));
			                             return 0;
		                             });
		functionDefinitions.Add<int>("repaint", "", (control, _) => { control.Repaint(); return 0; });
		functionDefinitions.Add<int>("settext", "", (control, args) => { control.Text = GetString(args, 0); return 0; });
		functionDefinitions.Add<int>("show", "", (control, _) => { control.Show(); return 0; });
		functionDefinitions.Add<int>("showtop", "", (control, _) => { control.ShowTop(); return 0; });
		functionDefinitions.Add<int>("showAlwaysTop", "", (control, _) => { control.ShowAlwaysTop(); return 0; });
		functionDefinitions.Add<int>("sortcontrols", "", (control, _) => { control.SortControls(); return 0; });
		functionDefinitions.Add<int>("startdrag", "", (control, _) => { control.StartDrag(); return 0; });
		functionDefinitions.Add<int>("stopanimations", "", (control, _) => { control.StopAnimations(); return 0; });
		functionDefinitions.Add<int>("stopinoutanimations", "", (control, _) => { control.StopInOutAnimations(); return 0; });
		functionDefinitions.Add<object>("tabfirst", "", (control, _) => control.TabFirst() is { } firstControl ? firstControl : 0);

		AddFunctions(this, functionDefinitions);

		Compile();
	}

	private static IGuiControl? GetControl(object? value)
	{
		if (value is IStackEntry entry) value = entry.GetValue();
		return value as IGuiControl;
	}

	private static object? GetValue(object? value) => value is IStackEntry entry ? entry.GetValue() : value;

	private static bool GetBool(IStackEntry[] args, int index) =>
		args.Length > index && args[index].GetValue<bool>();

	private static int GetInt(IStackEntry[] args, int index) =>
		args.Length > index ? (int)args[index].GetValue<double>() : 0;

	private static string GetString(IStackEntry[] args, int index) =>
		args.Length > index ? args[index].GetValue()?.ToString() ?? string.Empty : string.Empty;
}
