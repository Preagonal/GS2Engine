using Preagonal.Scripting.GS2Engine.Models.Properties;

namespace Preagonal.Scripting.GS2Engine.Models;

public class TGUIAnimationProperties : ScriptProperties<TGUIAnimation>
{
	public TGUIAnimationProperties() : base(typeof(ScriptVariable))
	{
		var properties = new PropertyDefinitions<TGUIAnimation>
		{
			{ "currenttime", "", animation => animation.CurrentTime, (animation, value) => animation.CurrentTime = value },
			{ "alpha", "", animation => animation.Alpha, (animation, value) => animation.Alpha = value },
			{ "amplitude", "", animation => animation.Amplitude, (animation, value) => animation.Amplitude = value },
			{ "bounds", "", animation => animation.Bounds, (animation, value) => animation.Bounds = value },
			{ "delay", "", animation => animation.Delay, (animation, value) => animation.Delay = value },
			{ "duration", "", animation => animation.Duration, (animation, value) => animation.Duration = value },
			{ "interval", "", animation => animation.Interval, (animation, value) => animation.Interval = value },
			{ "rotation", "", animation => animation.Rotation, (animation, value) => animation.Rotation = value },
			{ "sound", "", animation => animation.Sound, (animation, value) => animation.Sound = value },
			{ "tabfirstonshow", "", animation => animation.TabFirstOnShow, (animation, value) => animation.TabFirstOnShow = value },
			{ "timing", "", animation => animation.Timing, (animation, value) => animation.Timing = value },
			{ "transition", "", animation => animation.Transition, (animation, value) => animation.Transition = value }
		};

		AddProperties(this, properties);
		Compile();
	}
}
