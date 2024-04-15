using System;
using System.Collections.Generic;
using System.Threading;
using Parcs;

namespace NewMatrixModule
{
    public class TextRankModule : IModule
    {
        public void Run(ModuleInfo info, CancellationToken token = new CancellationToken())
        {
            var adjacencyMatrixList = (List<double[,]>)info.Parent.ReadObject(typeof(List<double[,]>));

            var pageRanks = new List<double[]>();

            foreach (var matrix in adjacencyMatrixList)
            {
                pageRanks.Add(GetTextRank(matrix));
            }
            
            info.Parent.WriteObject(pageRanks);
        }

        private double[] GetTextRank(double[,] adjacencyMatrix)
        {
            var tolerance = 1e-10;
            var d = 0.85;
            
            int n = adjacencyMatrix.GetLength(0);
            var outDegree = new double[n];
            
            double[] pagerank = new double[n];
            double[] oldPagerank = new double[n];
        
            for (int i = 0; i < n; i++)
            {
                pagerank[i] = 1.0 / n;
                oldPagerank[i] = 0;

                for (int j = 0; j < n; j++)
                {
                    if (adjacencyMatrix[i, j] != 0)
                        outDegree[i]++;
                }
            }
        
            while (Diff(pagerank, oldPagerank) > tolerance)
            {
                Array.Copy(pagerank, oldPagerank, n);

                for (int i = 0; i < n; i++)
                {
                    double sum = 0;

                    for (int j = 0; j < n; j++)
                    {
                        if (adjacencyMatrix[j, i] > 0)
                        {
                            sum += oldPagerank[j] * adjacencyMatrix[j, i] / outDegree[j];
                        }
                    }

                    pagerank[i] = (1 - d) / n + d * sum;
                }
            }

            return pagerank;
        }
        
        private double Diff(double[] arr1, double[] arr2)
        {
            double sum = 0;

            for (int i = 0; i < arr1.Length; i++)
            {
                sum += Math.Abs(arr1[i] - arr2[i]);
            }

            return sum;
        }
    }
}