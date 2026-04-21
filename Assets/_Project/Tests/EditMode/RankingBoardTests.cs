using System;
using NUnit.Framework;
using Alchemist.Domain.Ranking;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// RankingBoard 의 Top-N 추출 및 정렬 계약 검증.
    /// 정렬 키: Score DESC → Moves ASC → Timestamp ASC.
    /// </summary>
    [TestFixture]
    public sealed class RankingBoardTests
    {
        private static RankingEntry Make(
            string player,
            int score,
            int moves = 10,
            long tick = 100,
            string stage = "s1",
            RankingCategory cat = RankingCategory.Global)
        {
            return new RankingEntry(player, stage, score, moves, 0, tick, cat);
        }

        [Test]
        public void Top_SortsByScoreDescending()
        {
            var board = new RankingBoard(RankingCategory.Global);
            board.Add(Make("A", 100));
            board.Add(Make("B", 500));
            board.Add(Make("C", 250));

            var top = board.Top(3);
            Assert.That(top.Count, Is.EqualTo(3));
            Assert.That(top[0].PlayerId, Is.EqualTo("B"));
            Assert.That(top[1].PlayerId, Is.EqualTo("C"));
            Assert.That(top[2].PlayerId, Is.EqualTo("A"));
        }

        [Test]
        public void Top_TieBrokenByMovesAscending()
        {
            var board = new RankingBoard(RankingCategory.Global);
            board.Add(Make("slow", 300, moves: 20));
            board.Add(Make("fast", 300, moves: 5));
            var top = board.Top(2);
            Assert.That(top[0].PlayerId, Is.EqualTo("fast"));
            Assert.That(top[1].PlayerId, Is.EqualTo("slow"));
        }

        [Test]
        public void Top_TieBrokenByTimestampAscending()
        {
            var board = new RankingBoard(RankingCategory.Global);
            board.Add(Make("late", 100, moves: 5, tick: 200));
            board.Add(Make("early", 100, moves: 5, tick: 100));
            var top = board.Top(2);
            Assert.That(top[0].PlayerId, Is.EqualTo("early"));
            Assert.That(top[1].PlayerId, Is.EqualTo("late"));
        }

        [Test]
        public void Top_LimitsToN()
        {
            var board = new RankingBoard(RankingCategory.Global);
            for (int i = 0; i < 20; i++) board.Add(Make("p" + i, i));
            var top3 = board.Top(3);
            Assert.That(top3.Count, Is.EqualTo(3));
            Assert.That(top3[0].Score, Is.EqualTo(19));
        }

        [Test]
        public void Top_ZeroOrNegative_ReturnsEmpty()
        {
            var board = new RankingBoard(RankingCategory.Global);
            board.Add(Make("A", 1));
            Assert.That(board.Top(0).Count, Is.EqualTo(0));
            Assert.That(board.Top(-1).Count, Is.EqualTo(0));
        }

        [Test]
        public void TopForStage_FiltersByStage()
        {
            var board = new RankingBoard(RankingCategory.Prompt);
            board.Add(Make("A", 400, stage: "s1", cat: RankingCategory.Prompt));
            board.Add(Make("B", 999, stage: "s2", cat: RankingCategory.Prompt));
            board.Add(Make("C", 200, stage: "s1", cat: RankingCategory.Prompt));

            var s1 = board.TopForStage("s1", 10);
            Assert.That(s1.Count, Is.EqualTo(2));
            Assert.That(s1[0].PlayerId, Is.EqualTo("A"));
        }

        [Test]
        public void Add_WrongCategory_Throws()
        {
            var board = new RankingBoard(RankingCategory.Global);
            Assert.Throws<ArgumentException>(() =>
                board.Add(Make("x", 1, cat: RankingCategory.Daily)));
        }

        [Test]
        public void BestOf_ReturnsBestForPlayer()
        {
            var board = new RankingBoard(RankingCategory.Global);
            board.Add(Make("A", 100, moves: 10, tick: 1));
            board.Add(Make("A", 300, moves: 8, tick: 2));
            board.Add(Make("A", 300, moves: 9, tick: 3));
            board.Add(Make("B", 999));

            var best = board.BestOf("A");
            Assert.That(best.PlayerId, Is.EqualTo("A"));
            Assert.That(best.Score, Is.EqualTo(300));
            Assert.That(best.Moves, Is.EqualTo(8));
        }
    }
}
