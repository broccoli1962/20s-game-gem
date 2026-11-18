using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DdaIT.DungeonRun
{
    internal sealed class DungeonRunView
    {
        private static readonly Color Ivory = new Color32(246, 234, 206, 255);
        private static readonly Color Amber = new Color32(220, 167, 91, 255);
        private static readonly Color Ember = new Color32(255, 141, 114, 255);
        private static readonly Color Mint = new Color32(148, 233, 203, 255);
        private static readonly Color Ink = new Color32(14, 13, 15, 240);
        private static readonly Color Muted = new Color32(189, 175, 157, 255);

        private readonly UnityEngine.UI.RawImage _corridor;
        private readonly UnityEngine.UI.RawImage _nextCorridor;
        private readonly UnityEngine.UI.RawImage _monster;
        private readonly UnityEngine.UI.RawImage _defeated;
        private readonly UnityEngine.UI.RawImage _prompt;
        private readonly UnityEngine.UI.Image _slash;
        private readonly UnityEngine.UI.Image _flash;
        private readonly UnityEngine.UI.Image _clockBar;
        private readonly UnityEngine.UI.Image _recoveryBar;
        private readonly UnityEngine.UI.Image _comboFill;
        private readonly TMP_Text _combo;
        private readonly TMP_Text _anyKey;
        private readonly TMP_Text _slain;
        private readonly Texture2D[] _monsters;
        private readonly RectTransform _hud;
        private readonly RectTransform _menu;
        private readonly TMP_Text _clock;
        private readonly TMP_Text _score;
        private readonly TMP_Text _status;
        private readonly TMP_Text _eyebrow;
        private readonly TMP_Text _heading;
        private readonly TMP_Text _description;
        private readonly TMP_Text _buttonLabel;
        private float _depth;

        internal DungeonRunView(Transform owner, Action start)
        {
            var canvas = new GameObject("Dungeon Run Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.transform.SetParent(owner, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 10;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var input = new GameObject("Dungeon Run Event System", typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                input.transform.SetParent(owner, false);
            }

            var root = canvas.transform;
            Plate(root, "Letterbox", 0, 0, 1, 1, Color.black);
            var stage = Region(root, "Dungeon", 0, 0, 1, 1);
            var fit = stage.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 16f / 9f;
            stage.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

            var corridorArt = Art("Catacomb");
            _monsters = new[] { Art("Revenant"), Art("Bat"), Art("Mimic") };
            _corridor = Picture(stage, "Catacomb", 0, 0, 1, 1, corridorArt);
            _nextCorridor = Picture(stage, "Next Passage", 0, 0, 1, 1, corridorArt);
            _monster = Picture(stage, "Monster", .367f, .275f, .633f, .808f, _monsters[0]);
            _defeated = Picture(stage, "Defeated Monster", .367f, .275f, .633f, .808f, _monsters[0]);
            _slash = Plate(stage, "Strike", .31f, .456f, .69f, .468f, Color.clear);
            _flash = Plate(stage, "Impact", 0, 0, 1, 1, Color.clear);

            _hud = Region(stage, "Run HUD", 0, 0, 1, 1);
            var timer = Card(_hud, "Timer", .37f, .84f, .63f, .98f);
            Label(timer, "TIME LEFT", .06f, .67f, .94f, .95f, 16, Amber);
            _clock = Label(timer, "20.0", .04f, .18f, .96f, .68f, 42, Ivory, true);
            Plate(timer, "Timer Track", .07f, .12f, .93f, .17f, new Color32(65, 52, 43, 255));
            _clockBar = Plate(timer, "Time Remaining", .07f, .12f, .93f, .17f, Amber);
            var scoreCard = Card(_hud, "Score", .825f, .84f, .975f, .98f);
            Label(scoreCard, "SCORE", .06f, .67f, .94f, .95f, 16, Amber);
            _score = Label(scoreCard, "00", .04f, .12f, .96f, .68f, 42, Ivory, true);
            var killCard = Card(_hud, "Kills", .025f, .84f, .175f, .98f);
            Label(killCard, "SLAIN", .06f, .67f, .94f, .95f, 16, Amber);
            _slain = Label(killCard, "0", .04f, .12f, .96f, .68f, 42, Ivory, true);

            var inputCard = Card(_hud, "Direction", .374f, .024f, .626f, .252f);
            _prompt = Picture(inputCard, "Direction Key", .354f, .27f, .646f, .864f, Art("DirectionKey"));
            _prompt.uvRect = new Rect(.14f, .14f, .72f, .72f);
            _anyKey = Label(inputCard, "ANY KEY", .03f, .27f, .97f, .86f, 48, Amber, true);
            _status = Label(inputCard, "MATCH THE ARROW", .04f, .055f, .96f, .22f, 17, Muted, true);
            _recoveryBar = Plate(inputCard, "Stagger Remaining", 0, 0, 1, .04f, Ember);

            var comboCard = Card(_hud, "Combo", .644f, .055f, .918f, .218f);
            _combo = Label(comboCard, "COMBO", .04f, .49f, .96f, .90f, 29, Ivory, true);
            var meter = Region(comboCard, "Combo Meter", .05f, .14f, .95f, .46f);
            Plate(meter, "Track", .085f, .24f, .915f, .76f, new Color32(44, 38, 56, 255));
            _comboFill = Plate(meter, "Frenzy Gauge", .085f, .24f, .915f, .76f, Mint);
            var frame = Picture(meter, "Bone Frame", 0, 0, 1, 1, Art("ComboFrame"));
            frame.uvRect = Rect.MinMaxRect(.013f, .33f, .987f, .665f);

            _menu = Region(stage, "Start and Results", 0, 0, 1, 1);
            Plate(_menu, "Shade", 0, 0, 1, 1, new Color32(6, 8, 10, 85));
            var menuCard = Card(_menu, "Run Card", .075f, .16f, .51f, .84f);
            _eyebrow = Label(menuCard, "20-SECOND DUNGEON RUN", .1f, .80f, .90f, .91f, 20, Amber, true);
            _heading = Label(menuCard, "CRYPT\nRUSH", .09f, .43f, .91f, .79f, 87, Ivory, true);
            _heading.lineSpacing = -26;
            _description = Label(menuCard, "Match the arrows to slay and advance.\nA wrong key briefly locks your attack.",
                .065f, .26f, .935f, .424f, 22, Muted);

            var buttonArt = Plate(menuCard, "Start Button", .19f, .115f, .81f, .248f, Amber);
            buttonArt.raycastTarget = true;
            var button = buttonArt.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = buttonArt;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            button.onClick.AddListener(() => start());
            _buttonLabel = Label(buttonArt.rectTransform, "ENTER THE CRYPT", 0, 0, 1, 1, 23,
                new Color32(31, 20, 13, 255), true);
            Label(menuCard, "ENTER / SPACE", .10f, .025f, .90f, .10f, 15, Muted);
        }

        internal void Draw(DungeonRound round, float now, float hitAge, float missAge)
        {
            var playing = round.Phase == RoundPhase.Playing;
            var finished = round.Phase == RoundPhase.Finished;
            var locked = round.LockRemaining > 0;
            _hud.gameObject.SetActive(playing);
            _menu.gameObject.SetActive(!playing);

            var advance = Mathf.Clamp01(hitAge / .28f);
            var attack = 1 - advance;
            var stagger = 1 - Mathf.Clamp01(missAge / .23f);
            _depth = round.Slain < _depth
                ? round.Slain
                : Mathf.MoveTowards(_depth, round.Slain, Time.unscaledDeltaTime * 8);
            var passageDepth = Mathf.Repeat(_depth, 4);
            var sway = Mathf.Sin(stagger * 31) * .012f * stagger;
            _corridor.uvRect = CorridorCrop(1.045f + passageDepth * .092f, sway);
            _nextCorridor.uvRect = CorridorCrop(1.045f, sway);
            _nextCorridor.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1,
                Mathf.InverseLerp(3.35f, 4, passageDepth)));

            var monster = _monster.rectTransform;
            _monster.texture = _monsters[round.Slain % _monsters.Length];
            _defeated.texture = _monsters[Math.Max(0, round.Slain - 1) % _monsters.Length];
            var arrive = Mathf.SmoothStep(0, 1, Mathf.Clamp01((hitAge - .055f) / .16f));
            var idleBob = Mathf.Sin(now * 4) * 3;
            monster.anchoredPosition = playing ? new Vector2(0, idleBob) : new Vector2(270, idleBob - 70);
            monster.localScale = Vector3.one * (playing ? Mathf.Lerp(.55f, 1, arrive) : 1.22f);
            _monster.color = new Color(1, 1, 1, playing ? arrive : finished ? .48f : 1);
            _defeated.rectTransform.localScale = Vector3.one * (1 + .25f * advance);
            var strikeRotation = Quaternion.Euler(0, 0, -90 * (int)round.LastStrike);
            var strikeDirection = (Vector2)(strikeRotation * Vector3.up);
            _defeated.rectTransform.anchoredPosition = strikeDirection * (100 * advance);
            _defeated.rectTransform.localRotation = Quaternion.Euler(0, 0, -strikeDirection.x * 20 * advance);
            _defeated.color = new Color(1, .64f, .32f, playing ? attack * attack : 0);
            _slash.rectTransform.localRotation = strikeRotation * Quaternion.Euler(0, 0, 76);
            _slash.rectTransform.anchoredPosition = strikeDirection * Mathf.Lerp(-60, 90, advance);
            _slash.rectTransform.localScale = new Vector3(Mathf.Lerp(.1f, 1, Mathf.Clamp01(hitAge / .09f)), 1, 1);
            var slashAlpha = playing ? Mathf.Pow(attack, 2.4f) : 0;
            _slash.color = new Color(1, .81f, .49f, slashAlpha);
            _flash.color = stagger > 0
                ? new Color(.66f, .094f, .04f, stagger * .38f)
                : new Color(1, .61f, .27f, attack * .12f);

            if (!playing)
            {
                _eyebrow.text = finished ? "FINAL SCORE" : "20-SECOND DUNGEON RUN";
                _heading.text = finished ? round.Score.ToString("00", CultureInfo.InvariantCulture) : "CRYPT\nRUSH";
                _description.text = finished ? $"{round.Slain} slain   /   Frenzies: {round.Frenzies}\nLongest streak: {round.BestStreak}" :
                    "Match 5 arrows in a row to charge Frenzy.\nThen mash any keys for 3 seconds!";
                _buttonLabel.text = finished ? "RUN AGAIN" : "ENTER THE CRYPT";
                return;
            }

            _clock.text = (Math.Ceiling(round.Remaining * 10) / 10).ToString("00.0", CultureInfo.InvariantCulture);
            _score.text = round.Score.ToString("00", CultureInfo.InvariantCulture);
            _slain.SetText("{0}", round.Slain);
            var frenzy = round.IsFrenzy;
            var fraction = frenzy ? (float)(round.FrenzyRemaining / DungeonRound.FrenzyDuration)
                : (float)round.Charge / DungeonRound.ChargeRequired;
            if (frenzy)
                _combo.SetText("FRENZY  {0:1}s", (float)(Math.Ceiling(round.FrenzyRemaining * 10) / 10));
            else
                _combo.SetText("CHARGE  {0}/{1}", round.Charge, DungeonRound.ChargeRequired);
            _combo.color = frenzy ? Amber : Ivory;
            _comboFill.color = frenzy ? (fraction < .25f ? Ember : Amber) : Mint;
            Place(_comboFill.rectTransform, .085f, .24f, .085f + .83f * fraction, .76f);
            var urgent = round.Remaining <= 5;
            _clock.color = urgent ? Ember : Ivory;
            _clockBar.color = urgent ? Ember : Amber;
            Place(_clockBar.rectTransform, .07f, .12f, .07f + .86f * (float)(round.Remaining / DungeonRound.Duration), .17f);

            _prompt.rectTransform.localRotation = Quaternion.Euler(0, 0, -90 * (int)round.Prompt);
            _prompt.enabled = !frenzy;
            _anyKey.enabled = frenzy;
            _prompt.color = locked ? new Color32(241, 122, 99, 195) : Color.white;
            Place(_recoveryBar.rectTransform, 0, 0, (float)(round.LockRemaining / DungeonRound.LockDuration), .04f);
            var hit = hitAge < .3f;
            _status.color = locked ? Ember : hit ? Mint : Muted;
            _status.text = locked
                ? $"STAGGERED  {(Math.Ceiling(round.LockRemaining * 10) / 10).ToString("0.0", CultureInfo.InvariantCulture)}s"
                : hit ? $"+{round.LastHitPoints} SCORE" : frenzy ? "KEEP SLASHING" : "MATCH THE ARROW";
        }

        private static Texture2D Art(string name)
            => Resources.Load<Texture2D>($"DungeonRun/{name}");

        private static Rect CorridorCrop(float zoom, float sway)
        {
            var width = 1 / zoom;
            return new Rect((1 - width) * .5f + sway, (1 - width) * .387f, width, width);
        }

        private static RectTransform Region(Transform parent, string name, float left, float bottom, float right, float top)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            Place(rect, left, bottom, right, top);
            return rect;
        }

        private static void Place(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Card(Transform parent, string name, float left, float bottom, float right, float top)
        {
            var card = Plate(parent, name, left, bottom, right, top, Ink).rectTransform;
            Plate(card, "Top Edge", 0, .986f, 1, 1, Amber);
            Plate(card, "Bottom Edge", 0, 0, 1, .012f, new Color32(104, 78, 49, 255));
            return card;
        }

        private static UnityEngine.UI.Image Plate(Transform parent, string name, float left, float bottom,
            float right, float top, Color color)
        {
            var image = Region(parent, name, left, bottom, right, top).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static UnityEngine.UI.RawImage Picture(Transform parent, string name, float left, float bottom,
            float right, float top, Texture2D texture)
        {
            var image = Region(parent, name, left, bottom, right, top).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture = texture;
            image.color = texture == null ? Color.clear : Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Label(Transform parent, string text, float left, float bottom, float right, float top,
            float size, Color color, bool bold = false)
        {
            var label = Region(parent, text, left, bottom, right, top).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontSizeMin = size * .62f;
            label.fontSizeMax = size;
            label.enableAutoSizing = true;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }
    }
}
