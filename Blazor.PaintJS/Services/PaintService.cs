using System.Drawing;

namespace Blazor.PaintJS.Services
{

    public class PaintService
    {
        // DON´T TOUCH
        public IEnumerable<Point> BrensenhamLine(Point begin, Point end)
        {
            int dx = Math.Abs(end.X - begin.X), sx = begin.X < end.X ? 1 : -1;
            int dy = Math.Abs(end.Y - begin.Y), sy = begin.Y < end.Y ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2;
            while (true)
            {
                yield return new Point
                {
                    X = begin.X,
                    Y = begin.Y
                };

                if (begin.X == end.X && begin.Y == end.Y) break;
                var e2 = err;
                if (e2 > -dx)
                {
                    err -= dy;
                    begin.X += sx;
                }

                if (e2 < dy)
                {
                    err += dx;
                    begin.Y += sy;
                }
            }
        }
    }
}