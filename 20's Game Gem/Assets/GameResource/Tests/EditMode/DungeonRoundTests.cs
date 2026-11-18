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
            Assert.That(round.Press(Direction.Left, 102), Is.EqualTo(InputResult.Hit));
            Assert.That(round.Score, Is.EqualTo(3));
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
            Assert.That(round.Score, Is.EqualTo(4));
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
        public void FiveTimelyArrowsChargeFrenzy()
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(0);
            for (int i = 1; i < 5; i++)
            {
                round.Press(Direction.Up, i);
                Assert.That(round.Charge, Is.EqualTo(i));
                Assert.That(round.IsFrenzy, Is.False);
            }
            round.Press(Direction.Up, 5);
            Assert.That(round.Frenzies, Is.EqualTo(1));
            Assert.That(round.FrenzyRemaining, Is.EqualTo(3));
            Assert.That(round.Score, Is.EqualTo(15));
            Assert.That(round.Press(null, 6), Is.EqualTo(InputResult.Hit));
            Assert.That(round.Press(Direction.Left, 6), Is.EqualTo(InputResult.Hit));
            Assert.That(round.FrenzyRemaining, Is.EqualTo(2));
            Assert.That(round.Slain, Is.EqualTo(7));
            Assert.That(round.Score, Is.EqualTo(25));
            Assert.That(round.Charge, Is.Zero);
        }

        [Test]
        public void IdleChargeDrainsGraduallyAndIgnoredKeysDoNotRefreshIt()
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(0);
            round.Press(Direction.Up, 1);
            round.Press(Direction.Up, 1.5);
            round.Tick(2.5);
            Assert.That(round.Charge, Is.EqualTo(2));
            Assert.That(round.Press(null, 2.75), Is.EqualTo(InputResult.Ignored));
            Assert.That(round.Charge, Is.EqualTo(1.75));
            round.Tick(3);
            Assert.That(round.Charge, Is.EqualTo(1.5));
            round.Tick(10);
            Assert.That(round.Charge, Is.Zero);
            Assert.That(round.Score, Is.EqualTo(3));
        }

        [Test]
        public void NextArrowRefillsFromTheRemainingChargeAndRenewsTheGracePeriod()
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(0);
            for (int i = 1; i <= 4; i++) round.Press(Direction.Up, i);
            round.Press(Direction.Up, 5.5);
            Assert.That(round.Charge, Is.EqualTo(4.5));
            Assert.That(round.LastHitPoints, Is.EqualTo(4));
            Assert.That(round.IsFrenzy, Is.False);
            round.Tick(6.5);
            Assert.That(round.Charge, Is.EqualTo(4.5));
            round.Press(Direction.Up, 6.5);
            Assert.That(round.FrenzyRemaining, Is.EqualTo(DungeonRound.FrenzyDuration));
            Assert.That(round.Charge, Is.Zero);
        }

        [TestCase(2.999, InputResult.Hit)]
        [TestCase(3.0, InputResult.Ignored)]
        [TestCase(3.001, InputResult.Ignored)]
        public void NonArrowKeysOnlyScoreBeforeFrenzyExpires(double elapsed, InputResult expected)
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(0);
            for (int i = 1; i <= 5; i++) round.Press(Direction.Up, i);
            Assert.That(round.Press(null, 5 + elapsed), Is.EqualTo(expected));
            Assert.That(round.Press(Direction.Left, 9), Is.EqualTo(InputResult.Miss));
            Assert.That(round.Press(Direction.Up, 10), Is.EqualTo(InputResult.Hit));
            Assert.That(round.LastHitPoints, Is.EqualTo(1));
        }

        [Test]
        public void MissClearsChargeAndLockedOrNonArrowKeysCannotRebuildIt()
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(0);
            round.Press(Direction.Up, 1);
            Assert.That(round.Press(null, 1.1), Is.EqualTo(InputResult.Ignored));
            Assert.That(round.Charge, Is.EqualTo(1));
            round.Press(Direction.Down, 2);
            Assert.That(round.Press(Direction.Up, 2.1), Is.EqualTo(InputResult.Ignored));
            Assert.That(round.Charge, Is.Zero);
        }

        [Test]
        public void RoundDeadlineCutsFrenzyShortAndRestartClearsIt()
        {
            var round = new DungeonRound(() => Direction.Up);
            round.Start(0);
            for (int i = 15; i < 20; i++) round.Press(Direction.Up, i);
            Assert.That(round.FrenzyRemaining, Is.EqualTo(1));
            Assert.That(round.Press(null, 20), Is.EqualTo(InputResult.Ignored));
            Assert.That(round.IsFrenzy, Is.False);
            round.Start(21);
            Assert.That(round.Charge, Is.Zero);
            Assert.That(round.Frenzies, Is.Zero);
            Assert.That(round.Press(null, 21), Is.EqualTo(InputResult.Ignored));
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
