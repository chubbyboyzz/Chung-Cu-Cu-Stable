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
        [Export] public SpotLight3D LightSource;

        // Dữ liệu thẻ bài để vào túi đồ
        [ExportGroup("Inventory")]
        [Export] public ItemData ItemInfo;

        private bool _isOn = false;
        private bool _isHeld = false;

        public override void _Ready()
        {
            if (LightSource != null) LightSource.Visible = false;
        }

        public string GetInteractionPrompt()
        {
            return Tr(LocKeys.INTERACT_PICKUP_FLASHLIGHT);
        }

        // --- HÀM NGƯỜI NHẶT (ĐÃ GỘT RỬA SẠCH SẼ LỖI CŨ) ---
        public void Interact(Node interactor)
        {
            if (interactor is Player player)
            {
                // NẾU CÓ DỮ LIỆU TÚI ĐỒ -> Đưa vào túi
                if (ItemInfo != null)
                {
                    player.AddItem(ItemInfo);
                    GD.Print("[FLASHLIGHT] Đã nhặt đèn pin vào túi đồ!");
                    QueueFree(); // Xóa khỏi mặt đất
                }
                // NẾU QUÊN GẮN DỮ LIỆU -> Báo lỗi đỏ ra Log chứ không làm sập game nữa
                else
                {
                    GD.PrintErr("[FLASHLIGHT LỖI] Ông chưa kéo file Item_Flashlight.tres vào ô Item Info của cái đèn pin dưới đất!");
                }
            }
        }

        // --- HÀM MA TƯƠNG TÁC ---
        public void OnGhostInteract(Node ghost)
        {
            if (!_isHeld && _isOn)
            {
                GD.Print("[ĐÈN PIN] Ma dẫm phải đèn -> Tắt ngóm!");
                Toggle();
            }
        }

        // --- HÀM BỊ NÉM (DROP) ---
        public void Drop(Vector3 dropVelocity)
        {
            _isHeld = false;

            // Đưa lại ra ngoài thế giới thực
            this.Reparent(GetTree().CurrentScene, true);

            // Bật lại vật lý và va chạm
            Freeze = false;
            var collider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (collider != null) collider.Disabled = false;

            // Tác dụng lực ném
            LinearVelocity = dropVelocity;

            GD.Print("[FLASHLIGHT] Đã vứt đèn!");
        }

        // --- CÁC HÀM XỬ LÝ ÁNH SÁNG ---
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