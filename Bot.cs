﻿using System;
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
                FillNeighbouringPlanets(gamestate.Planets);
                FillFutureHealth(gamestate);

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
                Neighbors = ReadNeighbors()
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

        private static void FillNeighbouringPlanets(List<Planet> planets)
        {
            foreach (Planet planet in planets)
            {
                planet.NeighbouringPlanets = new List<Planet>();
            }

            foreach (Planet planet in planets)
            {
                planets.Where(ps => ps.Neighbors.Contains(planet.Id)).ToList().ForEach(ps => ps.NeighbouringPlanets.Add(planet));
            }
             
        }

        private static void FillFutureHealth(GameState gamestate)
        {
            foreach(Planet planet in gamestate.Planets)
            {
                List<PlanetState> planetFuture = new List<PlanetState>();

                var incomingShips = gamestate.Ships.Where(s => s.TargetId == planet.Id).Select(s =>
                                    new
                                    {
                                        s.Owner,
                                        s.Power,
                                        TurnsToLand = (int)MathF.Floor(MathF.Sqrt(MathF.Pow(s.X - planet.X, 2.0F) + MathF.Pow(s.Y - planet.Y, 2.0F)) / 15.0F)       // Not sure why this shouldn't be Math.Ceiling.
                                    });

                PlanetState lastState = new PlanetState()
                {
                    HealthBeforeGrowth = float.PositiveInfinity,        // Unknown
                    HealthEndOfTurn = planet.Health,
                    Owner = planet.Owner
                };

                planetFuture.Add(new PlanetState()
                {
                    HealthBeforeGrowth = float.PositiveInfinity,        // Unknown
                    HealthEndOfTurn = planet.Health,
                    Owner = planet.Owner
                });

                if (!incomingShips.Any())
                {
                    planet.FutureHealth = planetFuture;
                    continue;
                }

//                Console.WriteLine($"# Incoming ships for planet {planet.Id}");
//                Console.WriteLine($"# " + String.Join(" --- ", incomingShips.Select(s => s.Owner + "/" + s.Power + "/" + s.TurnsToLand)));

//                Console.WriteLine($"# Starting health is {lastState.HealthEndOfTurn}, owner is {lastState.Owner}");
                for (int turn = 1; turn <= incomingShips.Max(i => i.TurnsToLand);turn++)
                {
                    lastState.HealthBeforeGrowth = lastState.HealthEndOfTurn;

                    foreach (var landingShip in incomingShips.Where(i => i.TurnsToLand == turn))
                    {
                        switch (lastState.Owner)
                        {
                            case null:
                                lastState.HealthBeforeGrowth -= landingShip.Power;
                                if (lastState.HealthBeforeGrowth < 0)
                                {
                                    lastState.HealthBeforeGrowth = -1.0F * lastState.HealthBeforeGrowth;
                                    lastState.Owner = landingShip.Owner;
                                }
                    //            Console.WriteLine($"# Health is now {lastState.HealthBeforeGrowth}, owner is {lastState.Owner}");

                                break;
                            case 0:
                            case 1:
                                if (landingShip.Owner == lastState.Owner)
                                {
                                    lastState.HealthBeforeGrowth += landingShip.Power;
                                }
                                else
                                {
                                    lastState.HealthBeforeGrowth -= landingShip.Power;
                                }

                                if (lastState.HealthBeforeGrowth < 0)
                                {
                                    lastState.HealthBeforeGrowth = -1.0F * lastState.HealthBeforeGrowth;
                                    lastState.Owner = landingShip.Owner;
                                }
                                else if (lastState.HealthBeforeGrowth == 0)
                                {
                                    lastState.Owner = null;
                                }
              //                  Console.WriteLine($"# Health is now {lastState.HealthBeforeGrowth}, owner is {lastState.Owner}");

                                break;
                        }
                    }

                    if (lastState.Owner != null)
                    {
                        lastState.HealthEndOfTurn = lastState.HealthBeforeGrowth + (planet.Radius * 0.05F);
                    } else
                    {
                        lastState.HealthEndOfTurn = lastState.HealthBeforeGrowth;
                    }

         //           Console.WriteLine($"# Health after growth is {lastState.HealthEndOfTurn}");

                    planetFuture.Add(new PlanetState()
                    {
                        HealthBeforeGrowth = lastState.HealthBeforeGrowth,
                        HealthEndOfTurn = lastState.HealthEndOfTurn,
                        Owner = lastState.Owner
                    });
                }

                planet.FutureHealth = planetFuture;
            }
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