using UnityEngine;
using Gimersia.Gameplay;

namespace Gimersia.UI
{
    public sealed class PrototypeGameplayHUD : MonoBehaviour
    {
        [SerializeField] private DualClimberOrchestrator _orchestrator;

        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _badgeTitleStyle;
        private GUIStyle _badgeSubStyle;
        private GUIStyle _subStyle;
        private GUIStyle _buttonStyle;
        private Texture2D _whiteTex;

        private bool _hasStartedGame;
        private bool _showCreditsModal;
        private RestorableFoliage[] _allFoliage;

        private void Awake()
        {
            _whiteTex = Texture2D.whiteTexture;
            if (_orchestrator == null)
            {
                _orchestrator = GetComponent<DualClimberOrchestrator>();
            }
        }

        private void Start()
        {
            _allFoliage = FindObjectsByType<RestorableFoliage>(FindObjectsSortMode.None);
        }

        private void OnGUI()
        {
            if (_orchestrator == null) return;
            EnsureStyles();

            if (!_hasStartedGame)
            {
                DrawMainMenuOverlay();
                return;
            }

            if (_orchestrator.IsVictoryAchieved)
            {
                DrawVictoryOverlay();
                return;
            }

            if (_orchestrator.IsGamePaused)
            {
                DrawPauseOverlay();
                return;
            }

            DrawActiveGameplayHUD();
        }

        private void DrawMainMenuOverlay()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.04f, 0.08f, 0.14f, 0.86f));

            float panelW = 680f;
            float panelH = 490f;
            float px = (Screen.width - panelW) * 0.5f;
            float py = (Screen.height - panelH) * 0.5f;

            DrawRect(new Rect(px, py, panelW, panelH), new Color(0.09f, 0.14f, 0.21f, 0.96f));
            DrawRect(new Rect(px, py, panelW, 6f), new Color(0.1f, 0.95f, 0.80f, 1f));

            GUI.Label(new Rect(px, py + 20f, panelW, 44f), "PUNCAK HARMONI", _titleStyle);
            GUI.Label(new Rect(px + 20f, py + 66f, panelW - 40f, 26f), "GIMERSIA INSPIRE 2026 — EKSPEDISI PULAU GUNUNG ALAM TERBUKA (PEAK EDITION)", _headerStyle);

            if (!_showCreditsModal)
            {
                GUI.Label(new Rect(px + 35f, py + 100f, panelW - 70f, 75f),
                    "MISI GOTONG ROYONG: Bawa pulang Mustika Pertiwi dari Pesisir Pantai ke Altar Surya Kencana (125m)!\n" +
                    "• Pembawa Mustika tidak bisa memanjat tebing vertikal. Oper Mustika ke rekan yang memanjat duluan!\n" +
                    "• Pilih Mode Permainan di bawah ini untuk memulai ekspedisi:", _subStyle);

                // Two Explicit Mode Selection Buttons
                float btnW = 290f;
                float btnH = 62f;
                float leftBtnX = px + 40f;
                float rightBtnX = px + panelW - btnW - 40f;
                float btnY = py + 188f;

                DrawRect(new Rect(leftBtnX - 3f, btnY - 3f, btnW + 6f, btnH + 6f), new Color(0.05f, 0.78f, 0.96f, 1f));
                if (GUI.Button(new Rect(leftBtnX, btnY, btnW, btnH), "1 PLAYER (SOLO MODE)\nKala & Surya + Companion AI", _buttonStyle))
                {
                    _orchestrator.SetSplitScreenMode(false);
                    _hasStartedGame = true;
                }

                DrawRect(new Rect(rightBtnX - 3f, btnY - 3f, btnW + 6f, btnH + 6f), new Color(0.98f, 0.58f, 0.15f, 1f));
                if (GUI.Button(new Rect(rightBtnX, btnY, btnW, btnH), "2 PLAYERS (SPLIT-SCREEN)\nKiri: Kala (P1) & Kanan: Surya (P2)", _buttonStyle))
                {
                    _orchestrator.SetSplitScreenMode(true);
                    _hasStartedGame = true;
                }

                if (GUI.Button(new Rect(px + 190f, py + 272f, 300f, 40f), "CREDITS & ATRIBUSI LISENSI", _buttonStyle))
                {
                    _showCreditsModal = true;
                }

                GUI.Label(new Rect(px + 30f, py + 328f, panelW - 60f, 140f),
                    "PANDUAN KONTROL:\n" +
                    "• MODE 1 PLAYER (SOLO): [WASD] Gerak | [SPACE] Lompat & Tahan di Tebing untuk Manjat | [TAB] Ganti Karakter\n" +
                    "  [TAHAN KLIK KIRI / F] Lempar Bola | [KLIK KANAN / E] Tangkap Bola | [Q] Oper Cepat ke Rekan AI\n" +
                    "• MODE 2 PLAYERS (SPLIT SCREEN):\n" +
                    "  - P1 (Layar Kiri): [WASD] Gerak | [SPACE] Lompat/Panjat | [F] Lempar | [E] Tangkap | [Q] Oper\n" +
                    "  - P2 (Layar Kanan): [Panah Arah] Gerak | [R-CTRL / NUM 0] Lompat/Panjat | [ENTER / NUM 1] Lempar | [R-SHIFT / NUM 2] Tangkap", _subStyle);
            }
            else
            {
                GUI.Label(new Rect(px + 40f, py + 120f, panelW - 80f, 180f),
                    "TIM PENGEMBANG GIMERSIA INSPIRE 2026:\n" +
                    "• Lead Programmer & Architecture: Anggota 2 & Stella\n" +
                    "• 3D Character & Animation Artist: Anggota 1\n" +
                    "• Level & Sound Designer: Anggota 3\n\n" +
                    "ASET PIHAK KETIGA (100% LEGAL & NON-AI CC0):\n" +
                    "• Kenney Nature Kit & Audio SFX Pack (kenney.nl - CC0 1.0 Public Domain)", _subStyle);

                if (GUI.Button(new Rect(px + 190f, py + 340f, 300f, 44f), "KEMBALI KE MENU PILIH MODE", _buttonStyle))
                {
                    _showCreditsModal = false;
                }
            }
        }

        private void DrawActiveGameplayHUD()
        {
            BeanClimberController climberA = _orchestrator.ClimberA;
            BeanClimberController climberB = _orchestrator.ClimberB;
            BeanClimberController activeSolo = _orchestrator.ActiveClimber;
            bool isSplit = _orchestrator.IsSplitScreenMode;

            // Central Vertical Divider Line for 2-Player Split Screen
            if (isSplit)
            {
                float midX = Screen.width * 0.5f;
                DrawRect(new Rect(midX - 2f, 0f, 4f, Screen.height), new Color(0.05f, 0.08f, 0.12f, 0.95f));
                DrawRect(new Rect(midX - 1f, 0f, 2f, Screen.height), new Color(0.98f, 0.86f, 0.35f, 0.95f));
            }

            // Mountain Altitude & Restoration Progress Banner (Symmetrically Positioned Without Overlaps)
            int restoredCount = CountRestoredFlora();
            int totalFlora = _allFoliage != null ? Mathf.Max(1, _allFoliage.Length) : 1;
            int restorePct = Mathf.RoundToInt((restoredCount / (float)totalFlora) * 100f);

            int mins = Mathf.FloorToInt(_orchestrator.ElapsedRunTime / 60f);
            int secs = Mathf.FloorToInt(_orchestrator.ElapsedRunTime % 60f);

            if (!isSplit)
            {
                // In Solo Mode: Place Banner on Top-Right to balance Top-Left Active Badge without any overlap
                float topBannerW = 350f;
                float topBannerH = 86f;
                float topBx = Screen.width - topBannerW - 18f;
                DrawRect(new Rect(topBx, 18f, topBannerW, topBannerH), new Color(0.06f, 0.11f, 0.17f, 0.90f));
                DrawRect(new Rect(topBx, 18f, topBannerW, 4f), new Color(0.18f, 0.95f, 0.68f, 1f));

                GUI.Label(new Rect(topBx + 10f, 26f, topBannerW - 20f, 20f), $"{_orchestrator.CurrentZoneName.ToUpper()}  |  {mins:00}:{secs:00}", _headerStyle);
                GUI.Label(new Rect(topBx + 10f, 50f, topBannerW - 20f, 40f), $"KETINGGIAN: {_orchestrator.CurrentAltitude:0.0}m / 125m (PEAK)\nRESTORASI ALAM: {restorePct}% ({restoredCount}/{totalFlora} Flora)", _subStyle);
            }
            else
            {
                // In Split-Screen Mode: Compact Center Pill Banner between Left & Right Badges
                float topBannerW = 280f;
                float topBannerH = 46f;
                float topBx = (Screen.width - topBannerW) * 0.5f;
                DrawRect(new Rect(topBx, 12f, topBannerW, topBannerH), new Color(0.06f, 0.11f, 0.17f, 0.92f));
                DrawRect(new Rect(topBx, 12f, topBannerW, 3f), new Color(0.98f, 0.86f, 0.35f, 1f));

                GUI.Label(new Rect(topBx, 15f, topBannerW, 18f), $"{_orchestrator.CurrentZoneName.ToUpper()}  |  {mins:00}:{secs:00}", _headerStyle);
                GUI.Label(new Rect(topBx, 33f, topBannerW, 18f), $"PUNCAK: {_orchestrator.CurrentAltitude:0.0}m / 125m  |  PULIH: {restorePct}%", _subStyle);
            }

            if (isSplit)
            {
                // =========================================================================
                // 2-PLAYER MODE: P1 BADGE (TOP-LEFT) & P2 BADGE (TOP-RIGHT)
                // =========================================================================
                if (climberA != null)
                {
                    DrawCharacterAvatarBadge(
                        new Rect(18f, 18f, 285f, 82f),
                        climberA,
                        isBlueClimber: true,
                        titleText: "P1: KALA (BIRU)",
                        subRoleText: "Kontrol: [WASD] + [SPACE] + [F/E/Q]",
                        rightAligned: false
                    );
                }

                if (climberB != null)
                {
                    DrawCharacterAvatarBadge(
                        new Rect(Screen.width - 303f, 18f, 285f, 82f),
                        climberB,
                        isBlueClimber: false,
                        titleText: "P2: SURYA (ORANYE)",
                        subRoleText: "Kontrol: [PANAH] + [R-CTRL/NUM]",
                        rightAligned: true
                    );
                }

                DrawThrowChargeBar(climberA, Screen.width * 0.25f, "KALA THROW");
                DrawThrowChargeBar(climberB, Screen.width * 0.75f, "SURYA THROW");
                DrawCatchPrompt(climberA, Screen.width * 0.25f, "KALA: TEKAN [E] UNTUK CATCH!");
                DrawCatchPrompt(climberB, Screen.width * 0.75f, "SURYA: TEKAN [R-SHIFT / NUM 2] CATCH!");
                DrawLedgePullUpPrompt(climberA, Screen.width * 0.25f, "ULURAN TANGAN! TEKAN [E] TARIK SURYA!");
                DrawLedgePullUpPrompt(climberB, Screen.width * 0.75f, "ULURAN TANGAN! TEKAN [NUM 2] TARIK KALA!");
                DrawOffScreenPartnerCompass(_orchestrator.CameraA, climberA, climberB, "SURYA", isBluePartner: false, new Rect(0f, 0f, Screen.width * 0.5f, Screen.height));
                DrawOffScreenPartnerCompass(_orchestrator.CameraB, climberB, climberA, "KALA", isBluePartner: true, new Rect(Screen.width * 0.5f, 0f, Screen.width * 0.5f, Screen.height));
            }
            else
            {
                // =========================================================================
                // 1-PLAYER SOLO MODE: SINGLE TOP-LEFT BADGE FOR ACTIVE CLIMBER + AI STATUS
                // =========================================================================
                if (activeSolo != null)
                {
                    bool isBlueActive = (activeSolo == climberA);
                    string activeTitle = isBlueActive ? "AKTIF: KALA (BIRU)" : "AKTIF: SURYA (ORANYE)";
                    string aiPartnerStatus = isBlueActive
                        ? "AI Rekan (Surya): Aktif Mengikuti  |  [TAB] Ganti"
                        : "AI Rekan (Kala): Aktif Mengikuti  |  [TAB] Ganti";

                    DrawCharacterAvatarBadge(
                        new Rect(18f, 18f, 320f, 86f),
                        activeSolo,
                        isBlueClimber: isBlueActive,
                        titleText: activeTitle,
                        subRoleText: aiPartnerStatus,
                        rightAligned: false
                    );

                    BeanClimberController partnerSolo = isBlueActive ? climberB : climberA;
                    string partnerName = isBlueActive ? "SURYA" : "KALA";

                    DrawThrowChargeBar(activeSolo, Screen.width * 0.5f, "LEMPAR MUSTIKA");
                    DrawCatchPrompt(activeSolo, Screen.width * 0.5f, "SLOW-MO! TEKAN [E] / KLIK KANAN UNTUK CATCH!");
                    DrawLedgePullUpPrompt(activeSolo, Screen.width * 0.5f, $"ULURAN TANGAN! TEKAN [E] TARIK {partnerName} KE ATAS!");
                    DrawOffScreenPartnerCompass(_orchestrator.CameraA, activeSolo, partnerSolo, partnerName, isBluePartner: !isBlueActive, new Rect(0f, 0f, Screen.width, Screen.height));
                }
            }
        }

        private void DrawCharacterAvatarBadge(
            Rect rect,
            BeanClimberController climber,
            bool isBlueClimber,
            string titleText,
            string subRoleText,
            bool rightAligned)
        {
            Color themeColor = isBlueClimber ? new Color(0.06f, 0.74f, 0.96f, 1f) : new Color(0.98f, 0.56f, 0.14f, 1f);
            Color bgColor = isBlueClimber ? new Color(0.05f, 0.10f, 0.17f, 0.90f) : new Color(0.16f, 0.10f, 0.06f, 0.90f);

            DrawRect(rect, bgColor);
            if (!rightAligned)
            {
                DrawRect(new Rect(rect.x, rect.y, 5f, rect.height), themeColor);
            }
            else
            {
                DrawRect(new Rect(rect.xMax - 5f, rect.y, 5f, rect.height), themeColor);
            }

            // Draw Stylized Character Portrait Icon (60x62)
            float iconSize = 60f;
            float iconX = rightAligned ? (rect.xMax - iconSize - 12f) : (rect.x + 12f);
            float iconY = rect.y + 11f;

            // Portrait Frame
            DrawRect(new Rect(iconX - 2f, iconY - 2f, iconSize + 4f, iconSize + 4f), new Color(0.95f, 0.95f, 0.98f, 0.9f));
            DrawRect(new Rect(iconX, iconY, iconSize, iconSize), new Color(0.12f, 0.18f, 0.25f, 1f));

            // Character Head/Bean Body Silhouette inside Portrait
            DrawRect(new Rect(iconX + 12f, iconY + 14f, 36f, 42f), themeColor);
            // Hat / Beanie Accent on top of head
            Color hatCol = isBlueClimber ? new Color(0.95f, 0.78f, 0.22f, 1f) : new Color(0.85f, 0.25f, 0.18f, 1f);
            DrawRect(new Rect(iconX + 10f, iconY + 8f, 40f, 10f), hatCol);
            // Expressive Eyes
            DrawRect(new Rect(iconX + 19f, iconY + 26f, 6f, 9f), new Color(0.08f, 0.08f, 0.10f, 1f));
            DrawRect(new Rect(iconX + 35f, iconY + 26f, 6f, 9f), new Color(0.08f, 0.08f, 0.10f, 1f));
            DrawRect(new Rect(iconX + 20f, iconY + 27f, 2f, 3f), Color.white);
            DrawRect(new Rect(iconX + 36f, iconY + 27f, 2f, 3f), Color.white);

            // Text & Stamina Bar Area
            float textX = rightAligned ? (rect.x + 14f) : (iconX + iconSize + 12f);
            float textW = rect.width - iconSize - 36f;

            string handStatus = climber.HasBall
                ? "★ MEMBAWA MUSTIKA (Tidak Bisa Manjat)"
                : "TANGAN KOSONG (Tahan SPACE = Manjat)";

            GUI.Label(new Rect(textX, rect.y + 8f, textW, 20f), titleText, _badgeTitleStyle);
            GUI.Label(new Rect(textX, rect.y + 27f, textW, 18f), handStatus, _badgeSubStyle);
            GUI.Label(new Rect(textX, rect.y + 44f, textW, 16f), subRoleText, _badgeSubStyle);

            // Stamina Bar
            float stamNorm = climber.ClimbStaminaNormalized;
            DrawRect(new Rect(textX, rect.y + 64f, textW - 6f, 9f), new Color(0.18f, 0.20f, 0.25f, 1f));
            Color stamCol = stamNorm > 0.28f ? new Color(0.20f, 0.95f, 0.55f, 1f) : new Color(1.0f, 0.32f, 0.20f, 1f);
            DrawRect(new Rect(textX, rect.y + 64f, (textW - 6f) * stamNorm, 9f), stamCol);
        }

        private void DrawThrowChargeBar(BeanClimberController climber, float centerX, string labelPrefix)
        {
            if (climber == null || !climber.IsChargingThrow) return;

            float barW = 250f;
            float barH = 20f;
            float x = centerX - (barW * 0.5f);
            float y = Screen.height - 105f;

            DrawRect(new Rect(x - 3f, y - 3f, barW + 6f, barH + 6f), new Color(0.05f, 0.05f, 0.08f, 0.92f));
            float sweetMin = climber.Config.SweetSpotMin;
            float sweetMax = climber.Config.SweetSpotMax;
            DrawRect(new Rect(x + (barW * sweetMin), y, barW * (sweetMax - sweetMin), barH), new Color(0.95f, 0.78f, 0.1f, 0.65f));

            bool inSweet = climber.ChargeNormalized >= sweetMin && climber.ChargeNormalized <= sweetMax;
            Color fill = inSweet ? new Color(0f, 0.95f, 1f, 1f) : new Color(0.2f, 0.85f, 0.45f, 1f);
            DrawRect(new Rect(x, y, barW * climber.ChargeNormalized, barH), fill);
            GUI.Label(new Rect(x - 20f, y - 24f, barW + 40f, 20f), inSweet ? $"{labelPrefix}: SWEET SPOT! LEPAS!" : $"{labelPrefix}: TAHAN LEMPAR...", _headerStyle);
        }

        private void DrawCatchPrompt(BeanClimberController climber, float centerX, string promptText)
        {
            if (climber == null || !climber.IsInCatchWindow) return;

            float boxW = 340f;
            float boxH = 44f;
            float cx = centerX - (boxW * 0.5f);
            float cy = Screen.height * 0.25f;

            DrawRect(new Rect(cx, cy, boxW, boxH), new Color(0f, 0.75f, 0.95f, 0.94f));
            GUI.Label(new Rect(cx + 10f, cy + 11f, boxW - 20f, 24f), promptText, _headerStyle);
        }

        private void DrawLedgePullUpPrompt(BeanClimberController climber, float centerX, string promptText)
        {
            if (climber == null || !climber.CanAssistPullUpPartner) return;

            float boxW = 380f;
            float boxH = 42f;
            float cx = centerX - (boxW * 0.5f);
            float cy = (Screen.height * 0.25f) + 52f;

            DrawRect(new Rect(cx, cy, boxW, boxH), new Color(0.15f, 0.82f, 0.48f, 0.94f));
            GUI.Label(new Rect(cx + 10f, cy + 10f, boxW - 20f, 22f), promptText, _headerStyle);
        }

        private void DrawOffScreenPartnerCompass(
            Camera viewCam,
            BeanClimberController viewer,
            BeanClimberController partner,
            string partnerLabel,
            bool isBluePartner,
            Rect viewportBounds)
        {
            if (viewCam == null || viewer == null || partner == null) return;

            float dist = Vector3.Distance(viewer.transform.position, partner.transform.position);
            Vector3 worldTarget = partner.transform.position + (Vector3.up * 2.2f);
            Vector3 screenPt = viewCam.WorldToScreenPoint(worldTarget);

            float guiX = screenPt.x;
            float guiY = Screen.height - screenPt.y;

            bool isBehind = screenPt.z < 0f;
            if (isBehind)
            {
                guiX = viewportBounds.xMax - (guiX - viewportBounds.xMin);
                guiY = Screen.height - 48f;
            }

            bool isOutsideViewport = isBehind ||
                                     guiX < viewportBounds.xMin + 35f ||
                                     guiX > viewportBounds.xMax - 35f ||
                                     guiY < 110f ||
                                     guiY > Screen.height - 45f;

            if (!isOutsideViewport && dist < 9.5f)
            {
                return;
            }

            float clampedX = Mathf.Clamp(guiX, viewportBounds.xMin + 70f, viewportBounds.xMax - 70f);
            float clampedY = Mathf.Clamp(guiY, 118f, Screen.height - 52f);

            float badgeW = 132f;
            float badgeH = 26f;
            Rect compassRect = new Rect(clampedX - (badgeW * 0.5f), clampedY - (badgeH * 0.5f), badgeW, badgeH);

            Color markerColor = isBluePartner ? new Color(0.06f, 0.78f, 0.98f, 0.94f) : new Color(0.98f, 0.55f, 0.12f, 0.94f);
            DrawRect(new Rect(compassRect.x - 2f, compassRect.y - 2f, compassRect.width + 4f, compassRect.height + 4f), markerColor);
            DrawRect(compassRect, new Color(0.06f, 0.10f, 0.16f, 0.92f));
            GUI.Label(compassRect, $"◆ {partnerLabel}: {dist:0}m", _subStyle);
        }

        private void DrawPauseOverlay()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.08f, 0.84f));

            float pw = 440f;
            float ph = 350f;
            float px = (Screen.width - pw) * 0.5f;
            float py = (Screen.height - ph) * 0.5f;

            DrawRect(new Rect(px, py, pw, ph), new Color(0.10f, 0.14f, 0.20f, 0.96f));
            GUI.Label(new Rect(px, py + 22f, pw, 36f), "PERMAINAN DIJEDA", _titleStyle);

            if (GUI.Button(new Rect(px + 70f, py + 78f, 300f, 42f), "LANJUTKAN (RESUME)", _buttonStyle))
            {
                _orchestrator.TogglePause();
            }

            if (GUI.Button(new Rect(px + 70f, py + 130f, 300f, 42f), "MODE 1 PLAYER (SOLO + AI)", _buttonStyle))
            {
                _orchestrator.SetSplitScreenMode(false);
                _orchestrator.TogglePause();
            }

            if (GUI.Button(new Rect(px + 70f, py + 182f, 300f, 42f), "MODE 2 PLAYERS (SPLIT SCREEN)", _buttonStyle))
            {
                _orchestrator.SetSplitScreenMode(true);
                _orchestrator.TogglePause();
            }

            if (GUI.Button(new Rect(px + 70f, py + 234f, 300f, 42f), "KEMBALI KE CHECKPOINT", _buttonStyle))
            {
                _orchestrator.RespawnAtLastCheckpoint();
                _orchestrator.TogglePause();
            }

            if (GUI.Button(new Rect(px + 70f, py + 286f, 300f, 42f), "PANGGIL SAHABAT KE SINI [G]", _buttonStyle))
            {
                _orchestrator.SummonPartnerToActiveCheckpoint();
                _orchestrator.TogglePause();
            }
        }

        private void DrawVictoryOverlay()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.04f, 0.12f, 0.09f, 0.84f));

            float pw = 620f;
            float ph = 400f;
            float px = (Screen.width - pw) * 0.5f;
            float py = (Screen.height - ph) * 0.5f;

            DrawRect(new Rect(px, py, pw, ph), new Color(0.07f, 0.15f, 0.13f, 0.96f));
            DrawRect(new Rect(px, py, pw, 6f), new Color(0.2f, 0.95f, 0.55f, 1f));

            int mins = Mathf.FloorToInt(_orchestrator.ElapsedRunTime / 60f);
            int secs = Mathf.FloorToInt(_orchestrator.ElapsedRunTime % 60f);
            int totalCoopActions = (_orchestrator.ClimberA != null ? _orchestrator.ClimberA.TotalTeamworkActions : 0) +
                                   (_orchestrator.ClimberB != null ? _orchestrator.ClimberB.TotalTeamworkActions : 0);

            GUI.Label(new Rect(px, py + 24f, pw, 44f), "ALTAR SURYA KENCANA TERCAPAI!", _titleStyle);
            GUI.Label(new Rect(px + 30f, py + 76f, pw - 60f, 195f),
                "Mustika Pertiwi telah kembali bersinar di Altar Surya Kencana (125 Meter)!\n" +
                "Gelombang restorasi zamrud telah menyembuhkan seluruh alam pulau.\n\n" +
                "--- PAPAN EVALUASI GOTONG ROYONG (TEAMWORK SCORE) ---\n" +
                $"• Waktu Tempuh Ekspedisi : {mins:00} Menit {secs:00} Detik\n" +
                $"• Aksi Operan & Uluran Tangan : {Mathf.Max(1, totalCoopActions)} Kali Sukses\n" +
                "• Pemulihan Ekosistem Pulau : 100% (Eco-Restoration Complete)\n" +
                "• GELAR KEHORMATAN : RANK S — \"HARMONI SEMPURNA NUSANTARA\"", _headerStyle);

            if (GUI.Button(new Rect(px + 160f, py + 310f, 300f, 48f), "LANJUTKAN EKSPLORASI PULAU", _buttonStyle))
            {
                typeof(DualClimberOrchestrator)
                    .GetField("_isVictoryAchieved", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(_orchestrator, false);
            }
        }

        private int CountRestoredFlora()
        {
            if (_allFoliage == null) return 0;
            int count = 0;
            for (int i = 0; i < _allFoliage.Length; i++)
            {
                if (_allFoliage[i] != null && _allFoliage[i].IsRestored) count++;
            }
            return count;
        }

        private void EnsureStyles()
        {
            if (_headerStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.1f, 0.95f, 0.85f, 1f) }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.88f, 0.35f, 1f) }
            };

            _badgeTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.98f, 0.92f, 0.45f, 1f) }
            };

            _badgeSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.90f, 0.95f, 0.98f, 1f) }
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.90f, 0.94f, 0.98f, 1f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = prev;
        }
    }
}
