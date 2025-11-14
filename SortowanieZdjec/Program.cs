class Program
{
    static void Main()
    {
        string startingpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string targetpath = Path.Combine(startingpath, "csharp_p3", "SortowanieZdjec", "co_znalazlem");
        if (!Directory.Exists(targetpath))
        {
            Directory.CreateDirectory(targetpath);
        }

        string targettextpath = Path.Combine(targetpath, "dane.txt");
        FileStream oot = new FileStream(targettextpath, FileMode.Create, FileAccess.ReadWrite);
        StreamWriter sr = new StreamWriter(oot);

        IEnumerable<string> Entries = Directory.EnumerateFiles(startingpath, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    string filename = Path.GetFileName(file);
                    return filename.Length < 10 && Path.GetExtension(file) == ".cpp"; //filename.StartsWith("O") && 
                })
            .OrderBy(file => Path.GetFileName(file));

        int i = 0;
        foreach (var variable in Entries)
        {
            sr.Write(variable + '\n');
            //Console.Write(" " + ++i + "\n");
        }
        sr.Close();
    }
}