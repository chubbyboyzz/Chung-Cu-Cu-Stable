using Godot;
using System;
using System.Collections.Generic;
using ChungCuCu_Stable.Game.Scripts.Core.Interfaces;

namespace ChungCuCu_Stable.Game.Scripts.Characters
{
    public partial class Ghost : CharacterBody3D
    {
        [ExportGroup("Movement Settings")]
        [Export] public float PatrolSpeed = 2.0f;
        [Export] public float ChaseSpeed = 4.5f;
        [Export] public float Acceleration = 10.0f;
        [Export] public float Gravity = 9.8f;
        [Export] public float SearchTime = 5.0f; // Thời gian đứng chờ

        [ExportGroup("References")]
        [Export] public NavigationAgent3D NavAgent;
        [Export] public RayCast3D Eyes;
        [Export] public Area3D DoorDetectorNode;
        [Export] public Godot.Collections.Array<Node3D> PatrolPoints;

        // --- BIẾN LOGIC ---
        private Node3D _realPlayerNode;
        private Vector3 _lastKnownPosition;

        // Trạng thái (Chỉ được 1 cái true tại 1 thời điểm)
        private bool _isChasing = false;
        private bool _isSearching = false;

        private double _searchTimer = 0.0f;
        private int _currentPatrolIndex = 0;

        public bool IsChasing => _isChasing;
        public bool IsBusy = false;

        public override void _Ready()
        {
            GD.Print("--- [MA] KHỞI ĐỘNG V5 (FIX TRẠNG THÁI) ---");
            _realPlayerNode = GetTree().GetFirstNodeInGroup("Player") as Node3D;

            if (NavAgent != null)
            {
                NavAgent.PathDesiredDistance = 1.0f;
                NavAgent.TargetDesiredDistance = 1.0f;
            }

            if (DoorDetectorNode == null)
                DoorDetectorNode = GetNodeOrNull<Area3D>("DoorDetector");

            Callable.From(ActorSetup).CallDeferred();
        }

        private async void ActorSetup()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            SetNextPatrolTarget();
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector3 velocity = Velocity;
            if (!IsOnFloor()) velocity.Y -= Gravity * (float)delta;

            if (IsBusy)
            {
                MoveToTarget(delta, ref velocity, ChaseSpeed);
                Velocity = velocity;
                MoveAndSlide();
                return;
            }

            // ================================================================
            // 1. ƯU TIÊN TUYỆT ĐỐI: MẮT THẦN (VISION)
            // ================================================================
            // Luôn kiểm tra tầm nhìn, bất kể đang làm gì
            bool canSeeNow = CheckVision();

            if (canSeeNow)
            {
                // Nếu thấy -> Hủy mọi trạng thái khác -> Kích hoạt ĐUỔI
                if (!_isChasing) GD.Print("[MA] THẤY PLAYER! -> HỦY TÌM/TUẦN -> DÍ THEO!");

                _isChasing = true;
                _isSearching = false; // Tắt ngay trạng thái tìm
                _searchTimer = 0.0f;

                // Cập nhật vị trí đuổi
                _lastKnownPosition = _realPlayerNode.GlobalPosition;
            }

            // ================================================================
            // 2. MÁY TRẠNG THÁI (STATE MACHINE)
            // ================================================================

            // --- TRẠNG THÁI 1: ĐANG ĐUỔI ---
            // --- TRẠNG THÁI 1: ĐANG ĐUỔI ---
            if (_isChasing)
            {
                NavAgent.TargetPosition = _lastKnownPosition;
                float distToLKP = GlobalPosition.DistanceTo(_lastKnownPosition);

                // Nếu đã đến sát đít (1.2m)
                if (distToLKP < 1.2f)
                {
                    // TRƯỜNG HỢP 1: ĐẾN NƠI VÀ VẪN THẤY PLAYER (Nó đang đứng trêu ngươi mình)
                    if (canSeeNow)
                    {
                        GD.Print("[MA] BẮT ĐƯỢC MÀY RỒI! -> GAME OVER");

                        // 1. Dừng Ma lại
                        velocity = Vector3.Zero;

                        // 2. Gọi hàm Game Over / Jumpscare trực tiếp tại đây
                        // Vì Player không trốn, nên Ma không cần tương tác với Tủ, mà tóm cổ Player luôn.
                        CatchPlayer();
                    }
                    // TRƯỜNG HỢP 2: ĐẾN NƠI NHƯNG KHÔNG THẤY (Nó đã kịp rẽ vào góc hoặc chui tủ)
                    else
                    {
                        GD.Print($"[MA] Mất dấu tại đích -> Bắt đầu tìm kiếm 5s.");

                        // Check xem có cái tủ nào ở đó không để mở
                        CheckInteractionsImmediate();

                        _isChasing = false;
                        _isSearching = true;
                        _searchTimer = 0.0f;
                    }
                    velocity = Vector3.Zero;
                }
                else
                {
                    MoveToTarget(delta, ref velocity, ChaseSpeed);
                }
            }
            // --- TRẠNG THÁI 2: ĐANG TÌM (ĐỨNG CHỜ 5S) ---
            else if (_isSearching)
            {
                velocity = Vector3.Zero; // Đứng im
                _searchTimer += delta;

                // Nếu hết 5s
                if (_searchTimer >= SearchTime)
                {
                    GD.Print("[MA] Hết 5s -> Không thấy gì -> Quay về đi tuần.");

                    _isSearching = false; // Tắt Tìm
                    _isChasing = false;   // Đảm bảo tắt Đuổi

                    // QUAY LẠI ĐI TUẦN
                    SetNextPatrolTarget();
                }
            }
            // --- TRẠNG THÁI 3: ĐI TUẦN (MẶC ĐỊNH) ---
            else
            {
                if (PatrolPoints != null && PatrolPoints.Count > 0)
                {
                    if (NavAgent.IsNavigationFinished())
                    {
                        _currentPatrolIndex = (_currentPatrolIndex + 1) % PatrolPoints.Count;
                        SetNextPatrolTarget();
                    }
                    MoveToTarget(delta, ref velocity, PatrolSpeed);
                }
            }

            Velocity = velocity;
            MoveAndSlide();
        }

        // --- CÁC HÀM HỖ TRỢ (GIỮ NGUYÊN) ---
        private bool CheckVision()
        {
            if (_realPlayerNode == null) return false;
            if (_realPlayerNode is Player playerScript && playerScript.IsHiding) return false;
            if (Eyes == null) return false;

            Vector3 targetPos = _realPlayerNode.GlobalPosition + Vector3.Up * 1.5f;
            Eyes.LookAt(targetPos);
            Eyes.ForceRaycastUpdate();

            if (Eyes.IsColliding())
            {
                var collider = Eyes.GetCollider();
                if (collider is Node node && node.IsInGroup("Player")) return true;
            }
            return false;
        }

        private void CheckInteractionsImmediate()
        {
            if (DoorDetectorNode == null) return;
            var bodies = DoorDetectorNode.GetOverlappingBodies();
            foreach (var body in bodies)
            {
                if (body is IGhostInteractable interactable) interactable.OnGhostInteract(this);
            }
        }

        public void ForceStopChasing() { if (_isChasing) GD.Print("[GHOST] Player trốn thoát!"); }

        public void CommandMoveTo(Vector3 targetPos)
        {
            IsBusy = true;
            _isChasing = false;
            _isSearching = false;
            NavAgent.TargetPosition = targetPos;
        }

        private void _on_door_detector_body_entered(Node3D body)
        {
            if (body is IGhostInteractable interactableObject) interactableObject.OnGhostInteract(this);
        }

        private void SetNextPatrolTarget()
        {
            if (PatrolPoints == null || PatrolPoints.Count == 0) return;
            NavAgent.TargetPosition = PatrolPoints[_currentPatrolIndex].GlobalPosition;
        }

        private void MoveToTarget(double delta, ref Vector3 velocity, float currentSpeed)
        {
            if (NavAgent == null) return;
            if (!NavAgent.IsNavigationFinished())
            {
                Vector3 nextPathPosition = NavAgent.GetNextPathPosition();
                Vector3 direction = (nextPathPosition - GlobalPosition).Normalized();
                direction.Y = 0;
                velocity.X = Mathf.Lerp(velocity.X, direction.X * currentSpeed, Acceleration * (float)delta);
                velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * currentSpeed, Acceleration * (float)delta);
                if (direction.Length() > 0.001f)
                {
                    Vector3 lookTarget = new Vector3(nextPathPosition.X, GlobalPosition.Y, nextPathPosition.Z);
                    LookAt(lookTarget, Vector3.Up);
                }
            }
            else
            {
                velocity.X = Mathf.MoveToward(velocity.X, 0, currentSpeed);
                velocity.Z = Mathf.MoveToward(velocity.Z, 0, currentSpeed);
            }
        }

        private void CatchPlayer()
        {
            // Tạm thời in ra log và Pause game hoặc Restart
            GD.PrintErr("--- 💀 NGƯỜI CHƠI ĐÃ BỊ BẮT! 💀 ---");

            // Code ví dụ để xử lý thua:
            // GetTree().Paused = true; // Dừng game
            // Hoặc load lại màn chơi:
            // GetTree().ReloadCurrentScene();
        }
    }
}