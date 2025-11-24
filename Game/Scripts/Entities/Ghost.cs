using Godot;
using System;

namespace ChungCuCu_Stable.Game.Scripts.Entities
{
    public partial class Ghost : CharacterBody3D
    {
        [Export] public float Speed = 4.0f;
        [Export] public float Acceleration = 10.0f;
        [Export] public float Gravity = 9.8f;
        [Export] public NavigationAgent3D NavAgent;

        private Node3D _targetPlayer;

        // Biến mới: Để đánh dấu ma đang bận làm nhiệm vụ (ví dụ: đi mở tủ)
        public bool IsBusy = false;

        public override void _Ready()
        {
            _targetPlayer = GetTree().GetFirstNodeInGroup("Player") as Node3D;
            Callable.From(ActorSetup).CallDeferred();
        }

        private async void ActorSetup()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector3 velocity = Velocity;
            if (!IsOnFloor()) velocity.Y -= Gravity * (float)delta;

            // --- NẾU ĐANG BẬN (IS BUSY) ---
            // Thì chỉ di chuyển theo NavAgent đã được set, bỏ qua logic tìm Player
            if (IsBusy)
            {
                MoveToTarget(delta, ref velocity); // Hàm di chuyển tách riêng
                Velocity = velocity;
                MoveAndSlide();
                return; // Dừng hàm, không chạy logic đuổi bắt bên dưới
            }
            // ------------------------------

            if (_targetPlayer != null)
            {
                var playerScript = _targetPlayer as Player;
                if (playerScript != null && playerScript.IsHiding)
                {
                    velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
                    velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
                    Velocity = velocity;
                    MoveAndSlide();
                    return;
                }

                // Logic đuổi bắt bình thường
                NavAgent.TargetPosition = _targetPlayer.GlobalPosition;
                MoveToTarget(delta, ref velocity);
            }

            Velocity = velocity;
            MoveAndSlide();
        }

        // Tách logic di chuyển ra hàm riêng để tái sử dụng
        private void MoveToTarget(double delta, ref Vector3 velocity)
        {
            if (!NavAgent.IsNavigationFinished())
            {
                Vector3 nextPathPosition = NavAgent.GetNextPathPosition();
                Vector3 direction = (nextPathPosition - GlobalPosition).Normalized();
                direction.Y = 0;

                velocity.X = Mathf.Lerp(velocity.X, direction.X * Speed, Acceleration * (float)delta);
                velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * Speed, Acceleration * (float)delta);

                if (direction.Length() > 0.001f)
                {
                    Vector3 lookTarget = new Vector3(nextPathPosition.X, GlobalPosition.Y, nextPathPosition.Z);
                    LookAt(lookTarget, Vector3.Up);
                }
            }
            else
            {
                velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
                velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
            }
        }

        // --- HÀM ĐỂ CÁI TỦ GỌI ---
        public void CommandMoveTo(Vector3 targetPos)
        {
            IsBusy = true; // Vào chế độ kịch bản
            NavAgent.TargetPosition = targetPos; // Đi đến điểm chỉ định (Cửa tủ)
        }
    }
}