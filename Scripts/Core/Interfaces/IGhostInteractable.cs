using Godot;

namespace ChungCuCu_Stable.Game.Scripts.Core.Interfaces
{
    public interface IGhostInteractable
    {
        // Ma sẽ gọi hàm này khi chạm vào đồ vật
        void OnGhostInteract(Node ghost);
    }
}