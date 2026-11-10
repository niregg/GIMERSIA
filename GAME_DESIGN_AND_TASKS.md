# GIMERSIA INSPIRE 2026 — DOKUMEN MASTER DESAIN GAME & TUGAS TIM (REVISI TERPADU)

Dokumen ini adalah **Satu-Satunya Dokumen Induk (Single Source of Truth)** untuk proyek kompetisi nasional **Gimersia Inspire 2026** berjudul **"PUNCAK HARMONI"** (`D:\Gimersia`). Seluruh spesifikasi game, arsitektur sistem, pembagian tugas 3 anggota tim, konsep visual mockup, serta catatan revisi terbaru disatukan di sini.

---

## 1. Identitas Game & Harmonisasi Tema Kompetisi

- **Judul Game:** PUNCAK HARMONI
- **Genre:** 3D Co-op Mountain Climbing Platformer (Inspirasi: *It Takes Two*, *PEAK*, *Jusant*, *A Short Hike*)
- **Target Platform:** PC Windows Standalone & WebGL 60 FPS (itch.io)
- **Engine & Render Pipeline:** Unity 2022.3 LTS (Universal Render Pipeline - URP)

### Integrasi 3 Tema Wajib Kompetisi:
1. **"Pulang":** Membawa pulang kembali *Terra Core* (Bola Kristal Inti Bumi) yang terjatuh dari langit kembali ke rumah asalnya, yaitu Altar Suci Puncak Mahkota (125 Meter).
2. **"Gotong Royong":** Dua sahabat pendaki saling melengkapi keterbatasan fisik. Pemain yang membawa bola tidak bisa memanjat tebing (*free-hand wall climbing*) dan melompat lebih rendah. Satu sahabat harus memanjat tebing lebih dulu untuk bersiap menangkap lemparan bola dari bawah atau mengaktifkan mekanisme jembatan.
3. **"Melestarikan":** Radiasi aura suci bola kristal (*Sanctuary Aura*) secara dinamis menghidupkan dan melestarikan kembali 50+ pohon pinus dan flora gunung yang hangus menjadi hijau subur saat didekati atau dilewati lemparan bola.

---

## 2. Arsitektur Mode Permainan & Kontrol (Gaya *It Takes Two*)

### 2.1 Start Screen & Pemilihan Mode Eksplisit
Pada layar menu awal (*MainMenu*), terdapat dua tombol pilihan mode yang langsung menentukan alur rendering dan kontrol:
1. **Tombol "1 PLAYER (SOLO + COMPANION AI)":**
   - Mengaktifkan **Satu Layar Penuh (Fullscreen)** dengan 1 Kamera utama.
   - Pemain mengendalikan Sahabat A secara manual.
   - Sahabat B otomatis mengaktifkan modul **CompanionFollowerAI** untuk membuntuti pemain aktif.
   - Pemain dapat menekan **`[TAB]`** kapan saja untuk langsung bertukar kendali tubuh secara instan.
2. **Tombol "2 PLAYERS (CO-OP SPLIT SCREEN)":**
   - Mengaktifkan **Dual Camera Split-Screen Vertikal (Kiri - Kanan)** dengan garis pembatas emas/cyan di tengah layar (`x = 0.5`).
   - Layar Kiri: Fokus eksklusif pada Sahabat A (Player 1).
   - Layar Kanan: Fokus eksklusif pada Sahabat B (Player 2).
   - AI Follower dimatikan karena kedua tubuh dikendalikan langsung oleh manusia.

### 2.2 Skema Kontrol Dua Pemain (Keyboard Bersama & Gamepad)

| Aksi Gameplay | Player 1 (Sahabat A - Biru) | Player 2 (Sahabat B - Oranye) [Keyboard] | Player 2 [Gamepad Controller] |
| :--- | :--- | :--- | :--- |
| **Pergerakan (Move)** | `[W] [A] [S] [D]` | Tombol Panah `[↑] [←] [↓] [→]` | Left Analog Stick |
| **Lompat / Panjat Tebing** | `[SPACE]` (Tahan di tebing) | `[R-CTRL]` atau `[NUM 0]` | Tombol `[A]` (Xbox) / `[✕]` (PS) |
| **Lempar Bola (Power Throw)**| Tahan `[F]` / `[Klik Kiri]` | Tahan `[NUM 1]` atau `[ENTER]` | Tahan `[Right Trigger]` |
| **Tangkap Bola (Catch)** | `[E]` / `[Klik Kanan]` | `[NUM 2]` atau `[R-SHIFT]` | `[Right Bumper]` |
| **Oper Cepat (Quick Pass)** | `[Q]` | `[NUM 3]` atau `[\]` | Tombol `[X]` (Xbox) / `[▢]` (PS) |
| **Putar Kamera (Orbit)** | `[Z]` / `[C]` | `[NUM 4]` / `[NUM 6]` | Right Analog Stick |

### 2.3 UI Badge Avatar Karakter
- **Player 1 (Kiri Atas):** Badge avatar Sahabat A (Biru), nama "P1: SAHABAT A", status tangan *(Bawa Bola / Tangan Kosong)*, dan Bar Stamina Panjat.
- **Player 2 (Kanan Atas):** Badge avatar Sahabat B (Oranye), nama "P2: SAHABAT B", status tangan, dan Bar Stamina Panjat.
- Pada mode Solo, HUD otomatis menyusut menjadi satu badge di kiri atas yang berganti warna/nama secara dinamis mengikuti karakter yang sedang dikendalikan.

---

## 3. Desain Lingkungan Alam Terbuka ala *PEAK* (Bebas Kubus Indoor)

### 3.1 Konsep Alam Pulau Pegunungan Terbuka (*Open Island Wilderness*)
- **Bukan Kotak/Tangga Indoor:** Seluruh dinding balok geometris vertikal dan lantai kubus abu-abu digantikan dengan kontur pulau alam terbuka yang dikelilingi laut lepas dan langit pegunungan biru cerah (*alpine sky*).
- **Transparent / Invisible Barrier Blocks:** Pembatas map tidak lagi berupa tembok tebing kotak yang sempit, melainkan dinding pembatas tembus pandang (*Invisible Barrier Colliders*) yang diletakkan cukup jauh di batas luar pulau sehingga pemain tidak bisa jatuh ke laut lepas, namun tetap menikmati pemandangan alam terbuka 360 derajat.
- **Goal Horizon (Puncak 125m):** Dari titik awal di pantai/lembah kaki gunung (Basecamp 0m), pemain dapat langsung mendongak ke atas melihat mahkota Puncak Gunung tertinggi dengan Altar Kristal bercahaya zamrud/emas sebagai kompas arah alami perjalanan.

### 3.2 Alur 6 Zona Pendakian & Rintangan Gotong Royong
1. **Zona 1: Lembah Kaki Gunung (0m – 18m):**
   - Basecamp pantai terbuka, tenda, api unggun, dan padang rumput lereng.
   - *Rintangan 1 (Tebing Estafet Pertama):* Dinding tebing batu modular setinggi 6.5m. Pemain bertangan kosong memanjat tebing (`[SPACE]`) dengan sistem *ledge mantle*, lalu menangkap lemparan bola dari bawah atau menginjak saklar jembatan untuk rekannya.
2. **Zona 2: Lereng Hutan & Gerbang Soket Kristal (18m – 40m):**
   - Lereng terjal dengan formasi pohon pinus hangus yang siap dilestarikan.
   - *Rintangan 2 (Gerbang Soket Kristal):* Dinding gerbang batu kuno yang menghalangi jalan. Pemain harus melempar bola kristal masuk ke cincin target soket di seberang tebing untuk menurunkan gerbang batu.
3. **Zona 3: Ngarai Kembar & Barikade Batu (40m – 62m):**
   - Lereng terbelah oleh jurang ngarai dengan jembatan kayu dan jamur pegas (*bounce pads*) di dasar jurang.
   - *Rintangan 3 (Barikade Batu Sweet-Spot):* Dinding batu rapuh yang hanya bisa hancur jika ditembak dengan lemparan penuh *Sweet-Spot Power Throw* (80%–95%).
4. **Zona 4: Tebing Angin Terjal & Pijakan Bergerak (62m – 88m):**
   - Tebing bertingkat tinggi dengan dua platform batu melayang yang bergerak bolak-balik untuk aksi lempar-tangkap di udara (*aerial pass*).
   - Gerbang Soket Kristal kedua di ketinggian 88m.
5. **Zona 5: Lereng Guguran Batu Vulkanik (88m – 114m):**
   - Lereng curam tempat batu vulkanik besar menggelinding turun. Pemain harus berlindung di balik bunker batu alam, mengoper bola secara estafet antar celah perlindungan, atau memecahkan batu menggelinding dengan lemparan bertenaga.
6. **Zona 6: Puncak Mahkota Harmoni — Altar 125 Meter (114m – 125m):**
   - Punggungan puncak tertinggi yang megah. Menempatkan bola kristal di Altar Cincin Kuno memicu pemulihan 100% seluruh flora gunung, dentang lonceng kemenangan, dan kembang api partikel suci.

---

## 4. Perbaikan Mekanik: Ledge Mantle Anti-Stuck & Companion Follower AI

### 4.1 Ledge Mantle Anti-Stuck
- Menggunakan deteksi raycast ganda (dinding tebing dan bibir atas lantai datar).
- Ketika kepala karakter melewati bibir tebing, sistem otomatis menjalankan perpindahan posisi deterministik (*vault mantle*) mengangkat tubuh karakter langsung ke atas permukaan lantai, mengeliminasi bug karakter terjepit/bergetar di ujung kolisi.

### 4.2 Companion Follower AI (Solo Mode)
- Mengukur jarak horizontal ke karakter aktif. Jika jarak $> 2.5\text{m}$, AI secara otomatis berlari mengikuti arah pemain.
- Mendeteksi rintangan pendek di depannya dan otomatis melompat.
- Berhenti dengan tenang saat berada di dekat pemain (jarak $< 1.8\text{m}$).
- Nonaktif seketika dan berpindah menjadi kontrol manual penuh 100% begitu user menekan tombol `[TAB]`.

---

## 5. Pembagian Tugas 3 Anggota Tim

```
TIM PENGEMBANG (3 ANGGOTA):
├── Anggota 1: 3D Character, Prop Modeler & Animator (Blender)
├── Anggota 2: Systems Engineer, Gameplay Programmer & Technical Artist (Unity C#)
└── Anggota 3: Level Designer, Sound Designer & QA Tester (Audio/Balance)
```

### 5.1 Rincian Tugas Anggota 1 (Hanya 3 Aset di Blender)
Anggota 1 **hanya fokus pada 3 aset utama** berikut agar selesai cepat dan berkualitas tinggi:

| Nama File Ekspor | Format & Letak | Deskripsi Desain Karakter | Animasi Wajib (FBX Clips) |
| :--- | :--- | :--- | :--- |
| `SK_Char_SahabatA.fbx` | `Assets/_Project/Models/Characters/` | Karakter pendaki bean/kapsul lucu warna Biru, mengenakan jaket/syal pendaki dan ransel hiking di punggung. | `Idle`, `Run`, `Climb`, `Throw`, `Catch` |
| `SK_Char_SahabatB.fbx` | `Assets/_Project/Models/Characters/` | Karakter pendaki bean/kapsul lucu warna Oranye, mengenakan topi kupluk beanie dan ransel hiking di punggung. | `Idle`, `Run`, `Climb`, `Throw`, `Catch` |
| `SM_Prop_TerraCore.fbx` | `Assets/_Project/Models/Props/` | Bola Kristal Inti Bumi berukir ornamen relief kuno dengan bagian inti kristal tembus pandang bercahaya. | Static Mesh (Tanpa Rigging) |

*Spesifikasi Ekspor Blender:* Skala Metric `1.0`, `1 Unit = 1 Meter di Unity`, Sumbu `-Z Forward`, `Y-Up`, `Apply All Transforms` (Ctrl+A).

### 5.2 Rincian Tugas Anggota 2 (Stella & User)
- Arsitektur C# Gameplay, Dual Camera Split-Screen, Input System, dan Companion Follower AI.
- Otomasi scene builder alam terbuka pulau (*PEAK open island*) dan implementasi transparent boundary blocks.
- Integrasi efek "Juice" (Time dilation hit-stop, trauma camera shake, squash-stretch procedurals).

### 5.3 Rincian Tugas Anggota 3 (Level Design, Audio & QA)
- Balancing kurva tingkat kesulitan lemparan, jarak antar checkpoint, dan timing guguran batu vulkanik.
- Menata variasi efek suara audio (SFX langkah kaki rumput/batu, desau angin, petikan lompat, denting kristal, benturan batu).
- Menjalankan uji coba playtest dan verifikasi performa 60 FPS di WebGL dan Windows Standalone.

---

## 6. Referensi Mockup Visual Konsep
Visual target implementasi split-screen dan alam terbuka pegunungan tersimpan pada file gambar:
`C:\Users\LENOVO LOQ\.gemini\antigravity\brain\ee507b0b-cf8a-4b8f-9e44-f37a1539e774\puncak_harmoni_splitscreen_concept_1794315382589.jpg`.
Menampilkan layar terbagi dua (Player 1 di kiri memanjat tebing, Player 2 di kanan melempar bola kristal) dengan HUD badge avatar di pojok atas dan pemandangan pulau pegunungan terbuka menuju Altar Puncak 125m.
