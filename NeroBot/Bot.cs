namespace NeroBot
{
    class Bot
    {

        public LiveMonitor? liveMonitor;
        public async Task MainAsync(string[] args)
        {

            liveMonitor = new LiveMonitor();

            await Task.Delay(Timeout.Infinite);
        }


    }
}
