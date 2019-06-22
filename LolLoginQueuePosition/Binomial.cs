using System;
using System.Collections.Generic;
using System.Linq;

namespace LolLoginQueuePosition
{
    public static class Binomial
    {

        /// <summary>
        /// Calculates the binomial coefficient (n choose k). Results in a pascals triangle. See: https://en.wikipedia.org/wiki/Binomial_coefficient
        /// Taken from: https://dmitrybrant.com/2008/04/29/binomial-coefficients-stirling-numbers-csharp
        /// </summary>
        /// <param name="n">row of the triangle, 0 indexed</param>
        /// <param name="k">column of the triangle, 0 indexed</param>
        /// <returns>binomial coefficient</returns>
        public static long Coefficient(long n, long k)
        {
            if (k < 0 || k > n) return 0;
            k = k > n / 2 ? n - k : k;
            long a = 1;
            for (long i = 1; i <= k; i++)
            {
                a = a * (n - k + i) / i;
            }
            return a + (long) 0.5;
        }

        /// <summary>
        /// Calculates a binomially-weighted average for a set of arbitrary length. See: https://en.wikipedia.org/wiki/Moving_average
        /// </summary>
        /// <param name="elements">Set of elements to calculate a binominally-weighted average for.</param>
        /// <returns>Binominally-weighted average.</returns>
        public static double CalculateBinomialAverage(IList<int> elements)
        {
            var setSize = elements.Count - 1;
            if (setSize == 0) return elements.First();
            if (setSize == -1) return 0;

            double binomialAverage = 0;
            for (var i = 0; i < elements.Count; i++)
            {
                binomialAverage += Coefficient(setSize, i) / Math.Pow(2, setSize) * elements[i];
            }

            return binomialAverage;
        }
    }
}
