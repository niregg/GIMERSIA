# GIMERSIA INSPIRE 2026 — PROJECT ARCHITECTURAL DIRECTIVES (UNITY 6 C# & MULTI-DCC PIPELINE)

Dokumen ini mendefinisikan standar rekayasa perangkat lunak, optimasi memori, arsitektur sistem, dan integrasi pipeline aset untuk proyek kompetisi nasional **Gimersia Inspire 2026** (`d:\Gimersia`). Seluruh kode C#, aset Blender (`bpy`), dan skrip otomasi DaVinci Resolve Studio 19 wajib mematuhi spesifikasi ini tanpa pengecualian.

---

## 1. Paradigma & Arsitektur Kode (Anti-Spaghetti & Decoupled Systems)

### 1.1 Hierarchical / Finite State Machine (HFSM / FSM)
- **Larangan Boolean Flags:** Dilarang menggunakan rangkaian boolean (`isJumping`, `isAttacking`, `isDead`, `isPaused`) untuk mengatur status entitas atau alur game.
- **State Encapsulation:** Gunakan polimorfisme berbasis interface `IState` (`OnEnter()`, `OnTick(float deltaTime)`, `OnFixedTick(float fixedDeltaTime)`, `OnExit()`) atau struct-based state machine dengan transisi deterministik.
- **Game Loop State Machine:** Siklus hidup permainan (`Boot` $\rightarrow$ `MainMenu` $\rightarrow$ `Gameplay` $\rightarrow$ `Paused` $\rightarrow$ `LevelComplete` $\rightarrow$ `GameOver`) dikelola oleh `GameFlowStateMachine` yang mendelegasikan tanggung jawab ke subsistem spesifik, bukan mengeksekusi logika gameplay secara langsung.

### 1.2 Event-Driven Architecture (ScriptableObject Event Channels & C# Actions)
- **Zero Direct Cross-System Coupling:** Sistem `Player`, `UI`, `Audio`, dan `LevelManager` dilarang saling menyimpan referensi langsung (`[SerializeField] private UIManager uiManager;` di dalam `PlayerController` dilarang keras).
- **ScriptableObject Event Bus (Ryan Hipple Pattern):** Gunakan aset `EventChannelSO` (`VoidEventChannelSO`, `IntEventChannelSO`, `FloatEventChannelSO`, `AudioCueEventChannelSO`) sebagai broker pesan lintas scene dan lintas prefab.
- **Deterministic Subscription Lifecycle:** Setiap langganan event (`+=`) pada `OnEnable()` wajib memiliki pasangan pelepasan (`-=`) yang identik pada `OnDisable()` atau `OnDestroy()` untuk mencegah memory leak dan pemanggilan objek yang telah dihancurkan (`MissingReferenceException`).

### 1.3 Larangan Anti-Pattern & Resolusi Dependensi
- **Ban God Objects:** `GameManager` dilarang melampaui 150 baris atau menampung logika kalkulasi skor, fisika, UI, dan audio sekaligus. Pecah menjadi komponen berfokus tunggal (`ScoreSystem`, `WaveSpawner`, `LevelTimer`).
- **Ban Runtime Reflection & Scene Graph Scans:** Penggunaan `GameObject.Find()`, `FindObjectOfType<T>()`, `FindObjectsOfType<T>()`, `SendMessage()`, dan `GetComponent<T>()` di dalam siklus runtime (`Update`, `FixedUpdate`, `LateUpdate`, atau saat collision) dilarang mutlak.
- **Lightweight Service Locator / Bootstrapper:** Gunakan `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)` atau Persistent Bootstrapper Scene untuk menginisialisasi layanan inti (`IAudioService`, `IInputService`, `IPoolService`) ke dalam `ServiceLocator` bertipe kuat (`ServiceLocator.Get<IAudioService>()`).
- **Component Caching:** Seluruh referensi komponen lokal wajib di-cache pada fase `Awake()` atau divalidasi di Editor melalui `OnValidate()`.

---

## 2. Zero-Allocation & Performance Budget (WebGL & Windows 60+ FPS)

Target utama distribusi adalah **WebGL (itch.io browser embed)** dan **Windows Standalone**. Lingkungan WebGL (WebAssembly/JS Heap) sangat sensitif terhadap lonjakan Garbage Collector (GC spikes) dan keterbatasan single-threaded CPU.

### 2.1 Zero-GC Hot Paths (`Update`, `FixedUpdate`, `LateUpdate`)
- **Zero Heap Allocation per Frame:** Dilarang melakukan instansiasi class (`new MyClass()`), boxing value types (`object`, non-generic interfaces), alokasi array dinamis, atau penggunaan `LINQ` (`System.Linq` dilarang di seluruh runtime script) di dalam loop per-frame.
- **Lambda & Closure Ban in Hot Paths:** Hindari penggunaan anonymous delegates/lambdas yang menangkap variabel lokal (capturing closures) di dalam loop karena kompilator C# akan mengalokasikan class `DisplayClass` di heap.
- **Native Async (`Awaitable` & Coroutine Caching):**
  - Pada Unity 6+, prioritaskan `UnityEngine.Awaitable` (`await Awaitable.WaitForSecondsAsync()`, `await Awaitable.NextFrameAsync()`) yang terintegrasi dengan engine pool dan bebas alokasi `Task` standar, dengan selalu meneruskan `destroyCancellationToken`.
  - Jika menggunakan Coroutine (`IEnumerator`), seluruh objek yield (`WaitForSeconds`, `WaitForFixedUpdate`) wajib di-cache sebagai field `static readonly` atau instance field di `Awake()`. Dilarang menulis `yield return new WaitForSeconds(0.1f);` secara langsung di dalam loop.

### 2.2 NonAlloc Physics & Spatial Queries
- **Larangan Alokasi Array Fisika:** Dilarang menggunakan `Physics.RaycastAll`, `Physics.OverlapSphere`, atau `Physics.SphereCastAll` yang mengembalikan array baru setiap pemanggilan.
- **Pre-Allocated Buffers:** Wajib menggunakan varian `NonAlloc` atau pre-allocated buffer:
  ```csharp
  private readonly Collider[] _overlapBuffer = new Collider[16];
  private readonly RaycastHit[] _raycastBuffer = new RaycastHit[8];
  ```
- **Explicit LayerMask:** Setiap operasi raycast atau overlap wajib menyertakan parameter `LayerMask` eksplisit dan jarak maksimum (`maxDistance`) untuk memangkas beban broadphase PhysX.
- **Shader Property Hashing:** Dilarang mengubah parameter material menggunakan string literal (`material.SetFloat("_FlashAmount", 1f)`). Wajib menggunakan `static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");`.

### 2.3 High-Performance Object Pooling (`UnityEngine.Pool`)
- **Larangan `Instantiate` / `Destroy` di Gameplay:** Seluruh objek transien—proyektil, partikel VFX, floating combat text, decal, musuh wave, dan `AudioSource` emitter—wajib dikelola melalui `UnityEngine.Pool.ObjectPool<T>`.
- **Pre-Warming:** Pool harus melakukan pre-warm pada saat inisialisasi scene/level (`DefaultCapacity` disesuaikan dengan estimasi beban puncak) agar tidak terjadi kompilasi shader atau alokasi memori mendadak saat aksi pertama pemain.

### 2.4 String & UI Rendering Optimization
- **Ban String Concatenation (`+` / `$""`) pada Update UI:** Mengubah teks timer atau skor setiap frame dengan string interpolation akan memicu GC spike.
- **Dirty-Check & `SetText`:** Hanya perbarui elemen UI ketika nilai integer/float berubah (melalui event channel), dan gunakan metode `TMP_Text.SetText("{0:00}:{1:00}", minutes, seconds)` atau `StringBuilder` dengan kapasitas tetap.

---

## 3. Modern Input & Camera Architecture

### 3.1 Unity New Input System (`UnityEngine.InputSystem`)
- **Ban Legacy Input:** Penggunaan `UnityEngine.Input.GetKey`, `Input.GetAxis`, atau `Input.GetMouseButton` dilarang keras.
- **InputReader ScriptableObject Architecture:**
  - Bungkus `InputActionAsset` (C# generated wrapper class) di dalam `InputReaderSO` yang mengimplementasikan `IPlayerActions` dan `IUIActions`.
  - Ekspos C# events (`event Action<Vector2> MoveEvent`, `event Action JumpPerformedEvent`, `event Action JumpCanceledEvent`).
  - Sediakan metode switching action map eksplisit (`EnableGameplayInput()`, `EnableUIInput()`, `DisableAllInput()`) agar kontrol pemain otomatis terkunci saat menu pause atau cutscene aktif.
- **Input Buffering & Coyote Time:** Untuk aksi responsif (lompat, dash, serang, interaksi), terapkan input buffer (`0.10s–0.15s`) dan coyote time (`0.08s–0.12s`) menggunakan timestamp `Time.time` tanpa alokasi memori.

### 3.2 Cinemachine & Trauma-Based Camera System
- **Cinemachine Virtual Cameras (`CinemachineCamera`):** Seluruh pergerakan kamera dikendalikan oleh Cinemachine dengan damping yang terkalibrasi (hindari gerakan kamera kaku 1:1 tanpa redaman kecuali untuk aiming presisi tinggi).
- **Trauma-Based Perlin Shake ($T^2$ / $T^3$):**
  - Implementasikan sistem guncangan kamera berbasis nilai `trauma` $\in [0, 1]$ yang meluruh secara linier terhadap waktu, namun menghasilkan amplitudo guncangan kuadratik atau kubik ($\text{shake} = \text{trauma}^2$ atau $\text{trauma}^3$).
  - Gunakan `CinemachineBasicMultiChannelPerlin` atau offset rotasi/posisi berbasis Perlin noise multi-frekuensi menggunakan `Time.unscaledTime` agar tetap konsisten saat hit-stop.

---

## 4. Game Feel ("Juice") & Tactile Engineering Standards

Setiap interaksi utama pemain (serangan, tumbukan, pengambilan item, klik tombol UI) wajib memicu minimal **3 lapisan umpan balik simultan** (Visual, Kinetik, Audio).

### 4.1 Time Dilation & Hit-Stop (`UnscaledTime` Discipline)
- **Frame Freeze / Hit-Stop:** Saat terjadi impact signifikan, turunkan `Time.timeScale` ke `0.0f–0.05f` selama `0.02s–0.06s` menggunakan timer berbasis `Time.unscaledDeltaTime`, lalu pulihkan ke `1.0f` secara deterministik.
- **Pause Safety Invariant:** Sistem `HitStopManager` wajib menyimpan referensi state pause sehingga pemulihan hit-stop tidak pernah membuka kunci `Time.timeScale = 0f` ketika pemain sedang berada di layar Pause.

### 4.2 Physically Grounded Tweening & Procedural Secondary Motion
- **Ban Linear Easing:** Dilarang menggunakan interpolasi `linear` untuk pergerakan elemen UI, popup skor, atau respons transform objek.
- **Spring & Custom Easing Curves:** Gunakan kurva elastis (`OutBack`, `OutQuint`, `OutElastic`, atau spring damper analitis dengan `stiffness: 350–500`, `dampingRatio: 0.75–0.85`).
- **Squash & Stretch:** Saat entitas melompat, mendarat, atau menerima hit, terapkan deformasi skala non-uniform yang menjaga volume ($\text{Scale}_X \times \text{Scale}_Y \times \text{Scale}_Z \approx 1.0$), misalnya kompresi mendarat `(1.25f, 0.64f, 1.25f)` yang kembali ke `(1f, 1f, 1f)` dengan damped spring.
- **Hit Flash:** Gunakan `MaterialPropertyBlock` (bukan duplikasi material instance) untuk memicu kilatan warna putih/emissive selama `0.05s–0.08s` pada renderer target.

### 4.3 Modular Audio Trigger System
- **`AudioCueSO` Configuration:** Setiap efek suara direpresentasikan oleh `AudioCueSO` yang menyimpan array variasi `AudioClip[]`, rentang pitch acak (`0.93f–1.07f`), rentang volume acak (`0.92f–1.00f`), spatial blend (`2D` vs `3D`), dan kategori `AudioMixerGroup` (`Master`, `BGM`, `SFX`, `UI`).
- **Pooled Sound Emitters:** Pemutaran SFX dilakukan oleh pool `SoundEmitter` yang otomatis mengembalikan dirinya ke `ObjectPool` setelah `clip.length / pitch` selesai dieksekusi.

---

## 5. Kolaborasi Tim (3 Anggota) & Asset Pipeline Governance

### 5.1 Struktur Direktori Standar (`Assets/_Project/`)
Seluruh aset internal tim wajib berada di dalam namespace `Gimersia` dan direktori `Assets/_Project/` untuk memisahkan kode/aset asli dari third-party packages:
```text
Assets/
└── _Project/
    ├── Audio/
    │   ├── BGM/
    │   ├── SFX/
    │   └── Mixers/
    ├── Data/
    │   ├── Configs/          # Immutable ScriptableObject configs
    │   ├── Events/           # EventChannelSO assets
    │   └── AudioCues/        # AudioCueSO assets
    ├── Materials/
    ├── Models/
    │   ├── Characters/
    │   ├── Environment/
    │   └── Props/
    ├── Prefabs/
    │   ├── Actors/
    │   ├── Environment/
    │   ├── UI/
    │   └── VFX/
    ├── Scenes/
    │   ├── 00_Bootstrap.unity
    │   ├── 01_MainMenu.unity
    │   └── 02_Gameplay.unity
    ├── Scripts/
    │   ├── Core/             # StateMachine, ServiceLocator, EventChannels, Pooling
    │   ├── Input/            # InputReaderSO, Generated Input Actions
    │   ├── Gameplay/         # Player, Mechanics, AI, Level Systems
    │   ├── Juice/            # CameraShake, HitStop, ProceduralSpring, HitFlash
    │   ├── Audio/            # AudioManager, SoundEmitter, AudioCueSO
    │   ├── UI/               # HUD, Menus, Tweened Panels
    │   └── Editor/           # Custom Inspectors, Asset Postprocessors, Bridge Tools
    ├── Shaders/
    └── UI/
        ├── Fonts/
        └── Sprites/
```

### 5.2 Kontrak Pipeline Aset: Anggota 1 (Blender $\rightarrow$ Unity)
- **Unit & Scale:** `1 Unit = 1 Meter`, Metric scale `1.0`. Wajib menjalankan `Ctrl+A -> Apply All Transforms` (Location, Rotation, Scale = `1, 1, 1`) sebelum ekspor.
- **Coordinate System (FBX Export):** Forward: `-Z Forward`, Up: `Y Up`, `Apply Transform` diaktifkan untuk mencegah rotasi `-89.98` derajat di Unity.
- **Pivot Point Placement:**
  - Karakter & Prop Lantai: Pivot wajib berada tepat di **titik tengah dasar (Bottom-Center / $Y = 0$)** agar penempatan di level oleh Anggota 3 akurat terhadap permukaan tanah.
  - Prop Dinding / Engsel Pintu: Pivot berada pada sumbu engsel atau titik tempel.
- **Geometry & Shading Hygiene:** Wajib menerapkan `Bevel` tipis (`2–3 segments`, `harden_normals`) pada tepi keras agar menangkap pantulan cahaya URP, serta menghapus ngon dan face internal yang tidak terlihat.
- **Naming Convention:** `SM_<Category>_<Name>_<Variant>` untuk Static Mesh (contoh: `SM_Env_Pillar_01`), `SK_<Name>` untuk Skeletal Mesh, `T_<Name>_<MapType>` untuk tekstur (`_BaseMap`, `_Normal`, `_MaskMap`).

### 5.3 Kontrak Pipeline Level & Audio: Anggota 3 (Level/Sound/QA $\rightarrow$ Unity)
- **Prefab-Only Scene Assembly:** Anggota 3 wajib menyusun level di scene menggunakan **Prefab** dari `Assets/_Project/Prefabs/`, bukan menarik file `.fbx` mentah langsung ke Hierarchy.
- **Audio Normalization & Format:**
  - `SFX`: `.wav` (16-bit, 44.1kHz/48kHz), mono untuk 3D spatial sound, dipotong rapat di awal (*zero leading silence* $< 5\text{ms}$) agar respons input instan, di-set `Decompress On Load` di Unity.
  - `BGM`: `.ogg` / `.wav`, di-set `Streaming` (Windows) atau `Compressed In Memory` (Vorbis quality `70%` untuk kompatibilitas penuh WebGL), seamless loop points terverifikasi, dinormalisasi ke `-14 LUFS`.

---

## 6. Strict Anti-AI-Slop Code Hygiene

1. **Zero Incomplete Code:** Dilarang menulis `// TODO`, `// implement here`, `/* ... */`, atau metode kosong di dalam file `.cs` maupun `.py`. Setiap skrip yang ditulis harus langsung kompilasi bersih tanpa warning (`CS0168`, `CS0414`, `CS0649`).
2. **Immutable Configuration Data:** Seluruh parameter tuning gameplay (kecepatan gerak, gravitasi, durasi cooldown, skor) disimpan di dalam `ScriptableObject` dengan `[field: SerializeField] public float MoveSpeed { get; private set; }` sehingga Anggota 3 dapat melakukan balancing di Inspector tanpa risiko dimutasi oleh kode runtime.
3. **Defensive Lifecycle & Null Safety:** Gunakan `[RequireComponent(typeof(...))]` untuk dependensi komponen pada GameObject yang sama, validasi referensi `[SerializeField]` di dalam `OnValidate()` / `Awake()` dengan pesan `Debug.LogError(..., this)` yang menunjuk langsung ke GameObject terkait, dan pastikan pembatalan operasi async saat objek dihancurkan.
