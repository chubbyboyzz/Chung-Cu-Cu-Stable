using Godot;
using System;
using ChungCuCu_Stable.Game.Scripts.Core.Interfaces; // Nơi chứa IInteractable mới
using ChungCuCu_Stable.Game.Scripts.Interactables;   // Nơi chứa Flashlight, Door, Closet mới

namespace ChungCuCu_Stable.Game.Scripts.Characters // Đổi từ Entities sang Characters
{
    public partial class Player : CharacterBody3D
    {
        [ExportGroup("Movement")]
        [Export] public float Speed = 10.0f;
        [Export] public float MouseSensitivity = 0.003f;

        [ExportGroup("References")]
        [Export] public Node3D CameraPivot;
        [Export] public RayCast3D InteractionRay;
        [Export] public Label InteractionLabel;
        [Export] public Flashlight AttachedFlashlight; // Tham chiếu class Flashlight trong Interactables

        private Flashlight _currentFlashlight = null;
        private CollisionShape3D _playerCollider;

        // BIẾN TRẠNG THÁI (Public để Ghost và Closet truy cập)
        public bool IsHiding = false;

        public override void _Ready()
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _playerCollider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

            // Trang bị đèn pin nếu có sẵn lúc đầu game
            if (AttachedFlashlight != null) 
            {
                _currentFlashlight = AttachedFlashlight;
            }

            // Loại trừ bản thân khỏi tia Raycast (để không tự nhìn thấy bụng mình)
            if (InteractionRay != null) InteractionRay.AddException(this);
        }

        public override void _PhysicsProcess(double delta)
        {
            // 1. TRẠNG THÁI TRỐN
            if (IsHiding)
            {
                UpdateInteractionUI();
                CheckInputInteraction(); // Vẫn cho phép tương tác (để bấm E chui ra khỏi tủ)
                return; // Không di chuyển
            }

            // 2. DI CHUYỂN BÌNH THƯỜNG
            Vector3 velocity = Velocity;

            // Trọng lực
            if (!IsOnFloor()) velocity.Y -= 9.8f * (float)delta;

            // Input di chuyển
            Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

            if (direction != Vector3.Zero)
            {
                velocity.X = direction.X * Speed;
                velocity.Z = direction.Z * Speed;
            }
            else
            {
                velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
                velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
            }

            Velocity = velocity;
            MoveAndSlide();

            // 3. TƯƠNG TÁC UI
            UpdateInteractionUI();
            CheckInputInteraction();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseMotion mouseMotion)
            {
                float rotX = mouseMotion.Relative.X * MouseSensitivity;
                float rotY = mouseMotion.Relative.Y * MouseSensitivity;

                if (IsHiding)
                {
                    // Khi trốn: Giới hạn góc quay đầu (tránh quay 360 độ trong tủ nhìn xuyên tường)
                    if (CameraPivot != null)
                    {
                        Vector3 currentRot = CameraPivot.Rotation;
                        currentRot.Y -= rotX;
                        currentRot.X -= rotY;

                        currentRot.Y = Mathf.Clamp(currentRot.Y, Mathf.DegToRad(-60), Mathf.DegToRad(60)); // Quay trái phải ít thôi
                        currentRot.X = Mathf.Clamp(currentRot.X, Mathf.DegToRad(-30), Mathf.DegToRad(30)); // Ngước lên xuống ít thôi

                        CameraPivot.Rotation = currentRot;
                    }
                }
                else
                {
                    // Bình thường: Xoay người trái phải, xoay đầu lên xuống
                    RotateY(-rotX);
                    if (CameraPivot != null)
                    {
                        CameraPivot.RotateX(-rotY);
                        Vector3 rot = CameraPivot.Rotation;
                        rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
                        CameraPivot.Rotation = rot;
                    }
                }
            }

            // Bật tắt đèn
            if (@event.IsActionPressed("toggle_flashlight") && _currentFlashlight != null) 
                _currentFlashlight.Toggle();

            // Vứt đồ
            if (@event.IsActionPressed("drop_item") && _currentFlashlight != null)
            {
                Vector3 throwDir = -CameraPivot.GlobalTransform.Basis.Z; // Ném về phía trước mặt
                _currentFlashlight.Drop(Velocity + (throwDir * 5.0f));   // Cộng thêm đà ném
                _currentFlashlight = null;
            }
        }

        // --- HỆ THỐNG TRỐN TÌM (Gọi bởi Closet.cs) ---

        public void EnterHidingState(Vector3 hidePos, Vector3 hideRot)
        {
            IsHiding = true;
            if (_playerCollider != null) _playerCollider.Disabled = true; // Tắt va chạm để không bị kẹt vào tủ
            
            GlobalPosition = hidePos;
            GlobalRotation = hideRot; // Xoay người theo hướng tủ yêu cầu
            
            if (CameraPivot != null) CameraPivot.Rotation = Vector3.Zero; // Reset đầu nhìn thẳng
        }
        
        public void ExitHidingState(Vector3 exitPos, Vector3 exitRot)
        {
            IsHiding = false;
            if (_playerCollider != null) _playerCollider.Disabled = false; // Bật lại va chạm

            GlobalPosition = exitPos;
            GlobalRotation = exitRot; // Xoay người ra hướng cửa
            
            if (CameraPivot != null) CameraPivot.Rotation = Vector3.Zero;
        }

        // --- HỆ THỐNG TƯƠNG TÁC ---

        private void UpdateInteractionUI()
        {
            if (InteractionLabel == null) return;
            InteractionLabel.Text = "";

            if (InteractionRay != null && InteractionRay.IsColliding())
            {
                var collider = InteractionRay.GetCollider();
                
                // Trường hợp 1: Ray trúng node con, cần tìm cha (ví dụ trúng Mesh, script nằm ở cha)
                if (collider is Node node && node.GetParent() is IInteractable interactableParent)
                {
                    InteractionLabel.Text = interactableParent.GetInteractionPrompt();
                }
                // Trường hợp 2: Ray trúng trực tiếp vật thể có script (ví dụ Door, Closet)
                else if (collider is IInteractable interactableObject)
                {
                    InteractionLabel.Text = interactableObject.GetInteractionPrompt();
                }
            }
        }

        private void CheckInputInteraction()
        {
            if (Input.IsActionJustPressed("interact") && InteractionRay != null && InteractionRay.IsColliding())
            {
                var collider = InteractionRay.GetCollider();

                // Logic tương tự như Update UI nhưng gọi hàm Interact
                if (collider is Node node && node.GetParent() is IInteractable interactableParent)
                {
                    interactableParent.Interact(this);
                }
                else if (collider is IInteractable interactableObject)
                {
                    interactableObject.Interact(this);
                }
            }
        }

        public void EquipFlashlight(Flashlight item) 
        { 
            _currentFlashlight = item; 
        }

        // Hàm xử lý Game Over (Chết)
        public void Die()
        {
            GD.Print("PLAYER ĐÃ CHẾT!");
            SetPhysicsProcess(false); // Ngừng điều khiển
            Input.MouseMode = Input.MouseModeEnum.Visible; // Hiện chuột
            // Sau này ông có thể thêm màn hình Game Over ở đây
        }
    }
}