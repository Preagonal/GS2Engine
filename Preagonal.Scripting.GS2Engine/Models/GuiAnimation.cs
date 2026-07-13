namespace Preagonal.Scripting.GS2Engine.Models;

public class GuiAnimation(GuiControl owner) : ScriptVariable("animation")
{
	private string? _bounds;

	public new static readonly GuiAnimationProperties PropertiesInstance = [];
	public override IScriptProperties Properties => PropertiesInstance;

	public GuiControl Owner { get; } = owner;

	public double CurrentTime { get; set; }
	public double Alpha { get; set; } = 1;
	public double Amplitude { get; set; } = 32;
	public string Bounds
	{
		get => _bounds ?? Owner.Bounds;
		set => _bounds = value;
	}
	public double Delay { get; set; }
	public double Duration { get; set; } = 1;
	public double Interval { get; set; } = 1;
	public double Rotation { get; set; }
	public string Sound { get; set; } = string.Empty;
	public bool TabFirstOnShow { get; set; } = true;
	public string Timing { get; set; } = string.Empty;
	public string Transition { get; set; } = string.Empty;
}