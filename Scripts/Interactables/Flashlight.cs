using Godot;
using System;
using ChungCuCu_Stable.Game.Scripts.Core;
using ChungCuCu_Stable.Game.Scripts.Core.Interfaces;
using ChungCuCu_Stable.Game.Scripts.Core.Constants;
using ChungCuCu_Stable.Game.Scripts.Characters;
using ChungCuCu_Stable.Game.Scripts.Resources;

namespace ChungCuCu_Stable.Game.Scripts.Interactables
{
	public partial class Flashlight : RigidBody3D, IInteractable, IGhostInteractable
	{
		[ExportGroup("Components")]
		[Export] public SpotLight3D LightSource;

		[ExportGroup("Inventory")]
		[Export] public ItemData ItemInfo;

		private bool _isOn = false;
		private bool _isHeld = false;

		public override void _Ready()
		{
			if (LightSource != null) LightSource.Visible = false;

			//  Chờ Godot setup xong, đập thẳng Scale về 0.1
			CallDeferred(nameof(ForceScale), 0.3f);
		}

		public string GetInteractionPrompt()
		{
			return Tr(LocKeys.INTERACT_PICKUP_FLASHLIGHT);
		}

		public void Interact(Node interactor)
		{
			if (interactor is Player player)
			{
				if (ItemInfo != null)
				{
					player.AddItem(ItemInfo);
					GD.Print("[FLASHLIGHT] Đã nhặt đèn pin vào túi đồ!");
					QueueFree();
				}
			}
		}

		public void OnGhostInteract(Node ghost)
		{
			if (!_isHeld && _isOn) Toggle();
		}

	   
		// ÉP KÍCH THƯỚC AUTO-SCAN
		public void ForceScale(float targetScale)
		{
			// 1. Khóa mõm Physics Server (Bắt vỏ ngoài phải là 1)
			this.Scale = Vector3.One;

			// 2. Tự động lùng sục mọi ngóc ngách bên trong cây đèn
			foreach (Node child in GetChildren())
			{
				if (child is Node3D node3D)
				{
					// Tránh bóp nhầm khối va chạm và ánh sáng (gây lỗi tàng hình)
					if (node3D is CollisionShape3D || node3D is Light3D) continue;

					// Gặp Lưới 3D (Mesh/GLB) là bóp cổ về đúng kích thước
					node3D.Scale = new Vector3(targetScale, targetScale, targetScale);
				}
			}
		}

		// --- HÀM BỊ NÉM (DROP) ---
		public void Drop(Vector3 dropVelocity)
		{
			_isHeld = false;
			this.Reparent(GetTree().CurrentScene, true);

			// Chờ quăng ra xong, đập nó về 0.3
			CallDeferred(nameof(ForceScale), 0.3f);

			Freeze = false;
			var collider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
			if (collider != null) collider.Disabled = false;

			LinearVelocity = dropVelocity;
		}

		public void Toggle()
		{
			_isOn = !_isOn;
			if (LightSource != null) LightSource.Visible = _isOn;
		}

		public void TurnOn()
		{
			_isOn = true;
			if (LightSource != null) LightSource.Visible = true;
		}
	}
}
