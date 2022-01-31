namespace NeroBot
{
    static class Logging
    {
        public static void WriteToFile(Exception e)
        {
            using (StreamWriter sr = new StreamWriter(@"log.txt", append: true))
            {
                var dtn = DateTime.Now;
                sr.WriteLine(dtn.ToString() + Environment.NewLine);
                sr.WriteLine(e.ToString() + Environment.NewLine);
            }
        }

    }
}
