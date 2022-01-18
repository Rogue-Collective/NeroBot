namespace NeroBot
{
    class Program
    {
        public static Bot? bot;

#if RELEASE
        [STAThread]
#endif

        public static void Main(string[] args)
        {
            bot = new Bot();
            bot.MainAsync(args).GetAwaiter().GetResult();
        }

    }
}