namespace GS2Engine
{
	public class TScriptCom
	{
		public byte     BytecodeByte { get; set; }
		public uint     LoopCount    { get; set; } = 0;
		public double   Value        { get; set; }
		public TString? VariableName { get; set; }
	}
}