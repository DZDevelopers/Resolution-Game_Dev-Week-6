using System;
using System.Collections.Generic;
using System.Threading;

namespace TerminalSnake;

internal enum Direction
{
    Up,
    Down,
    Left,
    Right
}

internal readonly struct Point
{
    public readonly int X;
    public readonly int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static bool operator ==(Point a, Point b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Point a, Point b) => !(a == b);

    public override bool Equals(object? obj) => obj is Point p && this == p;
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

internal sealed class Game
{
    private const int Width = 30;
    private const int Height = 18;

    private const int StartDelayMs = 140;
    private const int MinDelayMs = 60;
    private const int SpeedupEveryNFood = 4;
    private const int SpeedupStepMs = 10;

    private const int OriginRow = 2;
    private const int OriginCol = 1;

    private readonly LinkedList<Point> _snake = new();
    private readonly HashSet<Point> _snakeLookup = new();
    private readonly HashSet<Point> _prevCells = new();
    private readonly Random _rng = new();

    private Direction _dir = Direction.Right;
    private Direction _nextDir = Direction.Right;
    private Point _food;
    private int _score;
    private int _foodEaten;
    private int _delayMs = StartDelayMs;
    private bool _over;

    public void Run()
    {
        Console.CursorVisible = false;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            DrawBorder();
            InitSnake();
            SpawnFood();
            DrawScore();

            using var input = new InputReader();

            while (!_over)
            {
                ReadKeys(input);
                Step();
                Thread.Sleep(_delayMs);
            }

            ShowGameOver(input);
        }
        finally
        {
            Console.CursorVisible = true;
            Console.ResetColor();
        }
    }

    private void InitSnake()
    {
        int startX = Width / 4;
        int startY = Height / 2;

        _snake.AddLast(new Point(startX - 2, startY));
        _snake.AddLast(new Point(startX - 1, startY));
        _snake.AddLast(new Point(startX, startY));

        foreach (var segment in _snake)
            _snakeLookup.Add(segment);
    }

    private void ReadKeys(InputReader input)
    {
        while (input.TryReadKey(out var key))
        {
            if (key == ConsoleKey.Escape)
            {
                _over = true;
                return;
            }

            Direction? requested = key switch
            {
                ConsoleKey.UpArrow or ConsoleKey.W => Direction.Up,
                ConsoleKey.DownArrow or ConsoleKey.S => Direction.Down,
                ConsoleKey.LeftArrow or ConsoleKey.A => Direction.Left,
                ConsoleKey.RightArrow or ConsoleKey.D => Direction.Right,
                _ => _nextDir
            };

            if (requested.HasValue && !IsOpposite(requested.Value, _dir))
                _nextDir = requested.Value;
        }
    }

    private static bool IsOpposite(Direction a, Direction b) =>
        (a == Direction.Up && b == Direction.Down) ||
        (a == Direction.Down && b == Direction.Up) ||
        (a == Direction.Left && b == Direction.Right) ||
        (a == Direction.Right && b == Direction.Left);

    private void Step()
    {
        _dir = _nextDir;

        var head = _snake.Last!.Value;
        var newHead = _dir switch
        {
            Direction.Up => new Point(head.X, head.Y - 1),
            Direction.Down => new Point(head.X, head.Y + 1),
            Direction.Left => new Point(head.X - 1, head.Y),
            Direction.Right => new Point(head.X + 1, head.Y),
            _ => head
        };

        if (newHead.X < 0 || newHead.X >= Width || newHead.Y < 0 || newHead.Y >= Height || _snakeLookup.Contains(newHead))
        {
            _over = true;
            return;
        }

        _snake.AddLast(newHead);
        _snakeLookup.Add(newHead);

        if (newHead == _food)
        {
            _score += 10;
            _foodEaten++;

            if (_foodEaten % SpeedupEveryNFood == 0 && _delayMs > MinDelayMs)
                _delayMs = Math.Max(MinDelayMs, _delayMs - SpeedupStepMs);

            SpawnFood();
            DrawScore();
        }
        else
        {
            var tail = _snake.First!.Value;
            _snake.RemoveFirst();
            _snakeLookup.Remove(tail);
        }

        DrawDelta();
    }

    private void SpawnFood()
    {
        if (_snakeLookup.Count >= Width * Height)
        {
            _over = true;
            return;
        }

        Point candidate;
        do
        {
            candidate = new Point(_rng.Next(0, Width), _rng.Next(0, Height));
        } while (_snakeLookup.Contains(candidate));

        _food = candidate;
        DrawCell(_food, '*', ConsoleColor.Red);
    }

    private void DrawBorder()
    {
        Console.Clear();
        Console.SetCursorPosition(0, OriginRow - 1);
        Console.Write('+' + new string('-', Width) + '+');

        for (int y = 0; y < Height; y++)
        {
            Console.SetCursorPosition(0, OriginRow + y);
            Console.Write('|');
            Console.SetCursorPosition(Width + OriginCol, OriginRow + y);
            Console.Write('|');
        }

        Console.SetCursorPosition(0, OriginRow + Height);
        Console.Write('+' + new string('-', Width) + '+');

        Console.SetCursorPosition(0, OriginRow + Height + 2);
        Console.WriteLine("Arrows/WASD to move, Esc to quit.");
    }

    private void DrawScore()
    {
        Console.SetCursorPosition(0, 0);
        Console.Write($"Score: {_score}".PadRight(Width + 2));
    }

    private void DrawCell(Point p, char symbol, ConsoleColor color)
    {
        Console.SetCursorPosition(OriginCol + p.X, OriginRow + p.Y);
        Console.ForegroundColor = color;
        Console.Write(symbol);
        Console.ResetColor();
    }

    private void ClearCell(Point p)
    {
        Console.SetCursorPosition(OriginCol + p.X, OriginRow + p.Y);
        Console.Write(' ');
    }

    private void DrawDelta()
    {
        foreach (var cell in _prevCells)
        {
            if (!_snakeLookup.Contains(cell))
                ClearCell(cell);
        }

        var head = _snake.Last!.Value;
        foreach (var segment in _snake)
            DrawCell(segment, segment == head ? '@' : 'o', ConsoleColor.Green);

        _prevCells.Clear();
        foreach (var segment in _snake)
            _prevCells.Add(segment);
    }

    private void ShowGameOver(InputReader input)
    {
        string message = $" GAME OVER — Score: {_score}  (Esc to exit) ";
        int row = OriginRow + Height / 2;
        int col = Math.Max(OriginCol, OriginCol + (Width - message.Length) / 2);

        Console.SetCursorPosition(col, row);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(message);
        Console.ResetColor();
        Console.SetCursorPosition(0, OriginRow + Height + 3);

        while (true)
        {
            if (input.TryReadKey(out var key) && key == ConsoleKey.Escape)
                return;

            Thread.Sleep(30);
        }
    }
}

internal sealed class InputReader : IDisposable
{
    public bool TryReadKey(out ConsoleKey key)
    {
        if (Console.KeyAvailable)
        {
            var info = Console.ReadKey(intercept: true);
            key = info.Key;
            return true;
        }

        key = default;
        return false;
    }

    public void Dispose() { }
}

internal static class EntryPoint
{
    private static void Main()
    {
        new Game().Run();
    }
}