using Godot;

namespace ChungCuCu_Stable.Game.Scripts.Resources
{
	[GlobalClass] // Để chuột phải Create New... được
	public partial class ItemData : Resource
	{
		[Export] public string ItemName { get; set; } = "Item Name";
		[Export] public Texture2D Icon { get; set; }  // Hình ảnh hiện trong túi
		[Export] public string ItemID { get; set; }   // ID định danh (VD: "flashlight", "key_01")
		[Export] public PackedScene HandModel { get; set; } // Model 3D khi cầm trên tay
	}
}
