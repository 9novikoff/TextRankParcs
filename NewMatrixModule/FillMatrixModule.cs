using System.Linq;
using System.Threading;
using Parcs;

namespace NewMatrixModule
{
    public class FillMatrixModule: IModule
    {
        public void Run(ModuleInfo info, CancellationToken token = new CancellationToken())
        {
            var stringsToProcess = (string[][])info.Parent.ReadObject(typeof(string[][]));
            var indices = (int[])info.Parent.ReadObject(typeof(int[]));
            
            var len = stringsToProcess.Length;
            var matrix = new double[len][];
            for (var i = 0; i < len; i++)
            {
                matrix[i] = new double[len];
            }
            for (int i = 0; i < indices.Length; i++)
            {
                var index = indices[i];
                for (int j = index+1; j < len; j++)
                {
                    if(i == j) continue;
                    
                    var similarity = 1.0 * stringsToProcess[index].Intersect(stringsToProcess[j]).Count() / 
                                     (stringsToProcess[index].Length + stringsToProcess[j].Length);
                    matrix[index][j] = similarity;
                    matrix[j][index] = similarity;
                }
            }
            info.Parent.WriteObject(matrix);
        }
    }
}