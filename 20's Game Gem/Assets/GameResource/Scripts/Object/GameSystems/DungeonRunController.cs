using UnityEngine;
using UnityEngine.InputSystem;

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
            _round.Tick(now);

            if (_round.Phase == RoundPhase.Playing)
            {
                if (ReadDirection(keyboard) is Direction direction)
                {
                    switch (_round.Press(direction, now))
                    {
                        case InputResult.Hit:
                            _hitAt = now;
                            _missAt = double.NegativeInfinity;
                            break;
                        case InputResult.Miss:
                            _missAt = now;
                            break;
                    }
                }
            }
            else if (keyboard != null &&
                     (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                StartRound();
            }

            _view.Draw(_round, (float)now, (float)(now - _hitAt), (float)(now - _missAt));
        }

        private void StartRound()
        {
            if (_round.Phase == RoundPhase.Playing)
                return;

            _previousDirection = -1;
            _hitAt = double.NegativeInfinity;
            _missAt = double.NegativeInfinity;
            _round.Start(Time.unscaledTimeAsDouble);
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
