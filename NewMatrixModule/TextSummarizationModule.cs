using Parcs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace NewMatrixModule
{
    using log4net;

    public class TextSummarizationModule : MainModule
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TextSummarizationModule));

        private static CommandLineOptions options;

        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            options = new CommandLineOptions();
            
            (new TextSummarizationModule()).RunModule(options);
        }

        public override void Run(ModuleInfo info, CancellationToken token = default(CancellationToken))
        {
            int pointsNum = 4;
            
            _log.InfoFormat("Starting Matrixes Module on {0} points", pointsNum);

            var points = new IPoint[pointsNum];
            var channels = new IChannel[pointsNum];
            for (int i = 0; i < pointsNum; ++i)
            {
                points[i] = info.CreatePoint();
                channels[i] = points[i].CreateChannel();
                points[i].ExecuteClass("NewMatrixModule.FillMatrixModule");
            }
            
            DateTime time = DateTime.Now;
            _log.Info("Waiting for a result...");

            var text = File.ReadAllText("text.txt");
            
            var stemmer = new EnglishPorter2Stemmer();

            var sentences = Regex.Split(text, @"(?<=[\.!\?])\s+").Select(s => s.Split(' ').Select(w => new string(stemmer.Stem(w).Where(c => !char.IsPunctuation(c)).ToArray())).Distinct().Where(w => !StopWordsCollection.StopWords.Contains(w) && !string.IsNullOrWhiteSpace(w)).ToArray()).Where(l => l.Length != 0).ToArray();
            var len = sentences.Length;
            
            var indices = new List<List<int>>();
            
            for (int i = 0; i < pointsNum; i++)
            {
                indices.Add(new List<int>());
            }
            
            int maxIndex = len-1;
            var currentPoint = 0;
            for (int i = 0; i <= maxIndex / 2; i++)
            {
                if(maxIndex - i == i)
                    indices[currentPoint].Add(i);
                else
                {
                    indices[currentPoint].Add(i);
                    indices[currentPoint].Add(maxIndex - i);   
                }
            
                currentPoint++;
                currentPoint %= pointsNum;
            }
            
            
            
            var matrix = new double[len][];
            for (var i = 0; i < len; i++)
            {
                matrix[i] = new double[len];
            }
            
            _log.Info("Waiting for a result...");
            
            for (int i = 0; i < pointsNum; i++)
            {
                channels[i].WriteObject(sentences);
                channels[i].WriteObject(indices[i].OrderBy(x => x).ToArray());
            }
            
            
            LogSendingTime(time);
            
            for (int i = 0; i < channels.Length; i++)
            {
                var res = channels[i].ReadObject<double[][]>();
                
                for (int j = 0; j < len; j++)
                {
                    for (int k = 0; k < len; k++)
                    {
                        matrix[j][k] += res[j][k];
                    }
                }
            }

            var components = BFS(matrix);
            var sentenceIndices = components.Select(t => t.Item2).ToList();
            
            for (int i = 0; i < pointsNum; ++i)
            {
                points[i] = info.CreatePoint();
                channels[i] = points[i].CreateChannel();
                points[i].ExecuteClass("NewMatrixModule.TextRankModule");
            }
            
            if(components.Count > pointsNum)
            {
                for (int i = 0; i < pointsNum; i++)
                {
                    if (i == pointsNum - 1)
                    {
                        channels[i].WriteObject(components.GetRange(components.Count/pointsNum*i, components.Count - components.Count/pointsNum*i).Select(c => c.Item1).ToList());
                    }
                    else
                    {
                        channels[i].WriteObject(components.GetRange(components.Count/pointsNum*i, components.Count/pointsNum).Select(c => c.Item1).ToList());
                    }
                }
            }
            else
            {
                for (int i = 0; i < components.Count; i++)
                {
                    channels[i].WriteObject(new List<double[,]>(){components[i].Item1});
                }
            }


            var textRank = new double[len];

            
            if(components.Count > pointsNum)
            {
                var previous = 0;
                
                for (int i = 0; i < pointsNum; i++)
                {
                    var res = channels[i].ReadObject<List<double[]>>();

                    for (int j = 0; j < res.Count; j++)
                    {
                        for (int k = 0; k < res[j].Length; k++)
                        {
                            textRank[sentenceIndices[j+previous][k]] = res[j][k];   
                        }
                    }

                    previous += res.Count;
                }
            }
            else
            {
                for (int i = 0; i < components.Count; i++)
                {
                    var res = channels[i].ReadObject<List<double[]>>();

                    for (int j = 0; j < res[0].Length; j++)
                    {
                        textRank[sentenceIndices[0][j]] = res[0][j];
                    }
                }
            }
            
            LogResultFoundTime(time);
            
            var summaryIndices = textRank.Select((value, index) => new { Value = value, Index = index })
                .OrderByDescending(x => x.Value)
                .Take(5)
                .Select(x => x.Index)
                .OrderBy(x => x);
            var summary = string.Join("\n", summaryIndices.Select(index => Regex.Split(text, @"(?<=[\.!\?])\s+")[index]));
            SaveSummary(summary);
        }

        private static void LogResultFoundTime(DateTime time)
        {
            _log.InfoFormat(
                "Result found: time = {0}",
                Math.Round((DateTime.Now - time).TotalSeconds, 3));
        }

        private static void LogSendingTime(DateTime time)
        {
            _log.InfoFormat("Sending finished: time = {0}", Math.Round((DateTime.Now - time).TotalSeconds, 3));
        }

        private static void SaveSummary(string summary)
        {
            File.WriteAllText(DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"), summary);
        }
        
        private static List<Tuple<double[,], int[]>> BFS(double[][] matrix)
        {
            var res = new List<Tuple<double[,], int[]>>();
            var componentNum = 1;
            var len = matrix.GetLength(0);
            var items = new int[len];
            var queue = new Queue<int>();

            for (int i = 0; i < len; i++)
            {
                if (items[i] == 0)
                {
                    var indices = new List<int>();
                    items[i] = componentNum;
                    queue.Enqueue(i);

                    while(queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        items[current] = componentNum;
                        indices.Add(current);

                        for (int j = 0; j < len; j++)
                        {
                            if (matrix[i][j] != 0 && items[j] == 0 && !queue.Contains(j))
                            {
                                queue.Enqueue(j);
                            }
                        }
                    }

                    var indicesCount = indices.Count;
                    var subMatrix = new double[indicesCount, indicesCount];

                    for (int j = 0; j < indicesCount; j++)
                    {
                        for (int k = 0; k < indicesCount; k++)
                        {
                            subMatrix[j, k] = matrix[indices[j]][indices[k]];
                        }
                    }
                    
                    componentNum++;
                    res.Add(new Tuple<double[,], int[]>(subMatrix, indices.ToArray()));
                }
            }

            return res;
        }

        private static void DFSUtil(double[,] matrix, int v, bool[] visited, List<int> component)
        {
            visited[v] = true;
            component.Add(v);

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                if (matrix[v, i] != 0 && !visited[i])
                    DFSUtil(matrix, i, visited, component);
            }
        }
        
        private static List<Tuple<double[,], int[]>> SplitIntoComponents(double[,] matrix)
        {
            int V = matrix.GetLength(0);
            bool[] visited = new bool[V];
            List<Tuple<double[,], int[]>> components = new List<Tuple<double[,], int[]>>();

            for (int v = 0; v < V; ++v)
            {
                if (!visited[v])
                {
                    List<int> component = new List<int>();
                    DFSUtil(matrix, v, visited, component);
                    
                    double[,] componentMatrix = new double[component.Count, component.Count];
                    int[] indexes = component.ToArray();

                    for (int i = 0; i < component.Count; i++)
                    {
                        for (int j = 0; j < component.Count; j++)
                        {
                            componentMatrix[i, j] = matrix[component[i], component[j]];
                        }
                    }

                    components.Add(new Tuple<double[,], int[]>(componentMatrix, indexes));
                }
            }

            return components;
        }

    }
}
