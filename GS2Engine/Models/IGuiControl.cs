namespace GS2Engine.Models
{
	public interface IGuiControl
	{
		public void        Draw();
		public IGuiControl parent { get; set; }
		void               Destroy();
		public void        AddControl(IGuiControl? obj);
	}
}