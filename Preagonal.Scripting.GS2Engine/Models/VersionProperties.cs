using System;

namespace Preagonal.Scripting.GS2Engine.Models;

public sealed class VersionProperties : ScriptProperties<Version>
{
	public static readonly VersionProperties Instance = [];

	private VersionProperties() : base(null)
	{
		AddProperties(
			this,
			new()
			{
				{ "major", "", version => version.Major },
				{ "minor", "", version => version.Minor },
				{ "build", "", version => version.Build },
				{ "revision", "", version => version.Revision },
			}
		);

		Compile();
	}
}
