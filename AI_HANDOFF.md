# Tóm tắt dự án để AI tiếp tục lập trình

## Tổng quan

- Dự án game platformer 2D bằng Unity **6000.3.24f1**, dùng Universal Render Pipeline 2D, Input System, Physics 2D và Tilemap.
- Scene chính và duy nhất trong Build Settings: `Assets/Scenes/SampleScene.unity`.
- Gameplay hiện có: Player di chuyển ngang, nhảy đôi, dash; Enemy tuần tra, phát hiện và đuổi Player. Chưa thấy logic chiến đấu, máu, sát thương hay kết thúc màn trong các script hiện tại.

## Vị trí code và asset quan trọng

| Phần | Đường dẫn | Vai trò |
| --- | --- | --- |
| Player | `Assets/Player/Scripts/` | `Player.cs`, state machine và các state Idle, Move, Jump, Fall, Dash. |
| Player input | `Assets/Player/Input/Player.inputactions` | Action map `Player`; scene dùng `PlayerInput` với Unity Events gọi `OnMove`, `OnJump`, `OnDash`. |
| Player animation | `Assets/Player/Animations/` | Controller và clip `Player_Idle`, `Player_Walk`, `Player_Jump`, `Player_Dash`. |
| Enemy | `Assets/Enemy/Scripts/` | `Enemy.cs`, state machine và các state Idle, Patrol, Chase. |
| Enemy animation | `Assets/Enemy/Animations/` | Các controller, clip Idle/Walk; scene hiện tham chiếu `Enemy 3.controller`. |
| Công cụ Editor | `Assets/Enemy/Editor/EnemyAnimationGenerator.cs` | Menu `Tools/Enemy/Generate Golem Animations`; tạo/cập nhật clip và `Enemy.controller`, rồi lưu scene. |
| Bản đồ | `Assets/Background/`, `Assets/Scenes/SampleScene.unity` | Woods tileset, Tilemap, nền và terrain. |

## Luồng gameplay hiện tại

- `Player` lấy `Rigidbody2D`/`Animator` trong `Awake`, khởi tạo state machine tại `Start`, cập nhật state trong `Update`. Input dùng `PlayerInput` và `InputAction.CallbackContext`.
- Phím A/D điều khiển ngang; W/S có trong binding Move nhưng code chỉ dùng trục X. Space để nhảy, tối đa 2 lần. Dash được binding với **Ctrl** trong file `.inputactions`; thời gian dash 0,2 giây, cooldown 0,5 giây, tạm tắt trọng lực. Player lật hướng bằng dấu của `transform.localScale.x`.
- Enemy tìm GameObject có tag `Player` trong `Start`, bắt đầu ở `PatrolState`, cập nhật state trong `FixedUpdate`. Nó dùng khoảng cách 2D để phát hiện Player; không có kiểm tra line of sight. Khi tuần tra, nó quay lại nếu không có nền phía trước hoặc gặp tường; khi phát hiện Player thì đuổi, tránh bước khỏi mép và dừng trước tường.
- `EnemyIdleState` đã có nhưng chưa có chuyển trạng thái nào đi vào state này.
- Cả hai hệ thống animation chuyển clip bằng tên state qua `Animator.CrossFade`; nếu đổi tên state trong controller cần sửa tên trong code tương ứng.

## Cấu hình scene hiện tại

- Scene có Player (tag `Player`), Enemy (tag `Enemy`), camera, nền woods, Tilemap `Terrain` đang bật ở layer `Ground` (6), và các object ground/wall khác. Tilemap `Ground` riêng đang tắt.
- Player: `moveSpeed=5`, `jumpForce=10`, `maxJumpCount=2`, `dashSpeed=10`, `dashDuration=0.2`, `dashCooldown=0.5`; ground check dùng layer `Ground`.
- Enemy: `moveSpeed=2`, `detectionRange=2.2`; `groundLayer` của nó gồm `Ground` và `Wall` (bits 192). Các Transform `groundCheck` và `wallCheck` được gán trong scene.
- Scene tham chiếu `Enemy 3.controller` (asset chưa được Git theo dõi). Công cụ Editor ở trên lại tạo/cập nhật `Enemy.controller`; cần kiểm tra controller nào là bản chính trước khi chỉnh animation Enemy.
- Tên action Dash được serialize trong scene vẫn hiển thị `[/Keyboard/q]`, còn binding thực tế trong `Player.inputactions` là `<Keyboard>/ctrl`. Kiểm tra lại trong Unity Editor/Play Mode nếu dash không hoạt động như mong muốn.
- Action `Attack` đã khai báo trong input asset nhưng chưa có binding phím hữu dụng, event trong scene hoặc code xử lý.

## Tình trạng repo và lưu ý khi tiếp tục

- Working tree đang có nhiều thay đổi **chưa commit**: scene, input asset, animation Enemy, meta của tileset; có thêm WoodsMapTiles, Tilemap prefab, Rule Tile, `Enemy 2/3.controller` và scene `_Recovery`. Không xóa, reset hoặc ghi đè những phần này khi tiếp tục làm việc.
- Dự án chưa có script test trong `Assets`. Tài liệu này được tổng hợp bằng cách đọc file, **chưa xác nhận hành vi bằng Play Mode hoặc build Unity**.
- Khi sửa scene/asset, mở bằng Unity **6000.3.24f1** và kiểm tra Console, liên kết Inspector và Play Mode. `Packages/manifest.json` có package `com.gamelovers.mcp-unity` từ GitHub, nên lần mở dự án có thể cần tải package.
- Nếu phát triển tính năng mới, hãy bắt đầu bằng việc hỏi người dùng muốn ưu tiên phần nào. Các hạng mục chưa có rõ nhất là Attack/combat, tương tác Player-Enemy, HP/damage và luồng màn chơi.
