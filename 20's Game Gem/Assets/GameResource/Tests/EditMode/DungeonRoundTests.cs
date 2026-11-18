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
