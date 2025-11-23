using System;
using ChungCuCu_Stable.Game.Scripts.Core;
using ChungCuCu_Stable.Game.Scripts.Items;
using Godot;

namespace ChungCuCu_Stable.Game.Scripts.Entities
{
    public partial class Player : CharacterBody3D
    {
        [Export] public float Speed = 8.0f;
        [Export] public float JumpVelocity = 4.5f;
        [Export] public float Gravity = 9.8f;
        [Export] public float MouseSensitivity = 0.003f;

        [Export] public Node3D CameraPivot;
        [Export] public RayCast3D InteractionRay;

        // --- THÊM DÒNG NÀY: Biến tham chiếu tới cái Label vừa tạo ---
        [Export] public Label InteractionLabel;

        private Flashlight _currentFlashlight = null;

        public override void _Ready()
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public override void _Input(InputEvent @event)
        {
            // Logic xoay chuột
            if (@event is InputEventMouseMotion mouseMotion)
            {
                RotateY(-mouseMotion.Relative.X * MouseSensitivity);
                if (CameraPivot != null)
                {
                    CameraPivot.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);
                    Vector3 rot = CameraPivot.Rotation;
                    rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
                    CameraPivot.Rotation = rot;
                }
            }

            // Logic bật đèn
            if (@event.IsActionPressed("toggle_flashlight"))
            {
                if (_currentFlashlight != null) _currentFlashlight.Toggle();
            }
            // logic ném đồ
            if (@event.IsActionPressed("drop_item"))
            {
                if (_currentFlashlight != null)
                {
                    // 1. Tính hướng ném: Là hướng trước mặt của Camera
                    // -CameraPivot.GlobalTransform.Basis.Z là hướng "Forward"
                    Vector3 throwDirection = -CameraPivot.GlobalTransform.Basis.Z;

                    // 2. Gọi hàm Drop bên Flashlight
                    // Lực ném = Vận tốc nhân vật hiện tại + (Hướng ném * 5.0f lực mạnh)
                    _currentFlashlight.Drop(Velocity + (throwDirection * 3.0f));

                    // 3. Xóa tham chiếu đèn pin khỏi tay Player
                    _currentFlashlight = null;
                }
            }

            if (@event.IsActionPressed("ui_cancel")) Input.MouseMode = Input.MouseModeEnum.Visible;
            if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed) Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector3 velocity = Velocity;

            if (!IsOnFloor()) velocity.Y -= Gravity * (float)delta;
            if (Input.IsActionJustPressed("ui_accept") && IsOnFloor()) velocity.Y = JumpVelocity;

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

           
            UpdateInteractionUI();

            // Xử lý bấm nút tương tác
            CheckInputInteraction();

            //if (InteractionRay.IsColliding())
            //{
            //    var collider = InteractionRay.GetCollider();
            //    // In ra tên vật thể đang bị nhìn thấy
            //    GD.Print("Đang nhìn thấy: " + (collider as Node).Name);
            //}
            //else
            //{
            //    // GD.Print("Không nhìn thấy gì");
            //}
        }

        // Hàm này chuyên lo việc HIỂN THỊ CHỮ (UI)
        private void UpdateInteractionUI()
        {
            // Nếu chưa gắn Label thì bỏ qua để tránh lỗi
            if (InteractionLabel == null) return;

            // Mặc định xóa chữ đi
            InteractionLabel.Text = "";

            // Nếu Raycast đang nhìn thấy gì đó
            if (InteractionRay != null && InteractionRay.IsColliding())
            {
                var collider = InteractionRay.GetCollider();

                // Logic tìm Interface (giống hàm CheckInteraction cũ)
                if (collider is Node node && node.GetParent() is IInteractable interactableParent)
                {
                    // Lấy câu thông báo từ vật phẩm (VD: "Nhặt đèn pin [E]")
                    InteractionLabel.Text = interactableParent.GetInteractionPrompt();
                }
                else if (collider is IInteractable interactableObject)
                {
                    InteractionLabel.Text = interactableObject.GetInteractionPrompt();
                }
            }
        }

        // Hàm này chuyên lo việc XỬ LÝ BẤM NÚT (Logic)
        private void CheckInputInteraction()
        {
            if (Input.IsActionJustPressed("interact") && InteractionRay != null && InteractionRay.IsColliding())
            {
                var collider = InteractionRay.GetCollider();

                if (collider is Node node && node.GetParent() is IInteractable interactableParent)
                {
                    interactableParent.Interact(this);
                    // Tương tác xong thì xóa chữ luôn cho đỡ vướng
                    if (InteractionLabel != null) InteractionLabel.Text = "";
                }
                else if (collider is IInteractable interactableObject)
                {
                    interactableObject.Interact(this);
                    if (InteractionLabel != null) InteractionLabel.Text = "";
                }
            }
        }

        public void EquipFlashlight(Flashlight item)
        {
            _currentFlashlight = item;
        }
    }
}

