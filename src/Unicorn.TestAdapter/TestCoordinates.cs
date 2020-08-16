namespace Unicorn.TestAdapter
{
    public class TestCoordinates
    {
        public static readonly TestCoordinates Invalid = new TestCoordinates(null, 0);

        public TestCoordinates(string filePath, int lineNumber)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
        }

        public string FilePath { get; private set; }

        public int LineNumber { get; private set; }

        public bool IsValid => !string.IsNullOrEmpty(FilePath);
    }
}
