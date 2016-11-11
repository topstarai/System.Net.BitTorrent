using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Net.BitTorrent.Client
{
    public class RandomisedPickerFixture : IDisposable
    {
        public PeerId Id { get; private set; }
        public TestRig Rig { get; private set; }

        public RandomisedPickerFixture()
        {
            Rig = TestRig.CreateMultiFile();
            Id = new PeerId(new Peer(new string('a', 20), new Uri("tcp://BLAH")), Rig.Manager);
            for (int i = 0; i < Id.BitField.Length; i += 2)
                Id.BitField[i] = true;
        }

        public void Dispose()
        {
            Rig.Dispose();
        }
    }

    public class RandomisedPickerTests:IClassFixture<RandomisedPickerFixture>
    {
        //static void Main()
        //{
        //    RandomisedPickerTests t = new RandomisedPickerTests();
        //    t.FixtureSetup();
        //    t.Setup();
        //    t.Pick();
        //}

        PeerId id;
        RandomisedPicker picker;
        TestRig rig;
        TestPicker tester;

        public RandomisedPickerTests(RandomisedPickerFixture rf)
        {
            rig = rf.Rig;
            id = rf.Id;
        }

        public RandomisedPickerTests()
        {
            tester = new TestPicker();
            picker = new RandomisedPicker(tester);
        }

        [Fact]
        public void EnsureRandomlyPicked()
        {
            tester.ReturnNoPiece = false;
            while (picker.PickPiece(id, new List<PeerId>(), 1) != null) { }
            Assert.Equal(rig.Torrent.Pieces.Count, tester.PickedPieces.Count);
            List<int> pieces = new List<int>(tester.PickedPieces);
            pieces.Sort();
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i] != tester.PickedPieces[i])
                    return;
            Assert.True(false,"The piece were picked in order");
        }
    }
}
