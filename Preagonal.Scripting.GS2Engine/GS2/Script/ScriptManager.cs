using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Preagonal.Scripting.GS2Engine.Extensions;
using Preagonal.Scripting.GS2Engine.Models;

namespace Preagonal.Scripting.GS2Engine.GS2.Script;

public class ScriptManager : IScriptManager
{
	private const string GlobalScriptPrefix = "__script:";

	protected readonly ILogger<ScriptManager>                       _logger;
	public static      Dictionary<string, IScriptProperties>        GlobalProperties { get; } = [];
	public             ScriptVariable                               GlobalVariables  { get; } = new();
	private readonly   Dictionary<string, ScriptObjectCreator>      _objectCreators  = new(StringComparer.OrdinalIgnoreCase);
	private            Action<string>?                              _classScriptRequestHandler;

	public ScriptManager(ILogger<ScriptManager> logger)
	{
		_logger = logger;
		RegisterDefaultObjectCreators();
	}

	public void RegisterGlobalObject(string name, ScriptVariable collection)
	{
		GlobalVariables.AddOrUpdate(name.ToLowerInvariant(), collection.ToStackEntry());

		if (collection is GuiControl guiControl)
			InstallEventCatchers(guiControl);
	}

	public void RegisterGlobalScript(Script script)
	{
		GlobalVariables.AddOrUpdate(GetGlobalScriptKey(script), script.ToStackEntry());

		var scriptName = GetGlobalScriptNameKey(script);
		if (!string.IsNullOrEmpty(scriptName))
			GlobalVariables.AddOrUpdate(scriptName, script.ToStackEntry());

		foreach (var guiControl in GetGlobalGuiControls())
			guiControl.InstallEventCatchers(script);
	}

	public void RegisterGlobalVariable(string name, object? variable) =>
		GlobalVariables.AddOrUpdate(name.ToLowerInvariant(), variable.ToStackEntry());

	public void RegisterObjectCreator(string typeName, ScriptObjectCreator creator) =>
		_objectCreators[typeName] = creator;

	public void SetClassScriptRequestHandler(Action<string>? handler) =>
		_classScriptRequestHandler = handler;

	public void RequestClassScript(string className)
	{
		if (string.IsNullOrWhiteSpace(className)) return;

		_classScriptRequestHandler?.Invoke(className);
	}

	public bool TryCreateObject(string typeName, string objectName, Script script, out ScriptVariable? createdObject)
	{
		var normalizedObjectName = objectName.ToLowerInvariant();
		if (!string.IsNullOrEmpty(normalizedObjectName) &&
		    GlobalVariables.TryGetVariable(normalizedObjectName, out var existingEntry) &&
		    existingEntry?.GetValue<ScriptVariable>() is { } existingObject)
		{
			createdObject = existingObject;
			return true;
		}

		if (_objectCreators.TryGetValue(typeName, out var creator))
		{
			createdObject = creator(objectName, script);
			if (!string.IsNullOrWhiteSpace(objectName) && createdObject != null)
				RegisterGlobalObject(objectName, createdObject);
			return true;
		}

		if (TryCreateProfile(typeName, objectName, out createdObject))
		{
			if (!string.IsNullOrWhiteSpace(objectName) && createdObject != null)
				RegisterGlobalObject(objectName, createdObject);
			return true;
		}

		createdObject = null;
		return false;
	}

	public void UnregisterGlobalObject(string name, ScriptVariable collection)
	{
		var normalizedName = name.ToLowerInvariant();
		if (string.IsNullOrEmpty(normalizedName) ||
		    !GlobalVariables.TryGetVariable(normalizedName, out var existingEntry) ||
		    !ReferenceEquals(existingEntry?.GetValue<ScriptVariable>(), collection))
		{
			return;
		}

		GlobalVariables.RemoveVariable(normalizedName);
	}

	public void UnregisterGlobalScript(Script script)
	{
		GlobalVariables.RemoveVariable(GetGlobalScriptKey(script));

		var scriptName = GetGlobalScriptNameKey(script);
		if (string.IsNullOrEmpty(scriptName))
			return;

		if (GlobalVariables.TryGetVariable(scriptName, out var entry) &&
			entry != null &&
			ReferenceEquals(entry.GetValue<Script>(), script))
		{
			GlobalVariables.RemoveVariable(scriptName);
		}
	}

	public IReadOnlyCollection<Script> GetGlobalScripts() =>
		GlobalVariables
			.GetSnapshot()
			.Where(pair => pair.Key.StartsWith(GlobalScriptPrefix, System.StringComparison.Ordinal))
			.Select(pair => pair.Value.GetValue<Script>())
			.Where(script => script != null)
			.Cast<Script>()
			.ToList();

	private static string GetGlobalScriptKey(Script script) => $"{GlobalScriptPrefix}{script.GetHashCode()}";
	private static string GetGlobalScriptNameKey(Script script) => script.Name?.ToString().ToLowerInvariant() ?? string.Empty;

	private void InstallEventCatchers(GuiControl guiControl)
	{
		foreach (var script in GetGlobalScripts())
			guiControl.InstallEventCatchers(script);
	}

	private IReadOnlyCollection<GuiControl> GetGlobalGuiControls() =>
		GlobalVariables
			.GetSnapshot()
			.Select(pair => pair.Value.GetValue<GuiControl>())
			.Where(guiControl => guiControl != null)
			.Cast<GuiControl>()
			.ToList();

	private bool TryCreateProfile(string typeName, string objectName, out ScriptVariable? createdObject)
	{
		if (!typeName.EndsWith("Profile", StringComparison.OrdinalIgnoreCase))
		{
			createdObject = null;
			return false;
		}

		var profile = CreateGuiControlProfile(objectName, copyDefaultProfile: true);
		if (GlobalVariables.TryGetVariable(typeName.ToLowerInvariant(), out var templateEntry) &&
		    templateEntry?.GetValue<GuiControlProfile>() is { } template)
		{
			profile.CopyFrom(template);
		}

		createdObject = profile;
		return true;
	}

	private void RegisterDefaultObjectCreators()
	{
		RegisterObjectCreator("GuiControl", (id, script) => new GuiControl(id, script));
		RegisterObjectCreator("GuiControlProfile", (id, _) => CreateGuiControlProfile(id, copyDefaultProfile: true));
	}

	private GuiControlProfile CreateGuiControlProfile(string objectName, bool copyDefaultProfile)
	{
		var profile = new GuiControlProfile(objectName);
		if (!copyDefaultProfile ||
		    objectName.Equals("GuiDefaultProfile", StringComparison.OrdinalIgnoreCase) ||
		    !GlobalVariables.TryGetVariable("guidefaultprofile", out var defaultEntry) ||
		    defaultEntry?.GetValue<GuiControlProfile>() is not { } defaultProfile)
		{
			return profile;
		}

		profile.CopyFrom(defaultProfile);
		return profile;
	}
}
