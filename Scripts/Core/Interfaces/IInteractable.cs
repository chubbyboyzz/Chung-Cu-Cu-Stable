using Godot;

namespace ChungCuCu_Stable.Game.Scripts.Core.Interfaces
{
    // Đây là Interface (Hợp đồng tương tác)
    public interface IInteractable
    {
        // Hàm xử lý khi bấm nút tương tác (E)
        void Interact(Node interactor);

        // Hàm hiện dòng chữ gợi ý (VD: "Mở cửa")
        string GetInteractionPrompt();
    }
}