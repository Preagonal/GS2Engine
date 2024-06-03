using System.Runtime.InteropServices;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace GS2Engine.TestApp;

internal struct Response
{
	public   bool   Success;
	public   string ErrMsg;
	public   IntPtr ByteCode;
	internal uint   ByteCodeSize;
}

public struct CompilerResponse
{
	public bool    Success;
	public string? ErrMsg;
	public byte[]  ByteCode;
}

public static class Gs2Compiler
{
	[DllImport("gs2compiler", CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr get_context();

	[DllImport("gs2compiler", CallingConvention = CallingConvention.Cdecl)]
	private static extern Response compile_code(IntPtr context, string? code, string? type, string? name);

	[DllImport("gs2compiler", CallingConvention = CallingConvention.Cdecl)]
	private static extern void delete_context(IntPtr context);

	public static CompilerResponse CompileCode(string? code, string? type = "weapon", string? name = "npc")
	{
		var context  = get_context();
		var response = compile_code(context, code, type, name);

		CompilerResponse compilerResponse = new() { Success = response.Success, ErrMsg = response.ErrMsg };

		if (response.ByteCodeSize > 0)
		{
			compilerResponse.ByteCode = new byte[response.ByteCodeSize];
			Marshal.Copy(response.ByteCode, compilerResponse.ByteCode, 0, (int)response.ByteCodeSize);
		}

		delete_context(context);

		return compilerResponse;
	}
}