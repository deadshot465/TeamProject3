﻿using System;

namespace TeamProject3
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new Game1();
            game.Run();

            return;
        }
    }
}
