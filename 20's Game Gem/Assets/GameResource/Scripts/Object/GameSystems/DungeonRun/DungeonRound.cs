using System;

namespace DdaIT.DungeonRun
{
    public enum Direction { Up, Right, Down, Left }
    public enum RoundPhase { Ready, Playing, Finished }
    public enum InputResult { Ignored, Hit, Miss }

    public sealed class DungeonRound
    {
        public const double Duration = 20.0;
        public const double LockDuration = 0.65;
        public const double FrenzyDuration = 3.0;
        public const int ChargeRequired = 5;
        public const int MaxHitPoints = 5;
        public const double ChargeLostPerSecond = 1.0;

        private readonly Func<Direction> _nextDirection;
        private double _deadline;
        private double _lockedUntil;
        private double _frenzyUntil;
        private double _decayFrom;

        public RoundPhase Phase { get; private set; }
        public Direction Prompt { get; private set; }
        public int Score { get; private set; }
        public int Slain { get; private set; }
        public double Charge { get; private set; }
        public int Frenzies { get; private set; }
        public int LastHitPoints { get; private set; }
        public double FrenzyRemaining { get; private set; }
        public bool IsFrenzy => FrenzyRemaining > 0;
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public Direction LastStrike { get; private set; }
        public double Remaining { get; private set; } = Duration;
        public double LockRemaining { get; private set; }

        /// <summary>Creates a round with the supplied prompt source.</summary>
        public DungeonRound(Func<Direction> nextDirection)
        {
            _nextDirection = nextDirection ?? throw new ArgumentNullException(nameof(nextDirection));
        }

        /// <summary>Starts a fresh round on the supplied monotonic clock.</summary>
        public void Start(double now)
        {
            _deadline = now + Duration;
            _lockedUntil = now;
            Charge = 0;
            _decayFrom = now;
            _frenzyUntil = now;
            FrenzyRemaining = 0;
            Frenzies = 0;
            LastHitPoints = 0;
            Slain = 0;
            Score = 0;
            Streak = 0;
            BestStreak = 0;
            Remaining = Duration;
            LockRemaining = 0;
            Prompt = _nextDirection();
            LastStrike = Prompt;
            Phase = RoundPhase.Playing;
        }

        /// <summary>Updates the countdown and resolves the round at its deadline.</summary>
        public void Tick(double now)
        {
            if (Phase != RoundPhase.Playing)
                return;

            Remaining = Math.Max(0, _deadline - now);
            LockRemaining = Math.Min(Remaining, Math.Max(0, _lockedUntil - now));
            FrenzyRemaining = Math.Min(Remaining, Math.Max(0, _frenzyUntil - now));
            var roundTime = Math.Min(now, _deadline);
            if (Charge > 0 && roundTime > _decayFrom)
            {
                Charge = Math.Max(0, Charge - (roundTime - _decayFrom) * ChargeLostPerSecond);
                _decayFrom = roundTime;
            }
            if (Remaining == 0)
                Phase = RoundPhase.Finished;
        }

        /// <summary>Applies a key press. A null direction represents a non-arrow key.</summary>
        public InputResult Press(Direction? direction, double now)
        {
            Tick(now);
            if (Phase != RoundPhase.Playing || LockRemaining > 0)
                return InputResult.Ignored;

            if (!IsFrenzy && !direction.HasValue)
                return InputResult.Ignored;

            if (!IsFrenzy && direction != Prompt)
            {
                Streak = 0;
                Charge = 0;
                _lockedUntil = now + LockDuration;
                LockRemaining = Math.Min(Remaining, LockDuration);
                return InputResult.Miss;
            }

            Slain++;
            if (IsFrenzy)
            {
                LastHitPoints = MaxHitPoints;
            }
            else
            {
                Charge++;
                _decayFrom = now;
                LastHitPoints = Math.Min((int)Charge, MaxHitPoints);
                if (Charge >= ChargeRequired)
                {
                    Charge = 0;
                    Frenzies++;
                    _frenzyUntil = Math.Min(_deadline, now + FrenzyDuration);
                    FrenzyRemaining = _frenzyUntil - now;
                }
            }
            Score += LastHitPoints;
            Streak++;
            BestStreak = Math.Max(BestStreak, Streak);
            LastStrike = direction ?? Prompt;
            Prompt = _nextDirection();
            return InputResult.Hit;
        }

    }
}
