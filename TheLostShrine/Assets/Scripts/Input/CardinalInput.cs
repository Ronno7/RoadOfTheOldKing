namespace TheLostShrine.Input
{
    public enum CardinalDirection { None, Up, Down, Left, Right }

    // Plain C#: chooses the newest held direction and remembers the older ones.
    public sealed class CardinalInput
    {
        private readonly bool[] held = new bool[4];
        private readonly long[] pressedAt = new long[4];
        private long sequence;

        public CardinalDirection Read(bool up, bool down, bool left, bool right)
        {
            Record(0, up);
            Record(1, down);
            Record(2, left);
            Record(3, right);

            int selected = -1;
            long newest = 0;
            for (int i = 0; i < held.Length; i++)
            {
                if (held[i] && pressedAt[i] > newest)
                {
                    selected = i;
                    newest = pressedAt[i];
                }
            }

            return selected < 0 ? CardinalDirection.None : (CardinalDirection)(selected + 1);
        }

        public void Clear()
        {
            for (int i = 0; i < held.Length; i++)
            {
                held[i] = false;
                pressedAt[i] = 0;
            }
            sequence = 0;
        }

        private void Record(int index, bool isHeld)
        {
            if (isHeld && !held[index])
                pressedAt[index] = ++sequence;

            held[index] = isHeld;
        }
    }
}
