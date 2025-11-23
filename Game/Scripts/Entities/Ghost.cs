using Godot;
using System;

namespace ChungCuCu_Stable.Game.Scripts.Entities
{
    public partial class Ghost : CharacterBody3D
    {
        [Export] public float Speed = 7.5f; // Chậm hơn Player (5.0) một tí để còn chạy thoát được
        [Export] public float Gravity = 9.8f;

        // Biến để lưu tham chiếu tới Player
        private Node3D _targetPlayer;

        public override void _Ready()
        {
            _targetPlayer = GetTree().GetFirstNodeInGroup("Player") as Node3D;

            if (_targetPlayer == null)
            {
                // Fallback: Tìm node gốc của scene rồi tìm con tên Player
                _targetPlayer = GetParent().GetNodeOrNull<Node3D>("Player");
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector3 velocity = Velocity;

            // 1. Áp dụng trọng lực (để ma đi bộ dưới đất, nếu muốn ma bay thì bỏ dòng này)
            if (!IsOnFloor())
            {
                velocity.Y -= Gravity * (float)delta;
            }

            // 2. Logic đuổi theo
            if (_targetPlayer != null)
            {
                // Tính vector hướng từ Ma tới Player
                Vector3 direction = (_targetPlayer.GlobalPosition - GlobalPosition).Normalized();

                // Chỉ lấy hướng X và Z 
                direction.Y = 0;
                direction = direction.Normalized();

                // Di chuyển
                velocity.X = direction.X * Speed;
                velocity.Z = direction.Z * Speed;

                // Xoay mặt về phía Player (tạo hiệu ứng nhìn chằm chằm)
                // Dùng LookAt
                LookAt(new Vector3(_targetPlayer.GlobalPosition.X, GlobalPosition.Y, _targetPlayer.GlobalPosition.Z), Vector3.Up);
            }

            Velocity = velocity;
            MoveAndSlide();
        }
    }
}