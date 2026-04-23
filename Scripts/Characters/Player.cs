using Godot;
using System;
using System.Collections.Generic;
using ChungCuCu_Stable.Game.Scripts.Core.Interfaces;
using ChungCuCu_Stable.Game.Scripts.Interactables;
using ChungCuCu_Stable.Game.Scripts.UI;
using ChungCuCu_Stable.Game.Scripts.Resources;

namespace ChungCuCu_Stable.Game.Scripts.Characters
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

        // ĐÃ XÓA AttachedFlashlight vì ta dùng cơ chế Lôi từ túi ra!
        private Flashlight _currentFlashlight = null;
        private CollisionShape3D _playerCollider;

        public bool IsHiding = false;

        [ExportGroup("Inventory System")]
        [Export] public InventoryUI InventoryScreen;

        [ExportGroup("Hand System")]
        [Export] public Node3D HandPosition;

        private List<ItemData> _collectedItems = new List<ItemData>();
        private bool _isInventoryOpen = false;

        public override void _Ready()
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _playerCollider = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

            if (InteractionRay != null) InteractionRay.AddException(this);

            if (InventoryScreen != null)
            {
                InventoryScreen.ItemSelected += EquipItem;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
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
            if (@event.IsActionPressed("toggle_inventory"))
            {
                ToggleInventory();
            }

            if (@event is InputEventMouseMotion mouseMotion)
            {
                if (_isInventoryOpen) return;

                float rotX = mouseMotion.Relative.X * MouseSensitivity;
                float rotY = mouseMotion.Relative.Y * MouseSensitivity;

                if (IsHiding)
                {
                    if (CameraPivot != null)
                    {
                        Vector3 currentRot = CameraPivot.Rotation;
                        currentRot.Y -= rotX;
                        currentRot.X -= rotY;

                        currentRot.Y = Mathf.Clamp(currentRot.Y, Mathf.DegToRad(-60), Mathf.DegToRad(60));
                        currentRot.X = Mathf.Clamp(currentRot.X, Mathf.DegToRad(-30), Mathf.DegToRad(30));

                        CameraPivot.Rotation = currentRot;
                    }
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

            // --- TEST BẤM PHÍM 'H' (LOGIC LÔI TỪ TÚI RA NHƯ ÔNG MUỐN) ---
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.PhysicalKeycode == Key.H)
            {
                ItemData flashlightData = null;
                foreach (var item in _collectedItems)
                {
                    if (item != null && item.ItemID == "flashlight")
                    {
                        flashlightData = item;
                        break;
                    }
                }

                if (flashlightData != null)
                {
                    // Nếu ĐANG CẦM -> Cất vào túi
                    if (_currentFlashlight != null)
                    {
                        _currentFlashlight.QueueFree(); // Xóa khỏi tay
                        _currentFlashlight = null;
                        GD.Print("[PLAYER] Đã CẤT đèn pin vào túi!");
                    }
                    // Nếu CHƯA CẦM -> Lôi từ túi ra
                    else
                    {
                        SpawnItemToHand(flashlightData);
                    }
                }
                else
                {
                    GD.Print("[PLAYER] Trong túi chưa có đèn pin!");
                }
            }

            // --- NÉM ĐỒ (DROP) VÀ XÓA KHỎI TÚI ---
            if (@event.IsActionPressed("drop_item") && _currentFlashlight != null)
            {
                // 1. Tìm lại cái dữ liệu thẻ bài đèn pin đang lưu trong túi
                ItemData flashlightData = _collectedItems.Find(i => i != null && i.ItemID == "flashlight");

                // 2. Bơm trả lại dữ liệu này vào cái đèn trên tay trước khi ném nó đi!
                if (flashlightData != null)
                {
                    _currentFlashlight.ItemInfo = flashlightData;
                }

                // 3. Thực hiện văng ra khỏi tay
                Vector3 throwDir = -CameraPivot.GlobalTransform.Basis.Z;
                _currentFlashlight.Drop(Velocity + (throwDir * 5.0f));

                // 4. Xóa hoàn toàn khỏi túi đồ
                _collectedItems.RemoveAll(i => i.ItemID == "flashlight");

                _currentFlashlight = null;
            }

            // Phím tắt bật/tắt bóng đèn khi đang cầm trên tay (Ví dụ phím F)
            if (@event.IsActionPressed("toggle_flashlight") && _currentFlashlight != null)
            {
                _currentFlashlight.Toggle();
            }
        }

        // --- CÁC HÀM TRỐN TÌM, TƯƠNG TÁC (Giữ nguyên) ---
        public void EnterHidingState(Vector3 hidePos, Vector3 hideRot)
        {
            IsHiding = true;
            if (_playerCollider != null) _playerCollider.Disabled = true;
            GlobalPosition = hidePos;
            GlobalRotation = hideRot;
            if (CameraPivot != null) CameraPivot.Rotation = Vector3.Zero;
        }

        public void ExitHidingState(Vector3 exitPos, Vector3 exitRot)
        {
            IsHiding = false;
            if (_playerCollider != null) _playerCollider.Disabled = false;
            GlobalPosition = exitPos;
            GlobalRotation = exitRot;
            if (CameraPivot != null) CameraPivot.Rotation = Vector3.Zero;
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

        public void Die()
        {
            GD.Print("PLAYER ĐÃ CHẾT!");
            SetPhysicsProcess(false);
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        // =========================================================
        // HỆ THỐNG TÚI ĐỒ VÀ CẦM NẮM (MỚI)
        // =========================================================

        private void ToggleInventory()
        {
            if (InventoryScreen == null) return;
            _isInventoryOpen = !_isInventoryOpen;

            if (_isInventoryOpen)
            {
                InventoryScreen.Open(_collectedItems);
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            else
            {
                InventoryScreen.Close();
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }

        public void AddItem(ItemData newItem)
        {
            if (newItem == null) return;
            if (!_collectedItems.Contains(newItem))
            {
                _collectedItems.Add(newItem);
                GD.Print($"[PLAYER] Đã bỏ vào túi: {Tr(newItem.ItemName)}");
            }
        }

        public bool HasKey(string keyID)
        {
            foreach (var item in _collectedItems)
            {
                if (item.ItemID == keyID) return true;
            }
            return false;
        }

        private void EquipItem(ItemData item)
        {
            if (item == null) return;
            SpawnItemToHand(item);
            ToggleInventory();
        }

        // HÀM CHÍNH ĐỂ LÔI ĐỒ TỪ TÚI RA TAY (ĐÃ GẮN RADAR DÒ TỌA ĐỘ)
        private void SpawnItemToHand(ItemData itemData)
        {
            GD.Print("\n--- BẮT ĐẦU LÔI ĐỒ RA TAY ---");
            GD.Print($"[PLAYER] Đang lôi {Tr(itemData.ItemName)} ra khỏi túi...");

            // 1. Dọn dẹp tay (Xóa cái đang cầm hiện tại)
            if (_currentFlashlight != null)
            {
                _currentFlashlight.QueueFree();
                _currentFlashlight = null;
            }

            if (HandPosition != null)
            {
                foreach (Node child in HandPosition.GetChildren()) child.QueueFree();

                // MÁY DÒ 1: Vị trí của Bàn Tay
                GD.Print($"[RADAR] Tọa độ của Bàn Tay (HandPosition) ngoài thế giới thực: {HandPosition.GlobalPosition}");
            }
            else
            {
                GD.PrintErr("[LỖI NẶNG] Node HandPosition bị NULL. Ông chưa gán nó vào ô Hand Position trong Inspector của Player!");
                return; // Dừng luôn nếu không có tay
            }

            // 2. Lấy Scene đẻ ra
            if (itemData.HandModel != null)
            {
                Node3D model = itemData.HandModel.Instantiate() as Node3D;

                // MÁY DÒ 2: Xác nhận đã đẻ ra thật chưa
                if (model != null)
                {
                    GD.Print($"[RADAR] Đã Instantiate thành công file 3D! Tên cục 3D là: {model.Name}");
                }

                HandPosition.AddChild(model);

                // MÁY DÒ 3: Kiểm tra xem nó có thực sự được gắn vào game không
                if (model.IsInsideTree())
                {
                    GD.Print("[RADAR] Đèn pin ĐÃ ĐƯỢC GẮN THÀNH CÔNG vào người Player!");
                }

                // 3. Reset tọa độ để nằm đúng lòng bàn tay
                model.Position = Vector3.Zero;
                model.Rotation = Vector3.Zero;

                // MÁY DÒ 4: Chốt tọa độ cuối cùng của cái đèn
                GD.Print($"[RADAR] Tọa độ gốc của đèn (so với tay): {model.Position}");
                GD.Print($"[RADAR] TỌA ĐỘ THỰC TẾ (GlobalPosition) của cái đèn trong map: {model.GlobalPosition}");

                // 4. Đặc quyền cho Đèn pin
                if (itemData.ItemID == "flashlight" && model is Flashlight fl)
                {
                    _currentFlashlight = fl;

                    _currentFlashlight.Freeze = true;
                    var collider = _currentFlashlight.GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
                    if (collider != null) collider.Disabled = true;

                    _currentFlashlight.TurnOn();
                    GD.Print("[RADAR] Bóng đèn đã được BẬT SÁNG (TurnOn) và Khóa Vật Lý (Freeze).");
                }

                GD.Print("--- KẾT THÚC LÔI ĐỒ ---\n");
            }
            else
            {
                GD.PrintErr($"[LỖI] Vật phẩm {itemData.ItemName} CHƯA CÓ FILE HandModel! (Hãy kéo file .tscn vào ô Hand Model của nó)");
            }
        }
    }
}