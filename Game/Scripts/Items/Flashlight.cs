using Godot;
using System;
using ChungCuCu_Stable.Game.Scripts.Core;
using ChungCuCu_Stable.Game.Scripts.Entities;

namespace ChungCuCu_Stable.Game.Scripts.Items
{
    // SỬA: Kế thừa RigidBody3D thay vì Node3D
    public partial class Flashlight : RigidBody3D, IInteractable
    {
        [Export] public SpotLight3D LightSource;

        private bool _isOn = false;

        public override void _Ready()
        {
            // Nếu chưa có ai cầm (không có cha là CameraPivot) thì cứ để vật lý hoạt động
            if (LightSource != null) LightSource.Visible = false;
        }

        public string GetInteractionPrompt()
        {
            return "Nhặt đèn pin [E]";
        }

        // --- HÀM BỊ NHẶT ---
        public void Interact(Node interactor)
        {
            if (interactor is Player player)
            {
                player.EquipFlashlight(this);

                // 1. Tắt vật lý (Freeze) để nó không rơi khỏi tay
                Freeze = true;

                // 2. Tắt va chạm (Collision) để không đẩy Player
                // Tìm CollisionShape3D là con trực tiếp
                var collider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
                if (collider != null) collider.Disabled = true;

                // 3. Chuyển nhà về tay Player
                // Lưu ý: RigidBody khi reparent đôi khi cần reset transform cẩn thận
                this.Reparent(player.CameraPivot, false);

                Position = new Vector3(0.3f, -0.25f, -0.5f);
                Rotation = Vector3.Zero;
                Scale = new Vector3(0.15f, 0.15f, 0.15f); // Scale nhỏ lại

                GD.Print("Đã nhặt đèn!");
            }
        }

        // --- HÀM BỊ NÉM (DROP) ---
        public void Drop(Vector3 dropVelocity)
        {
            // 1. Chuyển nhà ra ngoài Vũ trụ (Scene gốc)
            // GetTree().CurrentScene là node gốc của Level hiện tại
            this.Reparent(GetTree().CurrentScene, true);

            // 2. Bật lại vật lý
            Freeze = false;

            // 3. Bật lại va chạm
            var collider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (collider != null) collider.Disabled = false;

            // 4. Ném nó đi theo hướng Player đang nhìn (Velocity)
            LinearVelocity = dropVelocity;

            // Reset lại Scale to ra (nếu muốn lúc rơi xuống đất nó to dễ tìm)
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