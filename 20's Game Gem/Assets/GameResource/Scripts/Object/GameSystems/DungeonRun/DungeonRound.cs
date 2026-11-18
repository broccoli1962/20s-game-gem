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

        private readonly Func<Direction> _nextDirection;
        private double _deadline;
        private double _lockedUntil;

        public RoundPhase Phase { get; private set; }
        public Direction Prompt { get; private set; }
        public int Score { get; private set; }
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
            if (Remaining == 0)
                Phase = RoundPhase.Finished;
        }

        /// <summary>Applies a direction only while the round and input window are open.</summary>
        public InputResult Press(Direction direction, double now)
        {
            Tick(now);
            if (Phase != RoundPhase.Playing || LockRemaining > 0)
                return InputResult.Ignored;

            if (direction != Prompt)
            {
                Streak = 0;
                _lockedUntil = now + LockDuration;
                LockRemaining = Math.Min(Remaining, LockDuration);
                return InputResult.Miss;
            }

            Score++;
            Streak++;
            BestStreak = Math.Max(BestStreak, Streak);
            LastStrike = direction;
            Prompt = _nextDirection();
            return InputResult.Hit;
        }
    }
}
