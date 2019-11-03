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
                // How does the future of this planet look?
                if (supplyPlanet.WillLoseControlInTheFuture)
                {
                    Console.WriteLine("# Will lose control");
                    // We are going to be conquered, or revert to neutral, if nothing changes.
                    // Don't make the situation worse and keep all ships on the planet.
                    continue;
                }

                // How many ships do we have to spare?
                float shipsAvailable = MathF.Min(supplyPlanet.FutureHealth.Min(state => state.HealthBeforeGrowth) - 0.1F,  // -0.1 to be on the safe side.
                                                supplyPlanet.Health - 1.01F);

                if (shipsAvailable < 0F)
                {
                    continue;
                }

                // Are there neighbours that are going to be conquered?
                if (supplyPlanet.NeighbouringPlanets.Any(np => np.WillLoseControlInTheFuture))
                {
                    // Send our ships to the nearest one, to try to help.
                    moves.Add(new Move(shipsAvailable, supplyPlanet.Id, supplyPlanet.NeighbouringPlanets.Where(np => np.WillLoseControlInTheFuture).OrderBy(np => np.DistanceTo(supplyPlanet)).First().Id));
                    continue;
                } else
                {
                    // Otherwise, send ships to all planets that are closer to the frontline
                    var planetsCloserToFrontline = supplyPlanet.NeighbouringPlanets.Where(np => np.DistanceToFrontLine < supplyPlanet.DistanceToFrontLine);

                    if (!planetsCloserToFrontline.Any())
                    {
                        // No planets closer to the frontline. This should be impossible, right?
                        Console.WriteLine("# No targets closer to the frontline found: FIXME");
                        continue;
                    }

                    int closestDistanceToFrontLine = planetsCloserToFrontline.Min(p => p.DistanceToFrontLine ?? 0);    // Because these are my planets, DistanceToFrontLine is never null.

                    var targets = planetsCloserToFrontline.Where(p => p.DistanceToFrontLine == closestDistanceToFrontLine);

                    float amountToSend = shipsAvailable / targets.Count();

                    targets.ToList().ForEach(t => moves.Add(new Move(amountToSend, supplyPlanet.Id, t.Id)));
                }
            }

            Dictionary<int, float> needHelp = new Dictionary<int, float>();
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

                // Are we in the expansion phase? (=no enemies as neighbours)
                bool expansionLogic = !fp.NeighbouringPlanets.Any(n => n.Owner == opponentPlayerId);

                // Collect all potential targets and order them according to our preference.
                List<Planet> targets;
                if (expansionLogic)
                {
                    // In the expansion phase, attack the nearest planet first.
                    targets = fp.NeighbouringPlanets.Where(n => n.Owner == null).OrderBy(n => n.DistanceTo(fp)).ThenBy(n => n.Health).ToList();
                }
                else
                {
                    // When we have enemies nearby, attack the weakest planet first.
                    targets = fp.NeighbouringPlanets.Where(n => n.Owner != myPlayerId).OrderBy(p => p.Health).ToList();
                }

                if (expansionLogic && fp.Neighbors.Any(n => needHelp.ContainsKey(n)))
                {
                    foreach (Planet needsHelp in fp.NeighbouringPlanets.Where(n => needHelp.ContainsKey(n.Id))) {
                        if (shipsAvailable <= 0.001F)
                        {
                            break;
                        }

                        if (needHelp[needsHelp.Id] < 0.01F)
                        {
                            targets.Remove(needsHelp);
                            continue;
                        }

                        float numOfShipsToSend = Math.Min(needHelp[needsHelp.Id], shipsAvailable);
                        moves.Add(new Move(numOfShipsToSend, fp.Id, needsHelp.Id));
                        shipsAvailable -= numOfShipsToSend;

                        needHelp[needsHelp.Id] -= numOfShipsToSend;
                        targets.Remove(needsHelp);
                    }
                }

                foreach (Planet potentialTarget in targets)
                { 
                    if (shipsAvailable <= 0.001F)
                    {
                        break;
                    }
                    
                    // If we are sure that we are going to conquer this target, and there are no enemy planets nearby that could change that, then we don't need to send ships to this target.
                    if ((potentialTarget.FutureHealth.Last().Owner == myPlayerId) &&
                                !potentialTarget.NeighbouringPlanets.Any(np => np.Owner == opponentPlayerId))
                    {
                        // Skip to next target.
                        continue;
                    }
                    else
                    {
                        // Choose this target. Only send enough ships to conquer.
                        float numOfShipsToSend = Math.Min(potentialTarget.Health + 0.1F, shipsAvailable);

                        moves.Add(new Move(numOfShipsToSend, fp.Id, potentialTarget.Id));
                        shipsAvailable -= numOfShipsToSend;

                        if (expansionLogic && numOfShipsToSend < potentialTarget.Health + 0.09F)
                        {
                            needHelp.Add(potentialTarget.Id, potentialTarget.Health - numOfShipsToSend + 0.1F);
                        }
                    }
                }

                if (shipsAvailable > 0.001F)
                {
                    // Not all ships allocated. Divide remaining ships over all possible targets.
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
