using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Testing;
using Preagonal.Scripting.GS2Engine.Enums;
using Preagonal.Scripting.GS2Engine.GS2.Script;
using Preagonal.Scripting.GS2Engine.Models;

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

	[Fact]
	public async Task RegisterGlobalScript_WhenScriptBytecodeUpdatesConcurrently_DoesNotThrow()
	{
		var manager = new ScriptManager(new FakeLogger<ScriptManager>());
		var ownerScript = new Script(manager, ScriptType.Weapon);
		var sourceScript = new Script(manager, ScriptType.Weapon);
		_ = new GuiControl("control", ownerScript);
		var bytecode = Compile(
			"""
			//#CLIENTSIDE
			function control.onAction() {
				return 1;
			}
			"""
		);
		var alternateBytecode = Compile(
			"""
			//#CLIENTSIDE
			function control.onResize() {
				return 2;
			}
			"""
		);

		using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
		ConcurrentQueue<Exception> exceptions = [];
		var writer = Task.Run(() =>
		{
			var useAlternate = false;
			while (!cancellation.IsCancellationRequested)
			{
				try
				{
					sourceScript.UpdateFromByteCode("source", useAlternate ? alternateBytecode : bytecode);
					useAlternate = !useAlternate;
				}
				catch (Exception exception)
				{
					exceptions.Enqueue(exception);
				}
			}
		});

		while (!cancellation.IsCancellationRequested)
		{
			try
			{
				manager.RegisterGlobalScript(sourceScript);
			}
			catch (Exception exception)
			{
				exceptions.Enqueue(exception);
			}
		}

		cancellation.Cancel();
		await writer;

		Assert.Empty(exceptions);
	}

	private static byte[] Compile(string scriptText)
	{
		var response = GS2Compiler.Interface.CompileCode(scriptText, "weapon", "test", withHeader: false);
		if (response.Success)
			return response.ByteCode;

		throw new($"Script failure: {response.ErrMsg}");
	}
}
