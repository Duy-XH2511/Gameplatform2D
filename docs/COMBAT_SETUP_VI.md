# Combat trong Unity

## Chạy thử

Mở `Assets/Scenes/SampleScene.unity`. Đi sang phải để chạm cây weapon màu xanh ở vị trí x = -1.6. Weapon sẽ gắn vào player. Nhấn chuột trái để bắn theo hướng player đang nhìn. Khi chưa có weapon, player không vào CombatState và không bắn. Mỗi lần nhấn bắn một spell; mặc định hồi chiêu 0.5 giây.

## Player

Trong component **Player**, mục **Combat**:

- **Spell Cooldown**: số giây chờ giữa hai lần bắn. `SpellCooldownRemaining` cho UI đọc countdown nếu cần.
- **Cast Duration**: thời gian giữ CombatState và WeaponCastState trước khi quay về di chuyển/đứng yên; mặc định 0.15 giây. Player vẫn di chuyển ngang khi cast. Nhảy/dash được xử lý sau khi cast xong.
- **Spell Spawn Offset**: vị trí sinh spell tính theo đơn vị world so với player; trục X tự đảo theo hướng nhìn.
- **Weapon Holder**: Transform tay/cầm weapon; để trống dùng Transform của player.
- **Held Weapon Offset**: vị trí weapon theo tọa độ local của holder.
- **Starting Weapon Prefab**: để trống để nhặt weapon trong scene; gán `WeaponPickup.prefab` nếu muốn bắt đầu với weapon.

Code có `EquipWeapon(Weapon weapon)`, `UnequipWeapon()`, `HasWeapon`, `TryEnterCombat()`. Cooldown không reset khi đổi state hoặc thay weapon. Không vào CombatState trong lúc Dash. Player chết sẽ ngừng nhận tấn công.

## Spell và weapon

Prefab mặc định nằm tại `Assets/Combat/Prefabs/`:

- `SpellProjectile.prefab`: chỉnh **Speed**, **Max Distance**, **Damage**, **Blocking Layers** trong component SpellProjectile.
- Đổi hình bằng **Sprite Renderer → Sprite**, chỉnh kích thước bằng **Transform → Scale**. Chỉnh **Circle Collider 2D → Radius** để vùng va chạm khớp hình.
- **Max Distance** tính từ nơi spell được sinh ra. Spell tự hủy khi bay hết khoảng cách, đụng ground/wall, hoặc gây sát thương một enemy. Sweep kiểm tra cả đoạn bay giữa hai physics tick để hạn chế xuyên mục tiêu khi tốc độ cao.
- `WeaponPickup.prefab`: **Spell Prefab** chọn spell được bắn; **Held Local Scale** chọn kích thước khi player cầm. SpriteRenderer là hình weapon. BoxCollider2D dùng để nhặt và tự tắt sau khi trang bị.
- Kéo WeaponPickup vào scene để tạo thêm weapon. Sao chép spell/weapon prefab để cấu hình nhiều loại.

Menu **Tools → Combat → Create Default Prefabs** tạo asset còn thiếu và bổ sung Animator/controller cho weapon mặc định nếu chưa có. Menu giữ cấu hình spell/weapon, clip hiện có và controller tùy chỉnh đã gán.

## State và animation của weapon

Weapon có state machine riêng tại `Assets/Combat/WeaponStates/`:

- **WeaponIdleState**: state mặc định, wand đứng thẳng khi chưa dùng chiêu.
- **WeaponCastState**: vào khi player bắn spell thành công; tự trở về Idle sau **Player → Cast Duration**. Thả weapon hoặc player chết sẽ hủy Cast và trở về Idle.
- Lần bắn bị chặn bởi cooldown không khởi động lại animation Cast.

`WeaponPickup.prefab` đã gắn Animator với controller `Assets/Combat/Animations/Weapon.controller`. Hai state Animator là **Weapon_Idle** và **Weapon_Cast**, được code chuyển trực tiếp, không cần đặt transition/parameter thủ công.

Chỉnh clip **Weapon_Idle.anim** và **Weapon_Cast.anim** trong cửa sổ Animation của Unity để đổi tư thế/động tác. Clip Cast mặc định dài 0.15 giây, nghiêng wand 55 độ rồi trở về thẳng. Khi đổi độ dài clip, chỉnh **Player → Cast Duration** cho khớp. Có thể thay clip bằng animation sprite của weapon.

Trong component **Weapon → Weapon Animations**, **Idle Animation Name** và **Cast Animation Name** phải khớp tên state trong controller nếu dùng Animator riêng. `Weapon.StateMachine.CurrentState` cho code đọc state hiện tại; `Weapon.CastState.RemainingTime` là thời gian còn lại của động tác.

## Enemy thường và boss

Enemy đã có component **Health**. Chỉnh **Max Health** trên từng object/prefab trước Play Mode, ví dụ enemy thường 3 HP và boss 100 HP. `Enemy.TakeDamage(int amount)` gọi `Health.TakeDamage(int amount)`. Khi HP về 0, enemy ngừng AI, animation và va chạm.

Thêm component **Enemy Health Bar** vào object chứa Health:

- **Kind = Normal**: thanh nằm trên enemy. **World Size** và **World Offset** dùng đơn vị world; thanh không đảo hoặc phóng to theo sprite của enemy.
- **Kind = Boss**: thanh nằm ở phía trên màn hình. Chỉnh **Boss Size**, **Boss Screen Offset** theo pixel trên canvas tham chiếu 1920×1080.
- **Fill Color**, **Background Color**: đổi màu thanh.

SampleScene đã gắn thanh Normal cho enemy hiện có. Với boss, dùng Enemy + Rigidbody2D + Collider2D cụ thể (Box/Capsule...) + Animator + Health + EnemyHealthBar, rồi đặt Kind = Boss và Max Health riêng. Tính năng này cấu hình HP/bars cho boss; hành vi boss dùng AI hiện có hoặc script boss của bạn.

## Input và kiểm tra

Action **Player/Attack** được bind `<Mouse>/leftButton`, nối Unity Event tới `Player.OnAttack`. Control scheme Keyboard có Mouse tùy chọn để PlayerInput nhận click. A/D, Space và Shift giữ theo input hiện có.

Chạy test logic: `dotnet run --project Tests/CombatTests/CombatTests.csproj`.

Chạy kiểm thử gameplay: `Tests/Unity/Run-CombatVerification.ps1`. Script dùng Unity đã cài ở `D:\Design_game\6000.3.24f1\Editor\Unity.exe`; có thể truyền `-UnityPath` nếu cài ở nơi khác. Script sao chép các component vào project tạm trong `Temp/CombatVerification`, kiểm tra bằng Unity Play Mode ở chế độ nền và ghi kết quả tại `Temp/CombatVerification/combat-verification-results.txt`. Project Unity hiện đang mở của bạn không bị chạy/dừng Play Mode bởi script này.

Đã xác nhận 36 kiểm tra gameplay Unity (gồm state/animation weapon) và test logic .NET; kiểm tra gameplay tự động dùng scene kiểm thử riêng. Mở lại SampleScene để xem các thay đổi scene từ file và chơi thử với map hiện có.
