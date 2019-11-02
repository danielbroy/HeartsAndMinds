using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using HeartsAndMinds.Models;

namespace HeartsAndMinds
{
    internal static class HeartsAndMinds
    {
        public static Move[] Strategy(GameState gamestate)
        {
            var moves = new List<Move>();

            // Calculate distance to the frontline for all planets.
            AddFrontlineInfoToPlanets(gamestate);

            // Collect some handy info we might need.
            int myPlayerId = gamestate.Settings.PlayerId;
            int opponentPlayerId = (myPlayerId == 0 ? 1 : 0);

            List<Planet> allPlanets = gamestate.Planets;
            List<Planet> myPlanets = allPlanets.Where(p => p.Owner == myPlayerId).ToList();
            List<Planet> opponentPlanets = allPlanets.Where(p => p.Owner == opponentPlayerId).ToList();
            List<Planet> neutralPlanets = allPlanets.Where(p => p.Owner == null).ToList();


            // Planets behind the lines send their ships to the frontline planets.
            foreach (Planet supplyPlanet in myPlanets.Where(p => !p.IsOnFrontline))
            {
                // Send ships to all planets that are closer to the frontline
                var planetsCloserToFrontline = supplyPlanet.NeighbouringPlanets.Where(np => np.DistanceToFrontLine < supplyPlanet.DistanceToFrontLine);

                if (!planetsCloserToFrontline.Any())
                {
                    // No planets closer to the frontline. This should be impossible, right?
                    Console.WriteLine("# No targets closer to the frontline found: FIXME");
                    continue;
                }

                int closestDistanceToFrontLine = planetsCloserToFrontline.Min(p => p.DistanceToFrontLine ?? 0);    // Because these are my planets, DistanceToFrontLine is never null.

                var targets = planetsCloserToFrontline.Where(p => p.DistanceToFrontLine == closestDistanceToFrontLine);
                float amountToSend = (supplyPlanet.Health - 1.01F) / targets.Count();

                targets.ToList().ForEach(t => moves.Add(new Move(amountToSend, supplyPlanet.Id, t.Id)));
            }

            // Frontline planets send their ships to the easiest planet to conquer and that won't already fall under our control, but keep enough ships to resist attack.
            foreach (Planet fp in myPlanets.Where(p => p.IsOnFrontline))
            {
                // How does the future of this planet look?
                if (fp.WillLoseControlInTheFuture)
                {
                    Console.WriteLine("# Will lose control");
                    // We are going to be conquered, or revert to neutral, if nothing changes.
                    // Don't make the situation worse and keep all ships on the planet.
                    continue;
                }

                // How many ship do we have to spare?
                float shipsAvailable = MathF.Min(fp.FutureHealth.Min(state => state.HealthBeforeGrowth) - 0.1F,  // -0.1 to be on the safe side.
                                                fp.Health - 1.01F);

                if (shipsAvailable < 0F)
                {
                    continue;
                }


                // What is going to be our target?

                // First collect all potential targets.
                var targets = fp.NeighbouringPlanets.Where(n => n.Owner != myPlayerId);

                // Choose the first suitable target with the least health.
                bool targetFound = false;
                int targetId = -1;
                foreach (Planet potentialTarget in targets.OrderBy(p => p.Health))
                {
                  //  Console.WriteLine($"# Planet {fp.Id} considering planet {potentialTarget.Id} (Health {potentialTarget.Health})");
                    // If we are sure that we are going to conquer this target, and there are no enemy planets nearby that could change that, then we don't need to send ships to this target.
                    if ((potentialTarget.FutureHealth.Last().Owner == myPlayerId) &&
                            !potentialTarget.NeighbouringPlanets.Any(np => np.Owner == opponentPlayerId))
                    {
                    //    Console.WriteLine("# Skipping");
                        // Skip to next target.
                        continue;
                    }
                    else
                    {
                        // Choose this target.
                      //  Console.WriteLine("# Choosing");
                        targetFound = true;
                        targetId = potentialTarget.Id;
                        break;
                    }
                }

                // Send ships
                if (targetFound)
                {
                    moves.Add(new Move(shipsAvailable, fp.Id, targetId));
                }
                else
                {
                    // No target found. Divide ships over all possible targets.
                    foreach (Planet potentialTarget in targets)
                    {
                        moves.Add(new Move(shipsAvailable/targets.Count(), fp.Id, potentialTarget.Id));
                    }
                }

            }

            return moves.ToArray();
        }
        
        // Determine which planets are on the frontline.
        private static void AddFrontlineInfoToPlanets(GameState gamestate)
        {
            var allPlanets = gamestate.Planets;
            var myPlanets = allPlanets.Where(p => p.Owner == gamestate.Settings.PlayerId);
            var hisPlanets = allPlanets.Where(p => p.Owner != null && p.Owner != gamestate.Settings.PlayerId);

            // Mark neutral planets.
            allPlanets.Where(p => p.Owner == null).ToList().ForEach(p => p.DistanceToFrontLine = -1);

            // Mark planets on the frontline.
            List<int> frontline = allPlanets.Where(p => p.DistanceToFrontLine == -1).SelectMany(p => p.Neighbors).ToList();
            frontline.AddRange(myPlanets.Where(mp => hisPlanets.SelectMany(hp => hp.Neighbors).Contains(mp.Id)).Select(mp => mp.Id));
            frontline.AddRange(hisPlanets.Where(hp => myPlanets.SelectMany(mp => mp.Neighbors).Contains(hp.Id)).Select(hp => hp.Id));
            frontline = frontline.Distinct().ToList();
            
            allPlanets.Where(p => p.DistanceToFrontLine == null).Where(p => frontline.Contains(p.Id)).ToList().ForEach(p => p.DistanceToFrontLine = 0);

            int distance = 1;
            while (allPlanets.Where(p => p.DistanceToFrontLine == null).Any())
            {
                allPlanets.Where(p => p.DistanceToFrontLine == null &&
                                        allPlanets.Where(q => q.Owner == p.Owner && q.DistanceToFrontLine == distance - 1).SelectMany(q => q.Neighbors).Contains(p.Id)).ToList().ForEach(p => p.DistanceToFrontLine = distance);
                distance++;
            }
        }
    }
}
