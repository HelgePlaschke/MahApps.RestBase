namespace MahApps.RESTBase
{
    public class File
    {
        public string FilePath { get; private set; }
        public string FileName { get; private set; }

        public File (string filePath, string fileName)
        {
            FilePath = filePath;
            FileName = fileName;
        }
    }
}