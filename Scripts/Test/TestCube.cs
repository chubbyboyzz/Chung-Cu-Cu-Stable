using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using ChungCuCu_Stable.Game.Scripts.Core;

namespace ChungCuCu_Stable.Game.Scripts.Core.Interfaces
{
	// Class này kế thừa StaticBody3D (Vật thể đứng yên) VÀ IInteractable (Hợp đồng tương tác)
	public partial class TestCube : StaticBody3D, IInteractable
	{
		// Hàm này sẽ chạy khi Player bấm E
		public void Interact(Node interactor)
		{
			// In ra màn hình Output dòng chữ xanh lè
			GD.PrintRich("[color=green]BẠN ĐÃ BẤM VÀO CÁI HỘP! THÀNH CÔNG RỒI![/color]");

			this.GlobalPosition += new Vector3(0, 0.5f, 0);
		}

		// Hàm trả về dòng chữ gợi ý (sau này dùng cho UI)
		public string GetInteractionPrompt()
		{
			return "Bấm E để tương tác";
		}
	}
}
