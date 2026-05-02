using System;
using Microsoft.Xna.Framework;
using Snake.Core.Audio;
using Snake.Core.Configuration;
using Snake.Core.Input;
using Snake.Core.Persistence;
using Snake.Core.Rendering;

namespace Snake.Core.States
{
    /// <summary>
    /// Handles the Slots minigame state. Reachable from Menu, Paused, or GameOver;
    /// Back returns to origin.
    /// </summary>
    public class SlotsState : IGameStateHandler, IOriginAware
    {
        // ============================================================
        // LAYOUT TWEAK ZONE — adjust these to position the slot machine.
        // Virtual canvas is 256 wide x 180 tall. Origin (0,0) is top-left.
        // ============================================================

        // Outer cabinet rect. Source is a 24x24 9-slice (3x3 grid of 8x8 tiles):
        // corners drawn 1x, edges + middle tiled to fill. Width and Height should be
        // multiples of CabinetTileSize for crisp results, and at least 2*tile each.
        // Currently filling the entire 256x180 canvas.
        private const int CabinetTileSize = 8;
        private const int CabinetX = 0;
        private const int CabinetY = 0;
        private const int CabinetWidth = 256;
        private const int CabinetHeight = 180;

        // Reel layout: three identical columns (one per reel) sitting inside the window art.
        // ReelAreaX/Y is the top-left of the LEFTMOST reel; the others are placed at
        // ReelAreaX + i * (ReelWidth + ReelGap). Tuned by overlaying magenta test rects;
        // once the rects cover the three cream-colored slots exactly, flip
        // ShowReelAreaTest = false.
        private const int ReelCount = 3;
        private const int ReelAreaX = 59;
        private const int ReelAreaY = 50;
        private const int ReelWidth = 40;        // one reel's width
        private const int ReelHeight = 80;       // shared by all reels
        private const int ReelGap = 9;           // gap between reels
        private const bool ShowReelAreaTest = false;

        // Symbol sheet: slots_symbols.png is 48x32 = 3 cols x 2 rows of 16x16 sprites.
        private const int SymbolSize = 16;
        private const int SymbolSheetCols = 3;
        private const int SymbolSheetRows = 2;
        private const int SymbolsPerColumn = 3;  // visible symbols per reel during spin

        // Slots window: 24x24 source drawn as a single image, stretched to WindowWidth x
        // WindowHeight. Houses the 3x3 grid of slot symbols. Centered on the canvas; if
        // you change Width or Height, also recompute X = (256 - Width) / 2 and
        // Y = (180 - Height) / 2.
        private const int WindowWidth = 168;
        private const int WindowHeight = 96;
        private const int WindowX = 44;   // (256 - 168) / 2
        private const int WindowY = 42;   // (180 - 96) / 2

        // Cabinet trim: 16x64 source split vertically into:
        //   y  0..24 = top cap art (24 tall)
        //   y 24..40 = middle band (16 tall, tileable)
        //   y 40..64 = bottom cap art (24 tall)
        // Top + bottom caps are drawn once each; the middle tile repeats vertically to fill
        // the space between them, so TrimHeight can grow without distorting the cap art.
        // Same sprite used on both sides (no horizontal flipping).
        private const int TrimWidth = 24;
        private const int TrimHeight = 160;
        private const int TrimXInset = 4;
        private const int TrimYInset = 10;
        private const int TrimCapDestHeight = 48;        // output px per cap (2x source 24)
        private const int TrimMiddleTileDestHeight = 32; // output px per tiled middle band (2x source 16)

        // Top row labels (Apples, Win) — two columns above the window, pulled inward
        // toward center for a tighter centered grouping.
        private const int TopRowY = 18;
        private const int TopCol1X = 84;
        private const int TopCol2X = 172;

        // Bottom row buttons (Spin, Bet+, Bet-, Back) — four columns below the window. Gap = 48.
        private const int BottomRowY = 154;
        private const int BotCol1X = 56;
        private const int BotCol2X = 104;
        private const int BotCol3X = 152;
        private const int BotCol4X = 200;

        // Current bet value displayed centered above the bet buttons (canvas X center = 128).
        private const int BetDisplayY = 140;
        private const int BetDisplayX = 128;

        // Pixel gap between the mirrored arrow and "Bet" on the left bet button.
        // Increase if the flipped '~' glyph visually bleeds into the "B".
        private const int BetArrowGap = 4;

        // Help button "?" — top-right corner, navigable from the bottom row via Up/W.
        private const int HelpButtonX = 216;
        private const int HelpButtonY = 18;

        // ============================================================
        // END LAYOUT TWEAK ZONE
        // ============================================================

        private const int ButtonCount = 5;
        private const int SpinIndex = 0;
        private const int BetDownIndex = 1;  // Left bet button — "<- Bet"
        private const int BetUpIndex = 2;    // Right bet button — "Bet ->"
        private const int BackIndex = 3;
        private const int HelpIndex = 4;     // Top "?" button — opens paytable overlay

        private readonly GameConfig m_config;
        private readonly VisualConfig m_visuals;
        private readonly PlayerData m_playerData;
        private readonly SaveManager m_saveManager;
        private readonly AudioManager m_audio;

        // Spin tuning.
        private const float SpinSpeedPxPerSec = 400f;
        private const float MinSpinDurationSec = 1.5f;     // first reel stops at this time
        private const float ReelStopStaggerSec = 0.5f;     // additional delay per subsequent reel
        private const float ReelDecelDurationSec = 0.4f;   // ease-out duration when stopping

        // Hold-to-repeat tuning for bet+/- buttons.
        private const float BetRepeatInitialDelay = 0.4f;  // hold this long before auto-repeat kicks in
        private const float BetRepeatInterval = 0.08f;     // repeat rate after initial delay

        // Hidden pity-timer luck meter (Pokemon-Emerald-slots style). Starts at 0 each Slots
        // session; rises with every losing spin, resets on a win. The current value is the
        // probability that reels 1 and 2 are forced to match reel 0's middle symbol.
        private const float LuckLossIncrement = 0.05f;

        // Derived layout constants (compile-time).
        private const int VerticalPadding = ReelHeight - SymbolsPerColumn * SymbolSize;
        private const int SymbolGap = VerticalPadding / (SymbolsPerColumn + 1);
        private const int SlotHeight = SymbolSize + SymbolGap;
        private const int RestTopY = ReelAreaY + SymbolGap;

        // Per-reel fixed wheels (the actual symbols on each physical reel). Each entry is
        // an index into the symbol sheet (0..5). Wheel length = cycle period during spin.
        private static readonly int[][] WheelData = new[]
        {
            new[] { 0, 1, 2, 3, 4, 5, 0, 2, 4, 1, 3, 5 },
            new[] { 1, 2, 3, 4, 5, 0, 2, 4, 0, 3, 5, 1 },
            new[] { 2, 3, 4, 5, 0, 1, 4, 0, 2, 5, 1, 3 },
        };

        // Payout multipliers per symbol index (cherry, bell, coin, bar, seven, diamond).
        // 3-of-a-kind on the middle row pays multiplier * bet apples.
        private static readonly int[] SymbolMultipliers = { 2, 3, 5, 8, 12, 25 };

        private enum ReelState { Idle, Spinning, Stopping }

        private GameState m_origin = GameState.Menu;
        private int m_selectedButtonIndex;
        private int m_bet = 1;
        private int m_win;

        // Per-reel animation state.
        private readonly ReelState[] m_reelStates = new ReelState[ReelCount];
        private readonly float[] m_reelOffsets = new float[ReelCount];
        private readonly float[] m_reelStopTimes = new float[ReelCount];
        private readonly float[] m_reelStopStartOffsets = new float[ReelCount];
        private readonly float[] m_reelStopTargetOffsets = new float[ReelCount];
        private readonly float[] m_reelStopElapsed = new float[ReelCount];
        private float m_spinElapsed;
        private readonly Random m_random = new Random();

        // Hold-to-repeat state for bet buttons.
        private float m_actionHeldTime;
        private float m_betRepeatTimer;

        // Tracks active state across frames to detect "all reels just stopped" — that's the
        // moment we evaluate the payout.
        private bool m_anyReelActiveLastFrame;

        // Pity-timer state.
        private float m_luckMeter;
        private int m_anchorSymbol;  // middle symbol of reel 0; reels 1+ may be forced to match it

        // Paytable overlay state.
        private bool m_showPaytable;
        private int m_lastBottomButtonIndex = SpinIndex;  // remembered when jumping to "?" via Up

        public GameState StateType => GameState.Slots;

        public SlotsState(GameConfig config, VisualConfig visuals, PlayerData playerData, SaveManager saveManager, AudioManager audio)
        {
            m_config = config;
            m_visuals = visuals;
            m_playerData = playerData;
            m_saveManager = saveManager;
            m_audio = audio;
        }

        public void SetOrigin(GameState origin)
        {
            m_origin = origin;
        }

        public void Enter()
        {
            m_selectedButtonIndex = SpinIndex;
            m_lastBottomButtonIndex = SpinIndex;
            m_luckMeter = 0f;

            // Auto-show the paytable on a fresh save so new players learn the payouts.
            if (!m_playerData.HasSeenPaytable)
            {
                m_showPaytable = true;
                m_playerData.HasSeenPaytable = true;
                m_saveManager.Save(m_playerData);
            }

            m_audio.OnSlotsEnter();
        }

        public void Exit()
        {
        }

        public GameState? Update(GameTime gameTime, InputState input)
        {
            // Modal paytable overlay: action or pause closes it; everything else is ignored.
            if (m_showPaytable)
            {
                if (input.ActionPressed || input.PausePressed)
                {
                    m_showPaytable = false;
                }
                return null;
            }

            if (input.PausePressed)
            {
                return m_origin;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            bool reelsActive = AnyReelActive();

            // Input is locked while reels are spinning/stopping (except Pause, handled above).
            if (!reelsActive)
            {
                // Bottom row (Spin/Bet-/Bet+/Back, indices 0..3) cycles with Left/Right.
                // The "?" (HelpIndex=4) sits at the top, reachable via Up/W and dismissed via Down/S.
                if (input.DirectionPressed == Direction.Left && m_selectedButtonIndex != HelpIndex)
                {
                    m_selectedButtonIndex = (m_selectedButtonIndex - 1 + 4) % 4;
                }
                else if (input.DirectionPressed == Direction.Right && m_selectedButtonIndex != HelpIndex)
                {
                    m_selectedButtonIndex = (m_selectedButtonIndex + 1) % 4;
                }
                else if (input.DirectionPressed == Direction.Up && m_selectedButtonIndex != HelpIndex)
                {
                    m_lastBottomButtonIndex = m_selectedButtonIndex;
                    m_selectedButtonIndex = HelpIndex;
                }
                else if (input.DirectionPressed == Direction.Down && m_selectedButtonIndex == HelpIndex)
                {
                    m_selectedButtonIndex = m_lastBottomButtonIndex;
                }

                if (input.ActionPressed)
                {
                    switch (m_selectedButtonIndex)
                    {
                        case SpinIndex:
                            StartSpin();
                            break;
                        case BetUpIndex:
                        case BetDownIndex:
                            ApplyBetChange();
                            break;
                        case BackIndex:
                            return m_origin;
                        case HelpIndex:
                            m_showPaytable = true;
                            break;
                    }
                }

                // Hold-to-repeat for bet buttons: after a 0.4s hold, auto-fire every 0.08s.
                bool onBetButton = m_selectedButtonIndex == BetUpIndex || m_selectedButtonIndex == BetDownIndex;
                if (input.ActionHeld && onBetButton)
                {
                    m_actionHeldTime += dt;
                    if (m_actionHeldTime >= BetRepeatInitialDelay)
                    {
                        m_betRepeatTimer += dt;
                        while (m_betRepeatTimer >= BetRepeatInterval)
                        {
                            m_betRepeatTimer -= BetRepeatInterval;
                            ApplyBetChange();
                        }
                    }
                }
                else
                {
                    m_actionHeldTime = 0f;
                    m_betRepeatTimer = 0f;
                }
            }
            else
            {
                // Reels are spinning — clear hold state so a fresh hold is required afterward.
                m_actionHeldTime = 0f;
                m_betRepeatTimer = 0f;
            }

            if (reelsActive)
            {
                m_spinElapsed += dt;
            }

            for (int i = 0; i < ReelCount; i++)
            {
                UpdateReel(i, dt);
            }

            // Detect the frame where the last reel transitioned from active → idle so we
            // evaluate the payout exactly once per spin.
            bool anyActiveNow = AnyReelActive();
            if (m_anyReelActiveLastFrame && !anyActiveNow)
            {
                OnAllReelsStopped();
            }
            m_anyReelActiveLastFrame = anyActiveNow;

            return null;
        }

        private void ApplyBetChange()
        {
            if (m_selectedButtonIndex == BetUpIndex && m_bet < m_playerData.AppleBalance) m_bet++;
            else if (m_selectedButtonIndex == BetDownIndex && m_bet > 1) m_bet--;
        }

        private bool AnyReelActive()
        {
            for (int i = 0; i < ReelCount; i++)
            {
                if (m_reelStates[i] != ReelState.Idle) return true;
            }
            return false;
        }

        private void StartSpin()
        {
            // Don't allow a spin we can't afford.
            if (m_bet <= 0 || m_bet > m_playerData.AppleBalance) return;

            m_playerData.AppleBalance -= m_bet;
            m_win = 0;
            m_spinElapsed = 0f;

            for (int i = 0; i < ReelCount; i++)
            {
                m_reelStates[i] = ReelState.Spinning;
                m_reelStopTimes[i] = MinSpinDurationSec + i * ReelStopStaggerSec;
            }

            m_audio.PlayReelSpin();
        }

        private void OnAllReelsStopped()
        {
            m_audio.StopReelSpin();

            // Single payline: middle row across all 3 reels. 3-of-a-kind pays multiplier * bet.
            int s0 = GetMiddleSymbol(0);
            int s1 = GetMiddleSymbol(1);
            int s2 = GetMiddleSymbol(2);
            if (s0 == s1 && s1 == s2)
            {
                m_win = SymbolMultipliers[s0] * m_bet;
                m_playerData.AppleBalance += m_win;
                m_audio.PlayJackpot();
                m_luckMeter = 0f;
            }
            else
            {
                m_luckMeter = MathHelper.Min(1f, m_luckMeter + LuckLossIncrement);
            }
            // Bet was already deducted in StartSpin; persist either way.
            m_saveManager.Save(m_playerData);

            // Cap bet to remaining balance so the next spin can't overshoot.
            if (m_bet > m_playerData.AppleBalance) m_bet = Math.Max(1, m_playerData.AppleBalance);
        }

        private int PickTargetForMiddleSymbol(int reel, int desiredSymbol)
        {
            // Solve for a wheel index `t` such that wheel[(t+1) % len] == desiredSymbol.
            // (At rest, the middle slot's wheel index is (t + 1) mod len.) Pick a random
            // matching candidate so the reel doesn't always land on the same offset.
            int[] wheel = WheelData[reel];
            int len = wheel.Length;
            int matchCount = 0;
            for (int i = 0; i < len; i++)
            {
                if (wheel[i] == desiredSymbol) matchCount++;
            }
            int pick = m_random.Next(matchCount);
            int seen = 0;
            for (int i = 0; i < len; i++)
            {
                if (wheel[i] == desiredSymbol)
                {
                    if (seen == pick) return (i - 1 + len) % len;
                    seen++;
                }
            }
            return 0;  // fallback (every wheel contains every symbol, so this is unreachable)
        }

        private int GetMiddleSymbol(int reel)
        {
            int[] wheel = WheelData[reel];
            int wheelLen = wheel.Length;
            float offset = m_reelOffsets[reel];
            // At rest, strip-index k_top = -offset/SlotHeight is at the top slot; the middle
            // slot is one below it.
            int kMid = (int)Math.Round(-offset / SlotHeight) + 1;
            int wheelIndex = ((kMid % wheelLen) + wheelLen) % wheelLen;
            return wheel[wheelIndex];
        }

        private void UpdateReel(int reel, float dt)
        {
            switch (m_reelStates[reel])
            {
                case ReelState.Spinning:
                    m_reelOffsets[reel] += SpinSpeedPxPerSec * dt;
                    if (m_spinElapsed >= m_reelStopTimes[reel])
                    {
                        BeginStopping(reel);
                    }
                    break;

                case ReelState.Stopping:
                    m_reelStopElapsed[reel] += dt;
                    float progress = MathHelper.Clamp(m_reelStopElapsed[reel] / ReelDecelDurationSec, 0f, 1f);
                    float eased = 1f - (1f - progress) * (1f - progress);  // ease-out quadratic
                    m_reelOffsets[reel] = MathHelper.Lerp(
                        m_reelStopStartOffsets[reel],
                        m_reelStopTargetOffsets[reel],
                        eased);
                    if (progress >= 1f)
                    {
                        m_reelStates[reel] = ReelState.Idle;
                        m_audio.PlayReelStop();
                    }
                    break;
            }
        }

        private void BeginStopping(int reel)
        {
            int wheelLen = WheelData[reel].Length;

            int targetIndex;
            if (reel == 0)
            {
                // Reel 0: random target. Whatever lands in its middle slot becomes the
                // anchor symbol that later reels may be forced to match.
                targetIndex = m_random.Next(wheelLen);
                int[] wheel0 = WheelData[reel];
                m_anchorSymbol = wheel0[(targetIndex + 1) % wheelLen];
            }
            else if (m_random.NextDouble() < m_luckMeter)
            {
                // Pity hit: pick a target such that this reel's middle slot matches the anchor.
                targetIndex = PickTargetForMiddleSymbol(reel, m_anchorSymbol);
            }
            else
            {
                targetIndex = m_random.Next(wheelLen);
            }

            // Symbol with strip index k is at canvas Y = RestTopY + k*SlotHeight + offset.
            // For wheel index t to be at the top static slot we need k mod wheelLen == t,
            // i.e. offset = (n*wheelLen - t) * SlotHeight for positive integer n. Pick the
            // smallest n such that the target offset is at least decelMargin past current
            // (so the deceleration covers some forward distance).
            float current = m_reelOffsets[reel];
            float decelMargin = SpinSpeedPxPerSec * ReelDecelDurationSec * 0.5f;
            float minTarget = current + decelMargin;

            int n = (int)Math.Ceiling((minTarget / SlotHeight + targetIndex) / (float)wheelLen);
            if (n < 1) n = 1;
            float targetOffset = (n * wheelLen - targetIndex) * SlotHeight;
            while (targetOffset <= current)
            {
                n++;
                targetOffset = (n * wheelLen - targetIndex) * SlotHeight;
            }

            m_reelStopStartOffsets[reel] = current;
            m_reelStopTargetOffsets[reel] = targetOffset;
            m_reelStopElapsed[reel] = 0f;
            m_reelStates[reel] = ReelState.Stopping;
        }

        public void Draw(IGameRenderer renderer)
        {
            // Layer 1: outer cabinet — 9-slice composed from the 3x3 source.
            renderer.DrawNineSlice(
                GameRenderer.SlotsCabinet,
                new Rectangle(CabinetX, CabinetY, CabinetWidth, CabinetHeight),
                CabinetTileSize);

            // Layer 2: cabinet trim on the left + right inner edges. Same sprite, no flip.
            // Vertical 3-slice: 24-tall top cap, 16-tall tileable middle, 24-tall bottom cap.
            Rectangle trimTopSrc    = new Rectangle(0,  0, 16, 24);
            Rectangle trimMiddleSrc = new Rectangle(0, 24, 16, 16);
            Rectangle trimBottomSrc = new Rectangle(0, 40, 16, 24);
            int trimY = CabinetY + TrimYInset;
            int leftTrimX = CabinetX + TrimXInset;
            int rightTrimX = CabinetX + CabinetWidth - TrimXInset - TrimWidth;
            renderer.DrawVerticalThreeSlice(
                GameRenderer.SlotsTrim,
                new Rectangle(leftTrimX, trimY, TrimWidth, TrimHeight),
                trimTopSrc, trimMiddleSrc, trimBottomSrc,
                TrimCapDestHeight, TrimMiddleTileDestHeight);
            renderer.DrawVerticalThreeSlice(
                GameRenderer.SlotsTrim,
                new Rectangle(rightTrimX, trimY, TrimWidth, TrimHeight),
                trimTopSrc, trimMiddleSrc, trimBottomSrc,
                TrimCapDestHeight, TrimMiddleTileDestHeight);

            // Layer 3: slots window — single 24x24 image stretched to fill the window rect.
            renderer.DrawSprite(
                GameRenderer.SlotsWindow,
                new Rectangle(WindowX, WindowY, WindowWidth, WindowHeight),
                new Rectangle(0, 0, 24, 24));

            // Test layer: magenta rectangle per reel for tuning the per-reel bounds. Will be
            // replaced by the actual symbol rendering once the bounds are dialed in.
            if (ShowReelAreaTest)
            {
                int stride = ReelWidth + ReelGap;
                for (int i = 0; i < ReelCount; i++)
                {
                    renderer.DrawFilledRect(
                        new Rectangle(ReelAreaX + i * stride, ReelAreaY, ReelWidth, ReelHeight),
                        Color.Magenta);
                }
            }

            // Symbols: a single wheel-driven path. Each reel's offset determines what's
            // visible through its own fixed wheel. At rest (Idle) the offset is wherever
            // the last spin landed (or 0 if no spin yet); during spin the offset grows
            // continuously and decelerates onto a target index when stopping.
            DrawReelSymbols(renderer);

            // Layer 4: top row labels (read-only displays) + the "?" help button.
            if (renderer.HasFont)
            {
                Color labelColor = Color.Black;
                renderer.DrawSmallTextCenteredAt($"Apples-{m_playerData.AppleBalance}", TopCol1X, TopRowY, labelColor);
                renderer.DrawSmallTextCenteredAt($"Win-{m_win}", TopCol2X, TopRowY, labelColor);

                Color helpColor = m_selectedButtonIndex == HelpIndex ? m_visuals.HighlightColor : Color.Black;
                renderer.DrawSmallTextCenteredAt("?", HelpButtonX, HelpButtonY, helpColor);

                // Current bet value, centered above the bet buttons.
                renderer.DrawSmallTextCenteredAt($"{m_bet}", BetDisplayX, BetDisplayY, m_visuals.InstructionColor);

                // Layer 5: bottom row buttons. Selected button is highlighted.
                // The two bet buttons render an arrow glyph ('~' = right arrow in this font);
                // the left one mirrors the glyph to look like a left arrow.
                DrawButton(renderer, "Spin", BotCol1X, SpinIndex);
                DrawBetButton(renderer, BotCol2X, BetDownIndex, mirrorArrow: true);
                DrawBetButton(renderer, BotCol3X, BetUpIndex, mirrorArrow: false);
                DrawButton(renderer, "Back", BotCol4X, BackIndex);
            }

            // Modal paytable overlay (drawn last so it sits on top of everything).
            if (m_showPaytable)
            {
                DrawPaytableOverlay(renderer);
            }

            renderer.DrawTouchControls();
        }

        private void DrawPaytableOverlay(IGameRenderer renderer)
        {
            // Semi-transparent backdrop so the underlying UI is dimmed but still visible.
            renderer.DrawOverlay(new Color(0, 0, 0, 200));

            if (!renderer.HasFont) return;

            // Title.
            renderer.DrawCenteredText("PAYTABLE", m_visuals.HighlightColor, -68);

            // Two columns of three symbols each.
            int[] leftSymbols  = { 0, 2, 4 };  // cherry, coin, seven
            int[] rightSymbols = { 1, 3, 5 };  // bell, bar, diamond
            int[] rowYs        = { 44, 66, 88 };

            const int leftSpriteX  = 80;
            const int leftTextX    = 100;
            const int rightSpriteX = 138;
            const int rightTextX   = 158;

            for (int i = 0; i < 3; i++)
            {
                DrawPaytableEntry(renderer, leftSymbols[i],  leftSpriteX,  leftTextX,  rowYs[i]);
                DrawPaytableEntry(renderer, rightSymbols[i], rightSpriteX, rightTextX, rowYs[i]);
            }

            // Footer rule + dismissal hint. yOffset is relative to canvas vertical center (90).
            renderer.DrawCenteredText("3 IN A ROW WINS", m_visuals.InstructionColor, 24);
            renderer.DrawCenteredText("PRESS ACTION TO CLOSE", m_visuals.InstructionColor, 50);
        }

        private void DrawPaytableEntry(IGameRenderer renderer, int symbolIndex, int spriteX, int textX, int rowY)
        {
            int srcCol = symbolIndex % SymbolSheetCols;
            int srcRow = symbolIndex / SymbolSheetCols;
            Rectangle src  = new Rectangle(srcCol * SymbolSize, srcRow * SymbolSize, SymbolSize, SymbolSize);
            Rectangle dest = new Rectangle(spriteX, rowY, SymbolSize, SymbolSize);
            renderer.DrawSprite(GameRenderer.SlotsSymbols, dest, src);

            int multiplier = SymbolMultipliers[symbolIndex];
            // '*' renders as a centered times-glyph in this font; use it instead of an 'x'.
            renderer.DrawSmallTextAt($"{multiplier}*", textX, rowY + 4, m_visuals.InstructionColor, mirror: false);
        }

        private void DrawButton(IGameRenderer renderer, string label, int centerX, int index)
        {
            Color color = m_selectedButtonIndex == index ? m_visuals.HighlightColor : m_visuals.InstructionColor;
            renderer.DrawSmallTextCenteredAt(label, centerX, BottomRowY, color);
        }

        private void DrawReelSymbols(IGameRenderer renderer)
        {
            // Wheel-driven: each reel's symbols come from its own WheelData[reel] array;
            // the offset determines which slice of the wheel is visible. Symbols are spaced
            // SlotHeight apart (symbol + gap), matching the static rest layout. Vertical
            // clipping keeps symbols inside the reel area at top/bottom edges.
            int reelStride = ReelWidth + ReelGap;
            int symbolXOffset = (ReelWidth - SymbolSize) / 2;

            for (int reel = 0; reel < ReelCount; reel++)
            {
                int reelX = ReelAreaX + reel * reelStride;
                float offset = m_reelOffsets[reel];
                int[] wheel = WheelData[reel];
                int wheelLen = wheel.Length;

                // Strip index k's top edge canvas Y = RestTopY + k*SlotHeight + offset.
                int kStart = (int)Math.Floor(-(SymbolGap + offset + SymbolSize) / (float)SlotHeight);
                int kEnd = (int)Math.Ceiling((ReelHeight - SymbolGap - offset) / (float)SlotHeight);

                for (int k = kStart; k <= kEnd; k++)
                {
                    int wheelIndex = ((k % wheelLen) + wheelLen) % wheelLen;
                    int symbolIndex = wheel[wheelIndex];
                    int srcCol = symbolIndex % SymbolSheetCols;
                    int srcRow = symbolIndex / SymbolSheetCols;

                    int symbolTopY = (int)Math.Floor(RestTopY + k * SlotHeight + offset);
                    int visTop = Math.Max(symbolTopY, ReelAreaY);
                    int visBottom = Math.Min(symbolTopY + SymbolSize, ReelAreaY + ReelHeight);
                    if (visBottom <= visTop) continue;

                    int visHeight = visBottom - visTop;
                    int srcYOffset = visTop - symbolTopY;

                    Rectangle src = new Rectangle(
                        srcCol * SymbolSize, srcRow * SymbolSize + srcYOffset,
                        SymbolSize, visHeight);
                    Rectangle dest = new Rectangle(
                        reelX + symbolXOffset, visTop, SymbolSize, visHeight);
                    renderer.DrawSprite(GameRenderer.SlotsSymbols, dest, src);
                }
            }
        }

        private void DrawBetButton(IGameRenderer renderer, int centerX, int index, bool mirrorArrow)
        {
            Color color = m_selectedButtonIndex == index ? m_visuals.HighlightColor : m_visuals.InstructionColor;

            if (mirrorArrow)
            {
                // "<- Bet": mirrored '~' on the left, "Bet" on the right, with an explicit
                // pixel gap between them (the flipped glyph's whitespace lands on the
                // wrong side and would otherwise crowd the 'B').
                Vector2 arrowSize = renderer.MeasureSmallText("~");
                Vector2 betSize = renderer.MeasureSmallText("Bet");
                int totalWidth = (int)arrowSize.X + BetArrowGap + (int)betSize.X;
                int leftEdge = centerX - totalWidth / 2;
                renderer.DrawSmallTextAt("~", leftEdge, BottomRowY, color, mirror: true);
                renderer.DrawSmallTextAt("Bet", leftEdge + (int)arrowSize.X + BetArrowGap, BottomRowY, color, mirror: false);
            }
            else
            {
                // "Bet ->": just normal text since '~' is the right arrow glyph.
                renderer.DrawSmallTextCenteredAt("Bet ~", centerX, BottomRowY, color);
            }
        }
    }
}
