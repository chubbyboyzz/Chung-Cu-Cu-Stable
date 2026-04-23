using Godot;
using System.Collections.Generic;
using ChungCuCu_Stable.Game.Scripts.Resources;

namespace ChungCuCu_Stable.Game.Scripts.UI
{
    public partial class InventoryUI : Control
    {
        [Export] public ItemList ItemListNode;

        [Signal] public delegate void ItemSelectedEventHandler(ItemData item);

        private List<ItemData> _currentItems = new List<ItemData>();

        public override void _Ready()
        {
            Visible = false;

            // Check xem ông đã gắn dây ItemList chưa
            if (ItemListNode == null)
            {
                GD.PrintErr("[UI LỖI NẶNG] Chưa gắn Node ItemList vào ô Item List Node trong Inspector!");
            }
            else
            {
                ItemListNode.ItemClicked += OnItemClicked;
            }
        }

        public void Open(List<ItemData> items)
        {
            Visible = true;

            if (ItemListNode == null) return; // Nếu lỗi chưa gắn dây thì dừng luôn

            ItemListNode.Clear();
            _currentItems = items;

            // --- LOG DEBUG BẮT ĐẦU ---
            GD.Print($"[UI DEBUG] Bắt đầu mở túi đồ. Tổng số đồ nhận được từ Player: {items.Count} món.");

            foreach (var item in items)
            {
                if (item == null)
                {
                    GD.PrintErr("[UI DEBUG] Báo động: Có 1 khoảng trống (null) trong danh sách đồ!");
                    continue;
                }

                string translatedName = Tr(item.ItemName);

                // In ra chi tiết món đồ chuẩn bị nhét vào bảng
                GD.Print($"[UI DEBUG] Đang nhét đồ vào bảng: Tên = '{translatedName}', ID = '{item.ItemID}', Có ảnh không? = {(item.Icon != null ? "CÓ" : "KHÔNG")}");

                ItemListNode.AddItem(translatedName, item.Icon);
            }

            // In ra xem thằng ItemList thực tế nó đang cầm bao nhiêu dòng
            GD.Print($"[UI DEBUG] Mở xong! Số ô hiện có trên bảng ItemList: {ItemListNode.ItemCount}");
            // ---------------------------
        }

        public void Close()
        {
            Visible = false;
        }

        private void OnItemClicked(long index, Vector2 atPosition, long mouseButtonIndex)
        {
            if (index < 0 || index >= _currentItems.Count) return;

            ItemData selectedItem = _currentItems[(int)index];
            EmitSignal(SignalName.ItemSelected, selectedItem);
            GD.Print($"[UI] Đã chọn: {Tr(selectedItem.ItemName)}");
        }
    }
}