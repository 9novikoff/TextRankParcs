using NewMatrixModule;

public interface IPorter2Stemmer : IStemmer
{
    char[] Vowels { get; }
    
    string[] Doubles { get; }
    
    char[] LiEndings { get; }
    
    int GetRegion1(string word);
    
    int GetRegion2(string word);
}