using Microsoft.Extensions.Logging.Testing;
using Preagonal.Scripting.GS2Engine.Enums;
using Preagonal.Scripting.GS2Engine.GS2.Script;

namespace Preagonal.Scripting.GS2Engine.UnitTests;

public class ScriptTimeoutTests
{
	[Fact]
	public void Given_due_timer_When_timer_is_consumed_Then_timer_is_cleared()
	{
		var script = CreateScript();
		script.Timer = DateTime.UtcNow.AddSeconds(-1);

		var result = script.TryConsumeDueTimer(DateTime.UtcNow);

		Assert.True(result);
		Assert.Null(script.Timer);
	}

	[Fact]
	public void Given_future_timer_When_timer_is_consumed_Then_timer_remains_scheduled()
	{
		var script = CreateScript();
		script.Timer = DateTime.UtcNow.AddSeconds(1);

		var result = script.TryConsumeDueTimer(DateTime.UtcNow);

		Assert.False(result);
		Assert.NotNull(script.Timer);
	}

	[Fact]
	public void Given_zero_timer_When_timer_is_set_Then_timer_is_disabled()
	{
		var script = CreateScript();
		script.SetTimer(1);

		script.SetTimer(0);

		Assert.Null(script.Timer);
	}

	private static Script CreateScript() =>
		new(new ScriptManager(new FakeLogger<ScriptManager>()), ScriptType.Weapon);
}
