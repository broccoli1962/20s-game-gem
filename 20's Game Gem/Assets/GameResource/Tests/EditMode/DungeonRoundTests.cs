using System.Collections.Generic;
using DdaIT.DungeonRun;
using NUnit.Framework;

namespace DdaIT.Tests
{
    public sealed class DungeonRoundTests
    {
        [Test]
        public void ReadyRoundIgnoresInputUntilStarted()
        {
            var round = new DungeonRound(() => Direction.Up);
            Assert.That(round.Press(Direction.Up, 10), Is.EqualTo(InputResult.Ignored));
            Assert.That(round.Remaining, Is.EqualTo(20));
            Assert.That(round.Score, Is.Zero);
        }

        [Test]
        public void EachHitAddsOneKillAndConsumesTheNextPrompt()
        {
            var directions = new Queue<Direction>(new[] { Direction.Up, Direction.Left, Direction.Down });
            var round = new DungeonRound(directions.Dequeue);
            round.Start(100);
            Assert.That(round.Press(Direction.Up, 101), Is.EqualTo(InputResult.Hit));
            Assert.That(round.Score, Is.EqualTo(1));
            Assert.That(round.Prompt, Is.EqualTo(Direction.Left));
            Assert.That(round.Press(Direction.Left, 103), Is.EqualTo(InputResult.Hit));
            Assert.That(round.Score, Is.EqualTo(2));
            Assert.That(round.Prompt, Is.EqualTo(Direction.Down));
        }

        [Test]
        public void MissKeepsPromptAndLocksInputWhileTimeContinues()
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(10);
            Assert.That(round.Press(Direction.Left, 11), Is.EqualTo(InputResult.Miss));
            Assert.That(round.LockRemaining, Is.EqualTo(0.65).Within(0.0001));
            Assert.That(round.Press(Direction.Down, 11.2), Is.EqualTo(InputResult.Ignored));
            Assert.That(round.Press(Direction.Up, 11.4), Is.EqualTo(InputResult.Ignored));
            Assert.That(round.Remaining, Is.EqualTo(18.6).Within(0.0001));
            Assert.That(round.Prompt, Is.EqualTo(Direction.Up));
            Assert.That(round.Score, Is.Zero);
            Assert.That(round.Press(Direction.Up, 11 + DungeonRound.LockDuration), Is.EqualTo(InputResult.Hit));
        }

        [Test]
        public void MissBreaksTheStreakAndBestSurvivesUntilRestart()
        {
            var round = new DungeonRound(() => Direction.Left);
            round.Start(0);
            round.Press(Direction.Left, 1);
            round.Press(Direction.Left, 2);
            round.Press(Direction.Right, 3);
            round.Press(Direction.Left, 3.2);
            Assert.That(round.Streak, Is.Zero);
            Assert.That(round.BestStreak, Is.EqualTo(2));
            round.Press(Direction.Left, 4);
            round.Tick(20);
            Assert.That(round.Streak, Is.EqualTo(1));
            Assert.That(round.Score, Is.EqualTo(3));
            Assert.That(round.BestStreak, Is.EqualTo(2));
            round.Start(21);
            Assert.That(round.Streak, Is.Zero);
            Assert.That(round.BestStreak, Is.Zero);
        }

        [Test]
        public void StrikeFollowsTheAcceptedArrowWhileThePromptAdvances()
        {
            var directions = new Queue<Direction>(new[] { Direction.Right, Direction.Down });
            var round = new DungeonRound(directions.Dequeue);
            round.Start(0);
            round.Press(Direction.Right, 1);
            round.Press(Direction.Left, 2);
            Assert.That(round.LastStrike, Is.EqualTo(Direction.Right));
            Assert.That(round.Prompt, Is.EqualTo(Direction.Down));
        }

        [Test]
        public void DeadlineWinsOverInputAndClearsAnActiveLock()
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(31);
            Assert.That(round.Press(Direction.Up, 50.5), Is.EqualTo(InputResult.Hit));
            Assert.That(round.Press(Direction.Down, 50.8), Is.EqualTo(InputResult.Miss));
            Assert.That(round.Press(Direction.Up, 51), Is.EqualTo(InputResult.Ignored));
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Finished));
            Assert.That(round.Remaining, Is.Zero);
            Assert.That(round.LockRemaining, Is.Zero);
            Assert.That(round.Score, Is.EqualTo(1));
        }

        [Test]
        public void QuickHitsRefreshTheWindowAndCapTheAwardWithoutCappingCombo()
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(0);
            for (var i = 1; i <= 6; i++)
            {
                round.Press(Direction.Up, i * .5);
                Assert.That(round.LastHitPoints, Is.EqualTo(System.Math.Min(i, 5)));
                Assert.That(round.ComboRemaining, Is.EqualTo(DungeonRound.ComboWindow));
            }
            Assert.That(round.Score, Is.EqualTo(20));
            Assert.That(round.Slain, Is.EqualTo(6));
            Assert.That(round.BestCombo, Is.EqualTo(6));
        }

        [TestCase(.999, 2, 3)]
        [TestCase(1.0, 1, 2)]
        [TestCase(1.001, 1, 2)]
        public void OnlyAnInputBeforeExpiryContinuesTheCombo(double gap, int combo, int score)
        {
            var round = new DungeonRound(() => Direction.Left);
            round.Start(0);
            round.Press(Direction.Left, 1);
            round.Press(Direction.Left, 1 + gap);
            Assert.That(round.Combo, Is.EqualTo(combo));
            Assert.That(round.Score, Is.EqualTo(score));
        }

        [Test]
        public void MissLockAndRoundEndCannotPreserveOrExtendBonus()
        {
            var round = new DungeonRound(() => Direction.Left);
            round.Start(0);
            round.Press(Direction.Left, 1);
            round.Press(Direction.Left, 1.1);
            round.Press(Direction.Right, 1.2);
            round.Press(Direction.Left, 1.3);
            Assert.That(round.ComboRemaining, Is.Zero);
            Assert.That(round.Slain, Is.EqualTo(2));
            round.Press(Direction.Left, 2);
            Assert.That(round.LastHitPoints, Is.EqualTo(1));
            round.Tick(3);
            Assert.That(round.Combo, Is.Zero);
            round.Press(Direction.Left, 19.75);
            Assert.That(round.ComboRemaining, Is.EqualTo(.25));
            round.Press(Direction.Left, 20);
            Assert.That(round.Score, Is.EqualTo(5));
            Assert.That(round.BestCombo, Is.EqualTo(2));
            round.Start(21);
            Assert.That(round.BestCombo, Is.Zero);
            Assert.That(round.Slain, Is.Zero);
            Assert.That(round.LastHitPoints, Is.Zero);
        }

        [Test]
        public void TickFinishesWithoutInputAndRestartResetsScoreAndClock()
        {
            var round = new DungeonRound(() => Direction.Right);
            round.Start(1);
            round.Press(Direction.Right, 2);
            round.Tick(21);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Finished));
            Assert.That(round.Score, Is.EqualTo(1));
            round.Start(25);
            Assert.That(round.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(round.Score, Is.Zero);
            Assert.That(round.Remaining, Is.EqualTo(20));
            Assert.That(round.LockRemaining, Is.Zero);
        }
    }
}
