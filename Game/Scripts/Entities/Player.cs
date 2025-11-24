using System;
using ChungCuCu_Stable.Game.Scripts.Core;
using ChungCuCu_Stable.Game.Scripts.Items;
using Godot;

namespace ChungCuCu_Stable.Game.Scripts.Entities
{
    public partial class Player : CharacterBody3D
    {
        [Export] public float Speed = 5.0f;
        [Export] public float MouseSensitivity = 0.003f;

        [Export] public Node3D CameraPivot;
        [Export] public RayCast3D InteractionRay;
        [Export] public Label InteractionLabel;
        [Export] public Flashlight AttachedFlashlight;

        private Flashlight _currentFlashlight = null;

        // BIẾN PUBLIC (Viết Hoa chữ cái đầu)
        public bool IsHiding = false;

        private CollisionShape3D _playerCollider;

        public override void _Ready()
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _playerCollider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

            if (AttachedFlashlight != null) _currentFlashlight = AttachedFlashlight;
            if (InteractionRay != null) InteractionRay.AddException(this);
        }

        public override void _PhysicsProcess(double delta)
        {
            // SỬA LỖI 1: Dùng IsHiding (Viết hoa) thay vì _isHiding
            if (IsHiding)
            {
                UpdateInteractionUI();
                CheckInputInteraction();
                return;
            }

            Vector3 velocity = Velocity;
            if (!IsOnFloor()) velocity.Y -= 9.8f * (float)delta;

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
            CheckInputInteraction();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseMotion mouseMotion)
            {
                float rotX = mouseMotion.Relative.X * MouseSensitivity;
                float rotY = mouseMotion.Relative.Y * MouseSensitivity;

                // SỬA LỖI 1: Dùng IsHiding (Viết hoa)
                if (IsHiding)
                {
                    Vector3 currentRot = CameraPivot.Rotation;
                    currentRot.Y -= rotX;
                    currentRot.X -= rotY;

                    currentRot.Y = Mathf.Clamp(currentRot.Y, Mathf.DegToRad(-45), Mathf.DegToRad(45));
                    currentRot.X = Mathf.Clamp(currentRot.X, Mathf.DegToRad(-15), Mathf.DegToRad(15));

                    CameraPivot.Rotation = currentRot;
                }
                else
                {
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

            if (@event.IsActionPressed("toggle_flashlight") && _currentFlashlight != null) _currentFlashlight.Toggle();

            if (@event.IsActionPressed("drop_item") && _currentFlashlight != null)
            {
                Vector3 throwDir = -CameraPivot.GlobalTransform.Basis.Z;
                _currentFlashlight.Drop(Velocity + (throwDir * 5.0f));
                _currentFlashlight = null;
            }
        }

        public void EnterHidingState(Vector3 hidePos, Vector3 hideRot)
        {
            IsHiding = true; // Sửa thành IsHiding
            if (_playerCollider != null) _playerCollider.Disabled = true;
            GlobalPosition = hidePos;
            GlobalRotation = hideRot;
            CameraPivot.Rotation = Vector3.Zero;
        }

        
        public void ExitHidingState(Vector3 exitPos, Vector3 exitRot)
        {
            IsHiding = false;

            if (_playerCollider != null) _playerCollider.Disabled = false;

            // 1. Đặt vị trí
            GlobalPosition = exitPos;

            // 2. Đặt hướng xoay (Dòng này quan trọng để sửa lỗi lệch cổ)
            GlobalRotation = exitRot;

            // 3. Reset cổ
            CameraPivot.Rotation = Vector3.Zero;
        }

        private void UpdateInteractionUI()
        {
            if (InteractionLabel == null) return;
            InteractionLabel.Text = "";
            if (InteractionRay != null && InteractionRay.IsColliding())
            {
                var collider = InteractionRay.GetCollider();
                if (collider is Node node && node.GetParent() is IInteractable interactableParent)
                    InteractionLabel.Text = interactableParent.GetInteractionPrompt();
                else if (collider is IInteractable interactableObject)
                    InteractionLabel.Text = interactableObject.GetInteractionPrompt();
            }
        }

        private void CheckInputInteraction()
        {
            if (Input.IsActionJustPressed("interact") && InteractionRay != null && InteractionRay.IsColliding())
            {
                var collider = InteractionRay.GetCollider();
                if (collider is Node node && node.GetParent() is IInteractable interactableParent)
                    interactableParent.Interact(this);
                else if (collider is IInteractable interactableObject)
                    interactableObject.Interact(this);
            }
        }

        public void EquipFlashlight(Flashlight item) { _currentFlashlight = item; }
    }
}