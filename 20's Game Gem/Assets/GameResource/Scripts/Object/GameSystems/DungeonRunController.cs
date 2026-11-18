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

        private void Awake()
        {
            _round = new DungeonRound(NextDirection);
            _view = new DungeonRunView(transform, StartRound);
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
                    AudioManager.PlaySfx("CryptStrike", Mathf.Pow(2f, (_round.LastHitPoints - 1) * 2f / 12f));
                    if (!wasFrenzy && _round.IsFrenzy)
                        AudioManager.PlaySfx("CryptEnter", 1.3f);
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
