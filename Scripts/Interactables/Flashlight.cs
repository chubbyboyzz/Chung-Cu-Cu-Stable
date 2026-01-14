using Godot;
using System;
using ChungCuCu_Stable.Game.Scripts.Core;
using ChungCuCu_Stable.Game.Scripts.Core.Interfaces; // Interface cho Ma
using ChungCuCu_Stable.Game.Scripts.Core.Constants;  // Localization Key
using ChungCuCu_Stable.Game.Scripts.Characters;      // Namespace chứa Player

namespace ChungCuCu_Stable.Game.Scripts.Interactables
{
    // Kế thừa IGhostInteractable để Ma có thể tác động vào đèn (ví dụ làm tắt đèn)
    public partial class Flashlight : RigidBody3D, IInteractable, IGhostInteractable
    {
        [Export] public SpotLight3D LightSource;

        private bool _isOn = false;
        private bool _isHeld = false; // Biến kiểm tra xem đang nằm trên tay hay dưới đất

        public override void _Ready()
        {
            if (LightSource != null) LightSource.Visible = false;
        }

        public string GetInteractionPrompt()
        {
            // Dùng Key đa ngôn ngữ
            return Tr(LocKeys.INTERACT_PICKUP_FLASHLIGHT);
        }

        // --- HÀM NGƯỜI NHẶT ---
        public void Interact(Node interactor)
        {
            // Kiểm tra xem interactor có phải là Player (thuộc namespace Characters) không
            if (interactor is Player player)
            {
                player.EquipFlashlight(this);
                _isHeld = true;

                // 1. Tắt vật lý
                Freeze = true;

                // 2. Tắt va chạm (Tìm collider an toàn hơn)
                var collider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
                if (collider != null) collider.Disabled = true;

                // 3. Chuyển nhà về tay Player
                // Lưu ý: false để giữ transform tương đối, sau đó ta set lại tay bo
                this.Reparent(player.CameraPivot, false);

                Position = new Vector3(0.3f, -0.25f, -0.5f);
                Rotation = Vector3.Zero;
                Scale = new Vector3(0.15f, 0.15f, 0.15f);

                GD.Print("Đã nhặt đèn!");
            }
        }

        // --- HÀM MA TƯƠNG TÁC (IGhostInteractable) ---
        public void OnGhostInteract(Node ghost)
        {
            // Logic: Nếu đèn đang nằm dưới đất (_isHeld = false) và đang BẬT
            // Mà Ma đi qua -> Đèn tự tắt (Hù player)
            if (!_isHeld && _isOn)
            {
                GD.Print("[ĐÈN PIN] Ma dẫm phải đèn -> Tắt ngóm!");
                Toggle(); // Tắt đèn

                // (Nâng cao) Có thể thêm hiệu ứng chớp tắt vài cái rồi mới tắt hẳn ở đây
            }
        }

        // --- HÀM BỊ NÉM (DROP) ---
        public void Drop(Vector3 dropVelocity)
        {
            _isHeld = false;

            // 1. Chuyển nhà ra Scene gốc
            this.Reparent(GetTree().CurrentScene, true);

            // 2. Bật lại vật lý
            Freeze = false;

            // 3. Bật lại va chạm
            var collider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (collider != null) collider.Disabled = false;

            // 4. Ném đi
            LinearVelocity = dropVelocity;

            // Scale to ra 1 chút để dễ tìm lại 
            // Scale = Vector3.One; 

            GD.Print("Đã vứt đèn!");
        }

        public void Toggle()
        {
            _isOn = !_isOn;
            if (LightSource != null) LightSource.Visible = _isOn;
        }
    }
}