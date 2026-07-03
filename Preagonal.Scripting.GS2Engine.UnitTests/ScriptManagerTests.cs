using Microsoft.Extensions.Logging.Testing;
using Preagonal.Scripting.GS2Engine.Enums;
using Preagonal.Scripting.GS2Engine.GS2.Script;

namespace Preagonal.Scripting.GS2Engine.UnitTests;

public class ScriptManagerTests
{
	[Fact]
	public async Task GetGlobalScripts_WhenGlobalVariablesChangeConcurrently_DoesNotThrow()
	{
		var manager = new ScriptManager(new FakeLogger<ScriptManager>());
		var script  = new Script(manager, ScriptType.Weapon);
		manager.RegisterGlobalScript(script);

		using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
		var writer = Task.Run(() =>
		{
			var index = 0;
			while (!cancellation.IsCancellationRequested)
				manager.RegisterGlobalVariable($"global{index++}", index);
		});

		var exception = Record.Exception(() =>
		{
			while (!cancellation.IsCancellationRequested)
				_ = manager.GetGlobalScripts();
		});

		cancellation.Cancel();
		await writer;

		Assert.Null(exception);
	}
}
