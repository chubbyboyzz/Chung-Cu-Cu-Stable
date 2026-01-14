using Godot;
using System;
using System.Threading.Tasks;
using ChungCuCu_Stable.Game.Scripts.Core;
using ChungCuCu_Stable.Game.Scripts.Core.Interfaces;
using ChungCuCu_Stable.Game.Scripts.Core.Constants;
using ChungCuCu_Stable.Game.Scripts.Characters;

namespace ChungCuCu_Stable.Game.Scripts.Interactables
{
    public partial class Closet : StaticBody3D, IInteractable, IGhostInteractable
    {
        [Export] public AnimationPlayer AnimPlayer;
        [Export] public Marker3D HidingSpot;
        [Export] public Marker3D ExitPoint;

        // CẤU HÌNH: Khoảng cách an toàn (trên 6m là Ma tự bỏ đi)
        [Export] public float SafeDistance = 6.0f;

        // CẤU HÌNH: Layer của Tường để chắn tầm nhìn (Thường là Layer 1)
        [Export(PropertyHint.Layers3DPhysics)] public uint WallLayerMask = 1;

        private bool _isPlayerInside = false;
        private bool _isOpen = false;
        private Player _currentPlayer = null;

        public override void _Ready()
        {
            if (AnimPlayer == null) GD.PrintErr($"[CLOSET] Lỗi: Chưa gắn AnimPlayer cho tủ {Name}");
            if (HidingSpot == null || ExitPoint == null) GD.PrintErr($"[CLOSET] Lỗi: Thiếu Marker vị trí cho tủ {Name}");
        }

        public string GetInteractionPrompt()
        {
            if (_isPlayerInside) return Tr(LocKeys.INTERACT_EXIT_HIDING);
            return _isOpen ? Tr(LocKeys.INTERACT_HIDE) : Tr(LocKeys.INTERACT_OPEN_CLOSET);
        }

        // --- 1. NGƯỜI TƯƠNG TÁC ---
        public void Interact(Node interactor)
        {
            if (interactor is Player player)
            {
                if (_isPlayerInside) ExitHiding(player);
                else
                {
                    if (!_isOpen) OpenCloset();
                    else EnterHiding(player);
                }
            }
        }

        // --- 2. MA TƯƠNG TÁC (QUAN TRỌNG NHẤT) ---
        public void OnGhostInteract(Node ghostNode)
        {
            if (_isOpen) return; // Tủ mở rồi thì thôi

            Ghost ghostScript = ghostNode as Ghost;
            if (ghostScript == null) return;

            // NẾU CÓ NGƯỜI BÊN TRONG
            if (_isPlayerInside)
            {
                // Kiểm tra xem Ma có đang "Cay cú" (IsChasing) không?
                // Logic: Nếu lúc trốn ông đã lừa được nó (CheckSafetyMoment) thì IsChasing đã = false rồi.
                if (ghostScript.IsChasing)
                {
                    GD.Print($"[CLOSET] CHẾT! Ma {ghostNode.Name} mở tủ tóm sống!");
                    PerformJumpscare(ghostNode);
                }
                else
                {
                    GD.Print($"[CLOSET] Hú hồn! Ma đi ngang qua nhưng không biết.");
                }
            }
        }

        // --- 3. LOGIC TRỐN TÌM ---
        private async void EnterHiding(Player player)
        {
            _isPlayerInside = true;
            _currentPlayer = player;

            if (AnimPlayer != null) AnimPlayer.Play("Hide");
            player.EnterHidingState(HidingSpot.GlobalPosition, HidingSpot.GlobalRotation);

            // --- KIỂM TRA AN TOÀN NGAY KHOẢNH KHẮC VÀO TỦ ---
            CheckSafetyMoment();

            // Chờ animation đóng cửa xong
            await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
        }

        private void CheckSafetyMoment()
        {
            var ghostNode = GetTree().GetFirstNodeInGroup("Ghost") as Node3D;
            if (ghostNode == null) return;
            var ghostScript = ghostNode as Ghost;

            // 1. Đo khoảng cách
            float distance = GlobalPosition.DistanceTo(ghostNode.GlobalPosition);
            GD.Print($"[CLOSET] Khoảng cách khi trốn: {distance}m");

            // Nếu xa tít mù khơi -> AN TOÀN
            if (distance > SafeDistance)
            {
                GD.Print("-> [AN TOÀN] Đủ xa! Ma tự động mất dấu.");
                ghostScript?.ForceStopChasing(); // Gọi hàm quên của Ma
                return;
            }

            // 2. Nếu ở Gần -> Bắn tia Raycast kiểm tra tường
            // Bắn từ Mắt Ma (cao 1.5m) -> Đến Tâm Tủ (cao 1.0m)
            PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
            Vector3 fromPos = ghostNode.GlobalPosition + Vector3.Up * 1.5f;
            Vector3 toPos = GlobalPosition + Vector3.Up * 1.0f;

            var query = PhysicsRayQueryParameters3D.Create(fromPos, toPos);
            query.CollisionMask = WallLayerMask; // Chỉ va vào Tường (Layer 1)

            // Đừng để tia đâm vào chính cái Tủ
            query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                // Tia đâm vào cái gì đó (Tường) trước khi đến Tủ
                GD.Print("-> [MAY MẮN] Ma ở gần nhưng bị góc tường che khuất!");
                ghostScript?.ForceStopChasing(); // Cứu mạng -> Ma quên luôn
            }
            else
            {
                GD.Print("-> [NGUY HIỂM] Ma nhìn thấy rõ mồn một! Nó vẫn đang lao đến...");
                // Không làm gì cả -> Ma giữ nguyên IsChasing = true
                // Khi nó chạy đến chạm vào tủ -> Hàm OnGhostInteract ở trên sẽ xử lý.
            }
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

        private void OpenCloset()
        {
            _isOpen = true;
            if (AnimPlayer != null) AnimPlayer.Play("Open");
        }

        // --- 4. GAME OVER (JUMPSCARE) ---
        private void PerformJumpscare(Node ghostNode)
        {
            // 1. Mở toang tủ
            if (AnimPlayer != null) AnimPlayer.Play("Open");

            // 2. Xoay ma nhìn thẳng vào mặt
            if (ghostNode is Node3D ghost3D)
            {
                ghost3D.LookAt(GlobalPosition, Vector3.Up);
            }

            GD.Print("--- JUMPSCARE!!! GAME OVER ---");

            // 3. Gọi hàm chết của Player (nếu có)
            if (_currentPlayer != null)
            {
                // _currentPlayer.Die(); 
            }

            // Tạm thời restart game để test
            // GetTree().ReloadCurrentScene();
        }
    }
}