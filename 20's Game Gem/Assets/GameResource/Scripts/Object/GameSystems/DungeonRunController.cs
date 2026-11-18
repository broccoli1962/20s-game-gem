using UnityEngine;
using UnityEngine.InputSystem;
using DdaIT.Object.Management;

namespace DdaIT.DungeonRun
{
    public sealed class DungeonRunController : MonoBehaviour
    {
        private DungeonRound _round;
        private DungeonRunView _view;
        private int _previousDirection = -1;
        private double _hitAt = double.NegativeInfinity;
        private double _missAt = double.NegativeInfinity;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void CryptRushInstallAudioUnlock();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnableBrowserAudio() => CryptRushInstallAudioUnlock();
#endif

        private void Awake()
        {
            _round = new DungeonRound(NextDirection);
            _view = new DungeonRunView(transform, StartRound, () => AudioManager.PlaySfx("CryptEnter"));
        }

        private void Update()
        {
            var now = Time.unscaledTimeAsDouble;
            var keyboard = Keyboard.current;
            var previousPhase = _round.Phase;
            var wasFrenzy = _round.IsFrenzy;
            _round.Tick(now);
            if (previousPhase == RoundPhase.Playing && _round.Phase == RoundPhase.Finished)
                AudioManager.PlaySfx("CryptFinish");
            else if (wasFrenzy && !_round.IsFrenzy)
                AudioManager.PlaySfx("CryptComboFade");

            if (_round.Phase == RoundPhase.Playing)
            {
                if (_round.IsFrenzy && keyboard != null)
                {
                    foreach (var key in keyboard.allKeys)
                    {
                        if (key.wasPressedThisFrame)
                            Strike(key.keyCode switch
                            {
                                Key.UpArrow => Direction.Up,
                                Key.RightArrow => Direction.Right,
                                Key.DownArrow => Direction.Down,
                                Key.LeftArrow => Direction.Left,
                                _ => (Direction?)null
                            }, now);
                    }
                }
                else if (ReadDirection(keyboard) is Direction direction)
                    Strike(direction, now);
            }
            else if (previousPhase != RoundPhase.Playing && keyboard != null &&
                     (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                StartRound();
            }

            _view.Draw(_round, (float)now, (float)(now - _hitAt), (float)(now - _missAt));
        }

        private void Strike(Direction? direction, double now)
        {
            var wasFrenzy = _round.IsFrenzy;
            switch (_round.Press(direction, now))
            {
                case InputResult.Hit:
                    if (wasFrenzy)
                    {
                        // 동시에 누른 키는 각각 채점하되 같은 타격음이 겹쳐 커지지 않게 한다.
                        if (_hitAt != now)
                            AudioManager.PlaySfx("CryptFrenzyStrike");
                    }
                    else
                    {
                        AudioManager.PlaySfx("CryptStrike");
                        if (_round.IsFrenzy)
                            AudioManager.PlaySfx("CryptFrenzy");
                        else
                            AudioManager.PlaySfx("CryptCharge", Mathf.Pow(2f, (_round.LastHitPoints - 1) * 3f / 12f));
                    }
                    _hitAt = now;
                    _missAt = double.NegativeInfinity;
                    break;
                case InputResult.Miss:
                    AudioManager.PlaySfx("CryptStagger");
                    _missAt = now;
                    break;
            }
        }

        private void StartRound()
        {
            if (_round.Phase == RoundPhase.Playing)
                return;

            if (_round.Phase == RoundPhase.Ready)
                AudioManager.PlayBgm("CryptMachinery");

            _previousDirection = -1;
            _hitAt = double.NegativeInfinity;
            _missAt = double.NegativeInfinity;
            _round.Start(Time.unscaledTimeAsDouble);
            AudioManager.PlaySfx("CryptEnter");
        }

        private Direction NextDirection()
        {
            _previousDirection = _previousDirection < 0
                ? Random.Range(0, 4)
                : (_previousDirection + Random.Range(1, 4)) % 4;
            return (Direction)_previousDirection;
        }

        private static Direction? ReadDirection(Keyboard keyboard)
        {
            if (keyboard == null)
                return null;
            if (keyboard.upArrowKey.wasPressedThisFrame)
                return Direction.Up;
            if (keyboard.rightArrowKey.wasPressedThisFrame)
                return Direction.Right;
            if (keyboard.downArrowKey.wasPressedThisFrame)
                return Direction.Down;
            if (keyboard.leftArrowKey.wasPressedThisFrame)
                return Direction.Left;
            return null;
        }
    }
}
