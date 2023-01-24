using System;
using System.Collections.Generic;
using GS2Engine.Enums;
using GS2Engine.Extensions;
using GS2Engine.GS2.Script;

namespace GS2Engine.Models
{
	public class GuiControl : VariableCollection, IGuiControl, IDisposable
	{
		public GuiControl(string id)
		{
			Script.GlobalVariables.AddOrUpdate(id.ToLower(), this.ToStackEntry());
			
			Active = true;
			X = -1;
			Y = -1;
			Width = -1;
			Height = -1;
			AddOrUpdate("addcontrol", new Script.Command(
				(m, a) =>
				{
					if (a?.Length > 0)
						AddControl(m.GetEntry(a[0], StackEntryType.Variable).GetValue<IGuiControl>());
					return 0.ToStackEntry();
				}).ToStackEntry());
	}

		public void Destroy()
		{
			//lock (controls)
			{
				foreach (IGuiControl? control in Controls)
				{
					control?.Destroy();
				}
				Controls.Clear();
			}
			Dispose();
		}

		public bool Active
		{
			get => GetVariable("active").GetValue<bool>();
			private set => AddOrUpdate("active", value.ToStackEntry());
		}

		public bool Awake => GetVariable("awake").GetValue<bool>();
		public bool CanMove {
			get => GetVariable("canmove").GetValue<bool>();
			set => AddOrUpdate("canmove", value.ToStackEntry());
		}
		public bool CanResize {
			get => GetVariable("canresize").GetValue<bool>();
			set => AddOrUpdate("canresize", value.ToStackEntry());
		}
		public bool ClipChildren {
			get => GetVariable("clipchildren").GetValue<bool>();
			set => AddOrUpdate("clipchildren", value.ToStackEntry());
		}
		public bool ClipMove {
			get => GetVariable("clipmove").GetValue<bool>();
			set => AddOrUpdate("clipmove", value.ToStackEntry());
		}
		public bool ClipToBounds {
			get => GetVariable("cliptobounds").GetValue<bool>();
			set => AddOrUpdate("cliptobounds", value.ToStackEntry());
		}
		public HashSet<IGuiControl?> Controls      { get; } = new();
		public int Cursor {
			get => GetVariable("cursor").GetValue<int>();
			set => AddOrUpdate("cursor", value.ToStackEntry());
		}
		public bool Editing {
			get => GetVariable("editing").GetValue<bool>();
			set => AddOrUpdate("editing", value.ToStackEntry());
		}
		public string                extent        { get; set; }
		public bool Flickering {
			get => GetVariable("flickering").GetValue<bool>();
			set => AddOrUpdate("flickering", value.ToStackEntry());
		}
		public int FlickerTime {
			get => GetVariable("flickertime").GetValue<int>();
			set => AddOrUpdate("flickertime", value.ToStackEntry());
		}
		public string                hint          { get; set; }
		public string                horizsizing   { get; set; }
		public int                   layer         { get; }
		public string                minextent     { get; set; }
		public string                minsize       { get; set; }
		public IGuiControl           parent        { get; set; }
		public string                position      { get; set; }
		public object                profile       { get; set; }
		public bool ResizeHeight {
			get => GetVariable("resizeheight").GetValue<bool>();
			set => AddOrUpdate("resizeheight", value.ToStackEntry());
		}
		public bool ResizeWidth {
			get => GetVariable("resizewidth").GetValue<bool>();
			set => AddOrUpdate("resizewidth", value.ToStackEntry());
		}
		public int                   scrolllinex   { get; set; }
		public int                   scrollliney   { get; set; }
		public bool ShowHint {
			get => GetVariable("showhint").GetValue<bool>();
			set => AddOrUpdate("showhint", value.ToStackEntry());
		}
		public bool UseOwnProfile {
			get => GetVariable("useownprofile").GetValue<bool>();
			set => AddOrUpdate("useownprofile", value.ToStackEntry());
		}
		public string                vertsizing    { get; set; }
		public bool Visible {
			get => GetVariable("visible").GetValue<bool>();
			set => AddOrUpdate("visible", value.ToStackEntry());
		}
		
		public int Height {
			get => (int)GetVariable("height").GetValue<double>();
			set => AddOrUpdate("height", value.ToStackEntry());
		}
		public int Width {
			get => (int)GetVariable("width").GetValue<double>();
			set => AddOrUpdate("width", value.ToStackEntry());
		}
		public int X {
			get => (int)GetVariable("x").GetValue<double>();
			set => AddOrUpdate("x", value.ToStackEntry());
		}
		public int Y {
			get => (int)GetVariable("y").GetValue<double>();
			set => AddOrUpdate("y", value.ToStackEntry());
		}

		public void AddControl(IGuiControl? obj)
		{
			if (obj == null) return;

			obj.parent = this;
			Controls.Add(obj);
		}

		public virtual void Draw()
		{
			//lock (controls)
			{
				foreach (IGuiControl? control in Controls)
				{
					control?.Draw();
				}
			}
		}

		public void Dispose()
		{
			Active = false;
		}
	}
}