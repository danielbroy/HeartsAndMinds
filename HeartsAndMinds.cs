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

            // Collect some handy info we will need.
            int myPlayerId = gamestate.Settings.PlayerId;
            int opponentPlayerId = (myPlayerId == 0 ? 1 : 0);

            List<Planet> allPlanets = gamestate.Planets;
            List<Planet> myPlanets = allPlanets.Where(p => p.Owner == myPlayerId).ToList();
            List<Planet> opponentPlanets = allPlanets.Where(p => p.Owner == opponentPlayerId).ToList();
            List<Planet> neutralPlanets = allPlanets.Where(p => p.Owner == null).ToList();


            // Planets behind the lines send their ships to the frontline planets.
            foreach(Planet supplyPlanet in myPlanets.Where(p => !p.IsOnFrontline))
            {
                Console.WriteLine($"# Supplyplanet {supplyPlanet.Id} - Health {supplyPlanet.Health}");

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

            // Frontline planets send their ships to the easiest planet to conquer.
            foreach (Planet fp in myPlanets.Where(p => p.IsOnFrontline))
            {
                Console.WriteLine($"# Planet {fp.Id} - Health {fp.Health}");

                var targets = opponentPlanets.Where(p => p.Neighbors.Contains(fp.Id)).Select(p => p.Id).ToList();
                targets.AddRange(neutralPlanets.Where(p => p.Neighbors.Contains(fp.Id)).Select(p => p.Id));

                float amountToSend = fp.Health * 0.7F;
                if (fp.Health - amountToSend < 1.0F) continue;
                
                moves.Add(new Move(amountToSend, fp.Id, allPlanets.Where(p => targets.Contains(p.Id)).OrderBy(p => p.Health).First().Id));
            }

            return moves.ToArray();
        }

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
