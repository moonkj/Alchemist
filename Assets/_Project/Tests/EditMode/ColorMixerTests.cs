using NUnit.Framework;
using Alchemist.Domain.Colors;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="ColorMixer"/> and <see cref="ColorMixCache"/>.
    /// Covers primary/secondary/tertiary mixing, specials (Prism, Black, White),
    /// None/Gray treatment, color classification, and cache/function equivalence
    /// per docs/phase1_wave1_addendum.md §C4.
    /// </summary>
    [TestFixture]
    public sealed class ColorMixerTests
    {
        // ---------- Primary x Primary => Secondary ----------

        [Test]
        public void Mix_RedBlue_YieldsPurple()
        {
            Assert.That(ColorMixer.Mix(ColorId.Red, ColorId.Blue), Is.EqualTo(ColorId.Purple));
        }

        [Test]
        public void Mix_RedYellow_YieldsOrange()
        {
            Assert.That(ColorMixer.Mix(ColorId.Red, ColorId.Yellow), Is.EqualTo(ColorId.Orange));
        }

        [Test]
        public void Mix_YellowBlue_YieldsGreen()
        {
            Assert.That(ColorMixer.Mix(ColorId.Yellow, ColorId.Blue), Is.EqualTo(ColorId.Green));
        }

        // ---------- Three-primary route => White ----------

        [Test]
        public void Mix_OrangeBlue_YieldsWhite()
        {
            // Orange (R|Y) + Blue => R|Y|B = White
            Assert.That(ColorMixer.Mix(ColorId.Orange, ColorId.Blue), Is.EqualTo(ColorId.White));
        }

        [Test]
        public void Mix_PurpleYellow_YieldsWhite()
        {
            Assert.That(ColorMixer.Mix(ColorId.Purple, ColorId.Yellow), Is.EqualTo(ColorId.White));
        }

        // ---------- Secondary x Secondary (oversaturation) ----------

        [Test]
        public void Mix_PurpleOrange_YieldsWhite()
        {
            // Purple (R|B) + Orange (R|Y) => R|Y|B = White (all three primaries covered)
            Assert.That(ColorMixer.Mix(ColorId.Purple, ColorId.Orange), Is.EqualTo(ColorId.White));
        }

        // ---------- Prism wildcard ----------

        [Test]
        public void Mix_PrismRed_YieldsRed()
        {
            Assert.That(ColorMixer.Mix(ColorId.Prism, ColorId.Red), Is.EqualTo(ColorId.Red));
        }

        [Test]
        public void Mix_PrismPrism_YieldsPrism()
        {
            // C4: Prism + Prism = Prism.
            Assert.That(ColorMixer.Mix(ColorId.Prism, ColorId.Prism), Is.EqualTo(ColorId.Prism));
        }

        [Test]
        public void Mix_PrismNone_YieldsNone()
        {
            // Prism passes the "other side" through; other side is None.
            Assert.That(ColorMixer.Mix(ColorId.Prism, ColorId.None), Is.EqualTo(ColorId.None));
        }

        // ---------- White saturation & Black propagation (C4) ----------

        [Test]
        public void Mix_WhiteRed_YieldsBlack()
        {
            // C4: Mix(White, *primary) = Black (oversaturated).
            Assert.That(ColorMixer.Mix(ColorId.White, ColorId.Red), Is.EqualTo(ColorId.Black));
        }

        [Test]
        public void Mix_WhiteWhite_YieldsBlack()
        {
            // C4: Mix(White, White) = Black.
            Assert.That(ColorMixer.Mix(ColorId.White, ColorId.White), Is.EqualTo(ColorId.Black));
        }

        [Test]
        public void Mix_BlackRed_YieldsBlack_D16Propagation()
        {
            // D16: Black is infectious — propagates to anything non-Gray.
            Assert.That(ColorMixer.Mix(ColorId.Black, ColorId.Red), Is.EqualTo(ColorId.Black));
        }

        [Test]
        public void Mix_PrismGray_YieldsNone_D17GrayOverridesPrism()
        {
            Assert.That(ColorMixer.Mix(ColorId.Prism, ColorId.Gray), Is.EqualTo(ColorId.None));
        }

        [Test]
        public void Mix_PrismBlack_YieldsBlack_D17BlackOverridesPrism()
        {
            Assert.That(ColorMixer.Mix(ColorId.Prism, ColorId.Black), Is.EqualTo(ColorId.Black));
        }

        // ---------- None / Gray sink ----------

        [Test]
        public void Mix_NoneRed_YieldsNone()
        {
            Assert.That(ColorMixer.Mix(ColorId.None, ColorId.Red), Is.EqualTo(ColorId.None));
        }

        [Test]
        public void Mix_GrayRed_YieldsNone()
        {
            // C4: Gray (absorbed) does not participate in normal mixing.
            Assert.That(ColorMixer.Mix(ColorId.Gray, ColorId.Red), Is.EqualTo(ColorId.None));
        }

        // ---------- Classification helpers ----------

        [TestCase(ColorId.Red, true)]
        [TestCase(ColorId.Yellow, true)]
        [TestCase(ColorId.Blue, true)]
        [TestCase(ColorId.Orange, false)]
        [TestCase(ColorId.White, false)]
        [TestCase(ColorId.None, false)]
        public void IsPrimary_ClassifiesCorrectly(ColorId c, bool expected)
        {
            Assert.That(ColorMixer.IsPrimary(c), Is.EqualTo(expected));
        }

        [TestCase(ColorId.Orange, true)]
        [TestCase(ColorId.Green, true)]
        [TestCase(ColorId.Purple, true)]
        [TestCase(ColorId.Red, false)]
        [TestCase(ColorId.White, false)]
        public void IsSecondary_ClassifiesCorrectly(ColorId c, bool expected)
        {
            Assert.That(ColorMixer.IsSecondary(c), Is.EqualTo(expected));
        }

        [TestCase(ColorId.White, true)]
        [TestCase(ColorId.Red, false)]
        [TestCase(ColorId.Purple, false)]
        [TestCase(ColorId.Black, false)]
        public void IsTertiary_ClassifiesCorrectly(ColorId c, bool expected)
        {
            Assert.That(ColorMixer.IsTertiary(c), Is.EqualTo(expected));
        }

        // ---------- ColorMixCache equivalence ----------

        [Test]
        public void Cache_Lookup_Equivalent_RedBlue()
        {
            ColorMixCache.Initialize();
            Assert.That(ColorMixCache.Lookup(ColorId.Red, ColorId.Blue),
                Is.EqualTo(ColorMixer.Mix(ColorId.Red, ColorId.Blue)));
        }

        [Test]
        public void Cache_Lookup_Equivalent_Commutative()
        {
            ColorMixCache.Initialize();
            Assert.That(ColorMixCache.Lookup(ColorId.Blue, ColorId.Red),
                Is.EqualTo(ColorMixCache.Lookup(ColorId.Red, ColorId.Blue)));
        }

        [Test]
        public void Cache_Lookup_ExhaustiveMatchesFunction()
        {
            // Exhaustive sweep over the 11-value ColorId set to catch any divergence
            // between the function and the precomputed table.
            ColorMixCache.Initialize();
            ColorId[] all =
            {
                ColorId.None, ColorId.Red, ColorId.Yellow, ColorId.Blue,
                ColorId.Orange, ColorId.Green, ColorId.Purple, ColorId.White,
                ColorId.Black, ColorId.Prism, ColorId.Gray,
            };
            bool allMatch = true;
            for (int i = 0; i < all.Length; i++)
            {
                for (int j = 0; j < all.Length; j++)
                {
                    var fn = ColorMixer.Mix(all[i], all[j]);
                    var lk = ColorMixCache.Lookup(all[i], all[j]);
                    if (fn != lk) { allMatch = false; break; }
                }
                if (!allMatch) break;
            }
            Assert.That(allMatch, Is.True);
        }
    }
}
