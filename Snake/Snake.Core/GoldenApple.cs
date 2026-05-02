using Microsoft.Xna.Framework;

namespace Snake.Core
{
    /// <summary>
    /// A short-lived bonus apple spawned by the Frenzy upgrade. Appears at a random valid
    /// position with a finite lifetime; if the snake eats it before TimeRemaining hits zero
    /// the player receives a flat bonus, otherwise it disappears silently.
    /// </summary>
    public class GoldenApple
    {
        public Point Position { get; set; }
        public float TimeRemaining { get; set; }
        public bool Active { get; set; }

        public void Spawn(Point position, float lifetimeSec)
        {
            Position = position;
            TimeRemaining = lifetimeSec;
            Active = true;
        }

        public void Deactivate()
        {
            Active = false;
            TimeRemaining = 0f;
        }

        public void Tick(float dt)
        {
            if (!Active) return;
            TimeRemaining -= dt;
            if (TimeRemaining <= 0f) Deactivate();
        }
    }
}
