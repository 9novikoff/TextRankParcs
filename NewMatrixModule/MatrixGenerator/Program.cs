using NewMatrixModule;

namespace MatrixGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            int h = 2;
            int w = 2;
            new Matrix(h, w, true).WriteToFile("matrix2.mtr");
        }
    }
}
