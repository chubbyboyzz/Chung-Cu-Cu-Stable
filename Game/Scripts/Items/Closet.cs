using Godot;
using System;
using ChungCuCu_Stable.Game.Scripts.Core;
using ChungCuCu_Stable.Game.Scripts.Entities;

namespace ChungCuCu_Stable.Game.Scripts.Items
{
    public partial class Closet : StaticBody3D, IInteractable
    {
        [Export] public AnimationPlayer AnimPlayer;
        [Export] public Marker3D HidingSpot;
        [Export] public Marker3D ExitPoint;

        // Tăng lên 4m để ma bắt được từ xa hơn (như ông yêu cầu lúc nãy)
        [Export] public float KillDistance = 4.0f;

        private bool _isPlayerInside = false;
        private bool _isOpen = false;
        private Player _currentPlayer = null;

        public string GetInteractionPrompt()
        {
            if (_isPlayerInside) return "Chui ra [E]";
            return _isOpen ? "Trốn vào [E]" : "Mở tủ [E]";
        }

        // Lưu ý: Đã XÓA hàm _Process để tránh check liên tục

        public void Interact(Node interactor)
        {
            if (interactor is Player player)
            {
                if (_isPlayerInside)
                {
                    ExitHiding(player);
                }
                else
                {
                    if (!_isOpen) OpenCloset();
                    else EnterHiding(player);
                }
            }
        }

        // Chuyển sang async để dùng được tính năng "Chờ đợi" (await)
        private async void EnterHiding(Player player)
        {
            _isPlayerInside = true;
            _currentPlayer = player;

            // 1. Chui vào và đóng cửa hờ (Cho người chơi hi vọng)
            if (AnimPlayer != null) AnimPlayer.Play("Hide");
            player.EnterHidingState(HidingSpot.GlobalPosition, HidingSpot.GlobalRotation);
           
            await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);         
            // Nếu lúc này ma vẫn đang ở gần -> BỊ LÔI CỔ RA
            if (CheckIfGhostIsTooClose())
            {
                TriggerGameOver(player);
            }
            else
            {
                // Nếu ma xa -> An toàn, không làm gì cả
                GD.Print("Hú hồn! Thoát chết.");
            }
        }

        private async void TriggerGameOver(Player player)
        {
            GD.Print("PHÁT HIỆN NGƯỜI CHƠI! GỌI MA ĐẾN...");

            // 1. Tìm script con ma
            var ghostNode = GetTree().GetFirstNodeInGroup("Ghost") as Node3D;
            var ghostScript = ghostNode as Ghost; // Ép kiểu sang class Ghost để dùng hàm CommandMoveTo

            if (ghostScript != null)
            {
                // 2. Ra lệnh cho Ma đi đến trước cửa tủ (ExitPoint)
                ghostScript.CommandMoveTo(ExitPoint.GlobalPosition);

                // 3. VÒNG LẶP CHỜ ĐỢI 
                // Chờ cho đến khi khoảng cách giữa Ma và Cửa tủ nhỏ hơn 1.5m
                while (ghostNode.GlobalPosition.DistanceTo(ExitPoint.GlobalPosition) > 1f)
                {
                    // Chờ 1 frame (để game không bị đơ)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
            }

            // 4. Ma đã đến nơi -> JUMPSCARE!
            GD.Print("MA ĐÃ ĐẾN CỬA! MỞ TỦ!");

            _isPlayerInside = false;
            _currentPlayer = null;

            if (AnimPlayer != null) AnimPlayer.Play("Open");

            // (Optional) Xoay ma nhìn thẳng vào trong tủ cho ghê
            ghostNode?.LookAt(GlobalPosition, Vector3.Up);

            // player.Die();
        }

        private bool CheckIfGhostIsTooClose()
        {
            var ghost = GetTree().GetFirstNodeInGroup("Ghost") as Node3D;
            if (ghost != null)
            {
                float distance = GlobalPosition.DistanceTo(ghost.GlobalPosition);
                // Debug để ông xem khoảng cách
                GD.Print($"Ma cách tủ: {distance}m");

                if (distance <= KillDistance) return true;
            }
            return false;
        }

        private void OpenCloset()
        {
            _isOpen = true;
            if (AnimPlayer != null) AnimPlayer.Play("Open");
        }

        private async void ExitHiding(Player player)
        {
            _isPlayerInside = false;
            _currentPlayer = null;

            if (AnimPlayer != null) AnimPlayer.Play("Open");
            player.ExitHidingState(ExitPoint.GlobalPosition, ExitPoint.GlobalRotation);

            await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);

            if (!_isPlayerInside && _isOpen)
            {
                if (AnimPlayer != null) AnimPlayer.PlayBackwards("Open");
                _isOpen = false;
            }
        }
    }
}