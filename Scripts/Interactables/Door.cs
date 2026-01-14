using Godot;
using System;
using ChungCuCu_Stable.Game.Scripts.Core;
using ChungCuCu_Stable.Game.Scripts.Core.Interfaces; 
using ChungCuCu_Stable.Game.Scripts.Core.Constants;  

namespace ChungCuCu_Stable.Game.Scripts.Interactables
{
	public partial class Door : AnimatableBody3D, IInteractable, IGhostInteractable
	{
		[Export] public AnimationPlayer AnimPlayer;

		// Biến trạng thái
		private bool _isOpen = false;
		private bool _isMoving = false;

		public override void _Ready()
		{
			if (AnimPlayer != null)
			{
				AnimPlayer.AnimationFinished += OnAnimationFinished;

				// Debug check lỗi cơ bản
				if (!AnimPlayer.HasAnimation("Open"))
					GD.PrintErr($"[DOOR ERROR] Thiếu animation 'Open' trong cửa {Name}");
			}
			else
			{
				GD.PrintErr($"[DOOR ERROR] Cửa {Name} chưa gắn AnimationPlayer!");
			}
		}

		// --- PHẦN 1: TƯƠNG TÁC VỚI PLAYER (IInteractable) ---

		public string GetInteractionPrompt()
		{
			// Dùng hàm Tr() + LocKeys để lấy ngôn ngữ tự động
			if (_isOpen)
				return Tr(LocKeys.INTERACT_CLOSE_DOOR);

			return Tr(LocKeys.INTERACT_OPEN_DOOR);
		}

		public void Interact(Node interactor)
		{
			if (_isMoving) return;
			ToggleDoor();
		}

		// --- PHẦN 2: TƯƠNG TÁC VỚI MA (IGhostInteractable) ---

		public void OnGhostInteract(Node ghost)
		{
			// Logic riêng cho Ma: Chỉ mở nếu đang đóng
			if (!_isOpen && !_isMoving)
			{
				GD.Print($"[DOOR] Ma {ghost.Name} yêu cầu mở cửa.");
				ToggleDoor();
			}
		}

		// --- PHẦN 3: LOGIC CHUNG ---

		private void ToggleDoor()
		{
			if (AnimPlayer == null) return;

			_isMoving = true;

			if (_isOpen)
			{
				// Đóng cửa
				AnimPlayer.PlayBackwards("Open");
				_isOpen = false;
			}
			else
			{
				// Mở cửa
				AnimPlayer.Play("Open");
				_isOpen = true;
			}
		}

		private void OnAnimationFinished(StringName animName)
		{
			_isMoving = false; // Mở khóa nút bấm khi animation xong
		}
	}
}
