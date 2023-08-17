namespace Grid.Bot.Interfaces
{
    public interface IConsoleHook
    {
        char[] HookKeys { get; }
        void Callback(char key);
    }
}
