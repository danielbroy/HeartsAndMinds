using System;
using System.Collections.Generic;
using System.Linq;
using HeartsAndMinds.Models;

namespace HeartsAndMinds
{
    internal static class Bot
    {
        public static void Start(Func<GameState, Move[]> strategy)
        {
            var settings = new Settings
            {
                Seed = ReadInt("seed"),

                Players = ReadInt("num-players"),
                PlayerId = ReadInt("player-id")
            };

            string line;
            while ((line = Console.ReadLine()) != "game-end")
            {
                var gamestate = new GameState(settings);

                // Turn init
                if (line != "turn-init")
                {
                    throw new Exception($"Expected 'turn-init', got '{line}'");
                }

                gamestate.Planets = ReadPlanets();
                gamestate.Ships = ReadShips();

                line = Console.ReadLine();
                if (line != "turn-start")
                {
                    throw new Exception($"Expected 'turn-start', got '{line}");
                }

                foreach (var move in strategy.Invoke(gamestate))
                {
                    Console.WriteLine(move);
                }

                Console.WriteLine("end-turn");
            }
        }

        private static string ReadValue(string key)
        {
            var line = Console.ReadLine();
            var parts = line.Split();

            if (parts.Length != 2 || parts[0] != key)
            {
                throw new Exception($"Excepted '{key} <value>', got '{line}'");
            }

            return parts[1];
        }

        private static int ReadInt(string key)
        {
            return int.Parse(ReadValue(key));
        }

        private static float ReadFloat(string key)
        {
            return float.Parse(ReadValue(key));
        }

        private static List<Planet> ReadPlanets()
        {
            var planetCount = ReadInt("num-planets");
            var planets = new List<Planet>();

            for (var i = 0; i < planetCount; i++)
            {
                planets.Add(ReadPlanet());
            }

            return planets;
        }

        private static Planet ReadPlanet()
        {
            var line = Console.ReadLine();
            var parts = line.Split();

            if (parts.Length != 7 || parts[0] != "planet")
            {
                throw new Exception($"Expected 'planet <id> <x> <y> <radius> <owner> <health>', got '{line}'");
            }

            return new Planet
            {
                Id = int.Parse(parts[1]),
                X = float.Parse(parts[2]),
                Y = float.Parse(parts[3]),
                Radius = float.Parse(parts[4]),
                Owner = ParseOwner(parts[5]),
                Health = float.Parse(parts[6]),
                Neighbors = ReadNeighbors(),
            };
        }

        private static int[] ReadNeighbors()
        {
            var line = Console.ReadLine();
            var parts = line.Split();

            if (parts.Length == 0 || parts[0] != "neighbors")
            {
                throw new Exception($"Expected 'neighbors <neighbor1> <neighbor2> ...', got '{line}'");
            }

            return parts.Skip(1).Select(int.Parse).ToArray();
        }

    private static List<Ship> ReadShips() {
            var shipCount = ReadInt("num-ships");
            var ships = new List<Ship>();

            for (var i = 0; i < shipCount; i++) {
                ships.Add(ReadShip());
            }

            return ships;
        }

        private static Ship ReadShip() {
            var line = Console.ReadLine();
            var parts = line.Split();

            if (parts.Length != 6 || parts[0] != "ship") {
                throw new Exception($"Expected 'ship <x> <y> <target_id> <owner> <power>', got '{line}'");
            }

            return new Ship {
                X = float.Parse(parts[1]),
                Y = float.Parse(parts[2]),
                TargetId = int.Parse(parts[3]),
                Owner = ParseOwner(parts[4]),
                Power = float.Parse(parts[5])
            };
        }

        private static int? ParseOwner(string owner) {
            if (owner == "neutral") {
                return null;
            }

            return int.Parse(owner);
        }
    }
}