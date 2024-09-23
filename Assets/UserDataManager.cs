using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    // Tạo instance duy nhất của class này
    public static UserDataManager Instance { get; private set; }

    // Biến User12Words để lưu 12 từ của người dùng
    public string User12Words;
    public float UserAptosBalance;

    // Đảm bảo rằng instance chỉ tồn tại một lần
    private void Awake()
    {
        // Kiểm tra nếu đã có một instance của class này
        if (Instance != null && Instance != this)
        {
            // Hủy đối tượng mới nếu đã tồn tại một instance
            Destroy(this.gameObject);
        }
        else
        {
            // Gán instance và không hủy khi chuyển cảnh
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // Các phương thức khác có thể thêm vào đây để quản lý dữ liệu User12Words
    public void SetUser12Words(string words)
    {
        User12Words = words;
    }

    public void SetUserAptosBalance(float aptosBalance)
    {
        UserAptosBalance = aptosBalance;
    }

    public float GetUserAptosBalance()
    {
        return UserAptosBalance;
    }
}
