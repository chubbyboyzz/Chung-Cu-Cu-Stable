using Godot;

// Namespace này phải khớp với cấu trúc thư mục trong Visual Studio
// Nếu VS báo đỏ dòng này, cậu để chuột vào và ấn Alt+Enter để nó tự sửa
namespace ChungCuCu_Stable.Game.Scripts.Core
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